# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-01-13

### Added

- Initial release of LogTide .NET SDK
- Automatic batching with configurable size and interval
- Retry logic with exponential backoff
- Circuit breaker pattern for fault tolerance
- Max buffer size with drop policy
- Query API for searching and filtering logs
- Aggregated statistics API
- Trace ID context for distributed tracing
- Global metadata support
- Structured error serialization
- Internal metrics tracking
- Logging methods: Debug, Info, Warn, Error, Critical
- Thread-safe operations
- ASP.NET Core middleware for auto-logging HTTP requests
- Dependency injection support
- Full async/await support
- Support for .NET 6.0, 7.0, and 8.0

[0.1.0]: https://github.com/logtide-dev/logtide-sdk-csharp/releases/tag/v0.1.0
