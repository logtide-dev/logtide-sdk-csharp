using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using LogWard.SDK.Models;

namespace LogWard.SDK.Middleware;

/// <summary>
/// Extension methods for adding LogWard to ASP.NET Core applications.
/// </summary>
public static class LogWardExtensions
{
    /// <summary>
    /// Adds LogWard client as a singleton service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Client configuration options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLogWard(
        this IServiceCollection services, 
        ClientOptions options)
    {
        var client = new LogWardClient(options);
        services.AddSingleton(client);
        return services;
    }

    /// <summary>
    /// Adds LogWard client using a factory function.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsFactory">Factory function for client options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddLogWard(
        this IServiceCollection services, 
        Func<IServiceProvider, ClientOptions> optionsFactory)
    {
        services.AddSingleton(sp =>
        {
            var options = optionsFactory(sp);
            return new LogWardClient(options);
        });
        return services;
    }

    /// <summary>
    /// Adds LogWard HTTP request/response logging middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="optionsAction">Action to configure middleware options.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseLogWard(
        this IApplicationBuilder app,
        Action<LogWardMiddlewareOptions> optionsAction)
    {
        var options = new LogWardMiddlewareOptions
        {
            Client = app.ApplicationServices.GetRequiredService<LogWardClient>()
        };
        
        optionsAction(options);
        
        return app.UseMiddleware<LogWardMiddleware>(options);
    }

    /// <summary>
    /// Adds LogWard HTTP request/response logging middleware with the default client.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="serviceName">Service name to use in logs.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseLogWard(
        this IApplicationBuilder app,
        string serviceName = "aspnet-api")
    {
        var client = app.ApplicationServices.GetRequiredService<LogWardClient>();
        
        var options = new LogWardMiddlewareOptions
        {
            Client = client,
            ServiceName = serviceName
        };
        
        return app.UseMiddleware<LogWardMiddleware>(options);
    }

    /// <summary>
    /// Adds LogWard HTTP request/response logging middleware with explicit options.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="options">Middleware options.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseLogWard(
        this IApplicationBuilder app,
        LogWardMiddlewareOptions options)
    {
        return app.UseMiddleware<LogWardMiddleware>(options);
    }
}
