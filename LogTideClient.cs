using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LogTide.SDK.Enums;
using LogTide.SDK.Exceptions;
using LogTide.SDK.Internal;
using LogTide.SDK.Models;

namespace LogTide.SDK;

/// <summary>
/// Main LogTide SDK client for sending and querying logs.
/// </summary>
/// <remarks>
/// This client provides automatic batching, retry logic with exponential backoff,
/// circuit breaker pattern for fault tolerance, and comprehensive query capabilities.
/// </remarks>
public class LogTideClient : IDisposable, IAsyncDisposable
{
    private readonly ClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly List<LogEntry> _buffer = new();
    private readonly object _bufferLock = new();
    private readonly object _metricsLock = new();
    private readonly Timer _flushTimer;
    private readonly List<double> _latencyWindow = new();
    
    private ClientMetrics _metrics = new();
    private string? _currentTraceId;
    private bool _disposed;

    /// <summary>
    /// Creates a new LogTide client.
    /// </summary>
    /// <param name="options">Client configuration options.</param>
    public LogTideClient(ClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        // Initialize HTTP client
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.ApiUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds)
        };
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
        
        // Initialize circuit breaker
        _circuitBreaker = new CircuitBreaker(
            options.CircuitBreakerThreshold,
            options.CircuitBreakerResetMs
        );
        
        // Start flush timer
        _flushTimer = new Timer(
            _ => _ = FlushAsync(),
            null,
            options.FlushIntervalMs,
            options.FlushIntervalMs
        );

        if (_options.Debug)
        {
            Console.WriteLine($"[LogTide] Client initialized: {options.ApiUrl}");
        }
    }

    /// <summary>
    /// Creates a new LogTide client with an existing HttpClient (for DI scenarios).
    /// </summary>
    public LogTideClient(ClientOptions options, HttpClient httpClient)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
        
        _circuitBreaker = new CircuitBreaker(
            options.CircuitBreakerThreshold,
            options.CircuitBreakerResetMs
        );
        
        _flushTimer = new Timer(
            _ => _ = FlushAsync(),
            null,
            options.FlushIntervalMs,
            options.FlushIntervalMs
        );

        if (_options.Debug)
        {
            Console.WriteLine($"[LogTide] Client initialized with custom HttpClient: {options.ApiUrl}");
        }
    }

    #region Trace ID Context

    /// <summary>
    /// Sets the trace ID for subsequent logs.
    /// </summary>
    /// <param name="traceId">Trace ID or null to clear.</param>
    public void SetTraceId(string? traceId)
    {
        _currentTraceId = traceId;
    }

    /// <summary>
    /// Gets the current trace ID.
    /// </summary>
    public string? GetTraceId() => _currentTraceId;

    /// <summary>
    /// Executes an action with a specific trace ID context.
    /// </summary>
    /// <param name="traceId">Trace ID to use.</param>
    /// <param name="action">Action to execute.</param>
    public void WithTraceId(string traceId, Action action)
    {
        var previousTraceId = _currentTraceId;
        _currentTraceId = traceId;
        try
        {
            action();
        }
        finally
        {
            _currentTraceId = previousTraceId;
        }
    }

    /// <summary>
    /// Executes a function with a specific trace ID context.
    /// </summary>
    public T WithTraceId<T>(string traceId, Func<T> func)
    {
        var previousTraceId = _currentTraceId;
        _currentTraceId = traceId;
        try
        {
            return func();
        }
        finally
        {
            _currentTraceId = previousTraceId;
        }
    }

    /// <summary>
    /// Executes an action with a new auto-generated trace ID.
    /// </summary>
    public void WithNewTraceId(Action action)
    {
        WithTraceId(Guid.NewGuid().ToString(), action);
    }

    /// <summary>
    /// Executes a function with a new auto-generated trace ID.
    /// </summary>
    public T WithNewTraceId<T>(Func<T> func)
    {
        return WithTraceId(Guid.NewGuid().ToString(), func);
    }

    #endregion

    #region Logging Methods

    /// <summary>
    /// Logs a custom entry.
    /// </summary>
    /// <param name="entry">Log entry to send.</param>
    /// <exception cref="BufferFullException">Thrown when the buffer is full.</exception>
    public void Log(LogEntry entry)
    {
        if (_disposed) return;

        // Apply trace ID
        if (string.IsNullOrEmpty(entry.TraceId))
        {
            if (!string.IsNullOrEmpty(_currentTraceId))
            {
                entry.TraceId = _currentTraceId;
            }
            else if (_options.AutoTraceId)
            {
                entry.TraceId = Guid.NewGuid().ToString();
            }
        }

        // Merge global metadata
        if (_options.GlobalMetadata.Count > 0)
        {
            foreach (var kvp in _options.GlobalMetadata)
            {
                if (!entry.Metadata.ContainsKey(kvp.Key))
                {
                    entry.Metadata[kvp.Key] = kvp.Value;
                }
            }
        }

        lock (_bufferLock)
        {
            if (_buffer.Count >= _options.MaxBufferSize)
            {
                lock (_metricsLock)
                {
                    _metrics.LogsDropped++;
                }

                if (_options.Debug)
                {
                    Console.WriteLine($"[LogTide] Buffer full, dropping log: {entry.Message}");
                }

                throw new BufferFullException();
            }

            _buffer.Add(entry);

            if (_buffer.Count >= _options.BatchSize)
            {
                _ = FlushAsync();
            }
        }
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    public void Debug(string service, string message, Dictionary<string, object?>? metadata = null)
    {
        Log(new LogEntry
        {
            Service = service,
            Level = LogLevel.Debug,
            Message = message,
            Metadata = metadata ?? new()
        });
    }

    /// <summary>
    /// Logs an info message.
    /// </summary>
    public void Info(string service, string message, Dictionary<string, object?>? metadata = null)
    {
        Log(new LogEntry
        {
            Service = service,
            Level = LogLevel.Info,
            Message = message,
            Metadata = metadata ?? new()
        });
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public void Warn(string service, string message, Dictionary<string, object?>? metadata = null)
    {
        Log(new LogEntry
        {
            Service = service,
            Level = LogLevel.Warn,
            Message = message,
            Metadata = metadata ?? new()
        });
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public void Error(string service, string message, Dictionary<string, object?>? metadata = null)
    {
        Log(new LogEntry
        {
            Service = service,
            Level = LogLevel.Error,
            Message = message,
            Metadata = metadata ?? new()
        });
    }

    /// <summary>
    /// Logs an error message with exception details.
    /// </summary>
    public void Error(string service, string message, Exception exception)
    {
        Log(new LogEntry
        {
            Service = service,
            Level = LogLevel.Error,
            Message = message,
            Metadata = new Dictionary<string, object?>
            {
                ["error"] = SerializeException(exception)
            }
        });
    }

    /// <summary>
    /// Logs a critical message.
    /// </summary>
    public void Critical(string service, string message, Dictionary<string, object?>? metadata = null)
    {
        Log(new LogEntry
        {
            Service = service,
            Level = LogLevel.Critical,
            Message = message,
            Metadata = metadata ?? new()
        });
    }

    /// <summary>
    /// Logs a critical message with exception details.
    /// </summary>
    public void Critical(string service, string message, Exception exception)
    {
        Log(new LogEntry
        {
            Service = service,
            Level = LogLevel.Critical,
            Message = message,
            Metadata = new Dictionary<string, object?>
            {
                ["error"] = SerializeException(exception)
            }
        });
    }

    #endregion

    #region Flush

    /// <summary>
    /// Flushes buffered logs to the LogTide API.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        List<LogEntry> logsToSend;
        
        lock (_bufferLock)
        {
            if (_buffer.Count == 0) return;
            
            logsToSend = new List<LogEntry>(_buffer);
            _buffer.Clear();
        }

        await SendLogsWithRetryAsync(logsToSend, cancellationToken);
    }

    private async Task SendLogsWithRetryAsync(List<LogEntry> logs, CancellationToken cancellationToken)
    {
        var attempt = 0;
        var delay = _options.RetryDelayMs;
        Exception? lastException = null;

        while (attempt <= _options.MaxRetries)
        {
            try
            {
                // Check circuit breaker
                if (!_circuitBreaker.CanAttempt())
                {
                    if (_options.Debug)
                    {
                        Console.WriteLine("[LogTide] Circuit breaker OPEN, skipping send");
                    }

                    lock (_metricsLock)
                    {
                        _metrics.LogsDropped += logs.Count;
                        _metrics.CircuitBreakerTrips++;
                    }
                    
                    throw new CircuitBreakerOpenException();
                }

                var stopwatch = Stopwatch.StartNew();
                await SendLogsAsync(logs, cancellationToken);
                stopwatch.Stop();

                // Record success
                _circuitBreaker.RecordSuccess();
                UpdateLatency(stopwatch.Elapsed.TotalMilliseconds);

                lock (_metricsLock)
                {
                    _metrics.LogsSent += logs.Count;
                }

                if (_options.Debug)
                {
                    Console.WriteLine($"[LogTide] Sent {logs.Count} logs ({stopwatch.ElapsedMilliseconds}ms)");
                }

                return;
            }
            catch (CircuitBreakerOpenException)
            {
                break;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;
                _circuitBreaker.RecordFailure();

                lock (_metricsLock)
                {
                    _metrics.Errors++;
                    if (attempt <= _options.MaxRetries)
                    {
                        _metrics.Retries++;
                    }
                }

                if (attempt > _options.MaxRetries)
                {
                    if (_options.Debug)
                    {
                        Console.WriteLine($"[LogTide] Failed to send logs after {attempt} attempts: {ex.Message}");
                    }
                    break;
                }

                if (_options.Debug)
                {
                    Console.WriteLine($"[LogTide] Retry {attempt}/{_options.MaxRetries} in {delay}ms: {ex.Message}");
                }

                await Task.Delay(delay, cancellationToken);
                delay *= 2; // Exponential backoff
            }
        }

        // All retries failed
        lock (_metricsLock)
        {
            _metrics.LogsDropped += logs.Count;
        }

        if (_circuitBreaker.State == CircuitState.Open)
        {
            lock (_metricsLock)
            {
                _metrics.CircuitBreakerTrips++;
            }
        }
    }

    private async Task SendLogsAsync(List<LogEntry> logs, CancellationToken cancellationToken)
    {
        var serializableLogs = logs.Select(SerializableLogEntry.FromLogEntry).ToList();
        var payload = new { logs = serializableLogs };
        var json = JsonSerializer.Serialize(payload, JsonConfig.Options);
        
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("/api/v1/ingest", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException((int)response.StatusCode, errorBody);
        }
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Queries logs with filters.
    /// </summary>
    public async Task<LogsResponse> QueryAsync(QueryOptions options, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(options.Service))
            queryParams.Add($"service={Uri.EscapeDataString(options.Service)}");
        if (options.Level.HasValue)
            queryParams.Add($"level={options.Level.Value.ToApiString()}");
        if (options.From.HasValue)
            queryParams.Add($"from={Uri.EscapeDataString(options.From.Value.ToString("O"))}");
        if (options.To.HasValue)
            queryParams.Add($"to={Uri.EscapeDataString(options.To.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(options.Query))
            queryParams.Add($"q={Uri.EscapeDataString(options.Query)}");
        queryParams.Add($"limit={options.Limit}");
        queryParams.Add($"offset={options.Offset}");

        var url = $"/api/v1/logs?{string.Join("&", queryParams)}";
        
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException((int)response.StatusCode, errorBody);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<LogsResponse>(json, JsonConfig.Options);
        
        return result ?? new LogsResponse();
    }

    /// <summary>
    /// Gets logs by trace ID.
    /// </summary>
    public async Task<List<LogEntry>> GetByTraceIdAsync(string traceId, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v1/logs/trace/{Uri.EscapeDataString(traceId)}";
        
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException((int)response.StatusCode, errorBody);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<LogsResponse>(json, JsonConfig.Options);
        
        return result?.Logs ?? new List<LogEntry>();
    }

    /// <summary>
    /// Gets aggregated statistics.
    /// </summary>
    public async Task<AggregatedStatsResponse> GetAggregatedStatsAsync(
        AggregatedStatsOptions options, 
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"from={Uri.EscapeDataString(options.From.ToString("O"))}",
            $"to={Uri.EscapeDataString(options.To.ToString("O"))}",
            $"interval={Uri.EscapeDataString(options.Interval)}"
        };

        if (!string.IsNullOrEmpty(options.Service))
            queryParams.Add($"service={Uri.EscapeDataString(options.Service)}");

        var url = $"/api/v1/logs/aggregated?{string.Join("&", queryParams)}";
        
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException((int)response.StatusCode, errorBody);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<AggregatedStatsResponse>(json, JsonConfig.Options);
        
        return result ?? new AggregatedStatsResponse();
    }

    #endregion

    #region Metrics

    /// <summary>
    /// Gets the current SDK metrics.
    /// </summary>
    public ClientMetrics GetMetrics()
    {
        lock (_metricsLock)
        {
            return _metrics.Clone();
        }
    }

    /// <summary>
    /// Resets the SDK metrics.
    /// </summary>
    public void ResetMetrics()
    {
        lock (_metricsLock)
        {
            _metrics = new ClientMetrics();
            _latencyWindow.Clear();
        }
    }

    /// <summary>
    /// Gets the current circuit breaker state.
    /// </summary>
    public CircuitState GetCircuitBreakerState() => _circuitBreaker.State;

    private void UpdateLatency(double latencyMs)
    {
        lock (_metricsLock)
        {
            _latencyWindow.Add(latencyMs);
            
            if (_latencyWindow.Count > 100)
            {
                _latencyWindow.RemoveAt(0);
            }

            if (_latencyWindow.Count > 0)
            {
                _metrics.AvgLatencyMs = _latencyWindow.Average();
            }
        }
    }

    #endregion

    #region Helpers

    private static Dictionary<string, object?> SerializeException(Exception ex)
    {
        var result = new Dictionary<string, object?>
        {
            ["name"] = ex.GetType().Name,
            ["message"] = ex.Message,
            ["stack"] = ex.StackTrace
        };

        if (ex.InnerException != null)
        {
            result["cause"] = SerializeException(ex.InnerException);
        }

        return result;
    }

    #endregion

    #region Dispose

    /// <summary>
    /// Disposes the client and flushes remaining logs.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the client and flushes remaining logs.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        _disposed = true;
        await _flushTimer.DisposeAsync();
        await FlushAsync();
        _httpClient.Dispose();
        
        if (_options.Debug)
        {
            Console.WriteLine("[LogTide] Client disposed");
        }
        
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _disposed = true;
            _flushTimer.Dispose();
            FlushAsync().GetAwaiter().GetResult();
            _httpClient.Dispose();
            
            if (_options.Debug)
            {
                Console.WriteLine("[LogTide] Client disposed");
            }
        }
    }

    #endregion
}
