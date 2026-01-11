using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using LogTide.SDK.Models;

namespace LogTide.SDK.Middleware;

/// <summary>
/// Extension methods for adding LogTide to ASP.NET Core applications.
/// </summary>
public static class LogTideExtensions
{
    /// <summary>
    /// Adds LogTide client as a singleton service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Client configuration options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLogTide(
        this IServiceCollection services, 
        ClientOptions options)
    {
        var client = new LogTideClient(options);
        services.AddSingleton(client);
        return services;
    }

    /// <summary>
    /// Adds LogTide client using a factory function.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsFactory">Factory function for client options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLogTide(
        this IServiceCollection services, 
        Func<IServiceProvider, ClientOptions> optionsFactory)
    {
        services.AddSingleton(sp =>
        {
            var options = optionsFactory(sp);
            return new LogTideClient(options);
        });
        return services;
    }

    /// <summary>
    /// Adds LogTide HTTP request/response logging middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="optionsAction">Action to configure middleware options.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseLogTide(
        this IApplicationBuilder app,
        Action<LogTideMiddlewareOptions> optionsAction)
    {
        var options = new LogTideMiddlewareOptions
        {
            Client = app.ApplicationServices.GetRequiredService<LogTideClient>()
        };
        
        optionsAction(options);
        
        return app.UseMiddleware<LogTideMiddleware>(options);
    }

    /// <summary>
    /// Adds LogTide HTTP request/response logging middleware with the default client.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="serviceName">Service name to use in logs.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseLogTide(
        this IApplicationBuilder app,
        string serviceName = "aspnet-api")
    {
        var client = app.ApplicationServices.GetRequiredService<LogTideClient>();
        
        var options = new LogTideMiddlewareOptions
        {
            Client = client,
            ServiceName = serviceName
        };
        
        return app.UseMiddleware<LogTideMiddleware>(options);
    }

    /// <summary>
    /// Adds LogTide HTTP request/response logging middleware with explicit options.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="options">Middleware options.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseLogTide(
        this IApplicationBuilder app,
        LogTideMiddlewareOptions options)
    {
        return app.UseMiddleware<LogTideMiddleware>(options);
    }
}
