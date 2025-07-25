using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DrHan.Application.Extensions;

public static class PerformanceExtensions
{
    /// <summary>
    /// Execute operation with performance monitoring
    /// </summary>
    public static async Task<T> ExecuteWithTimingAsync<T>(
        this ILogger logger,
        Func<Task<T>> operation, 
        string operationName,
        TimeSpan? warningThreshold = null)
    {
        var threshold = warningThreshold ?? TimeSpan.FromMilliseconds(500);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            if (stopwatch.Elapsed > threshold)
            {
                logger.LogWarning("Slow operation detected: {Operation} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)", 
                    operationName, stopwatch.ElapsedMilliseconds, threshold.TotalMilliseconds);
            }
            else
            {
                logger.LogDebug("Operation {Operation} completed in {ElapsedMs}ms", 
                    operationName, stopwatch.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Operation {Operation} failed after {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Create performance scope for tracking multiple operations
    /// </summary>
    public static IDisposable CreatePerformanceScope(this ILogger logger, string scopeName)
    {
        return new PerformanceScope(logger, scopeName);
    }
}

public class PerformanceScope : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _scopeName;
    private readonly Stopwatch _stopwatch;

    public PerformanceScope(ILogger logger, string scopeName)
    {
        _logger = logger;
        _scopeName = scopeName;
        _stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Performance scope started: {ScopeName}", _scopeName);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        var elapsed = _stopwatch.ElapsedMilliseconds;
        
        if (elapsed > 1000)
        {
            _logger.LogWarning("Performance scope {ScopeName} took {ElapsedMs}ms", _scopeName, elapsed);
        }
        else
        {
            _logger.LogDebug("Performance scope {ScopeName} completed in {ElapsedMs}ms", _scopeName, elapsed);
        }
    }
}

public static class SearchPerformanceConstants
{
    public static readonly TimeSpan FastQueryThreshold = TimeSpan.FromMilliseconds(100);
    public static readonly TimeSpan AcceptableQueryThreshold = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan SlowQueryThreshold = TimeSpan.FromMilliseconds(1000);
    public static readonly TimeSpan CriticalQueryThreshold = TimeSpan.FromMilliseconds(3000);
}