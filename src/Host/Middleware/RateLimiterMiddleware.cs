namespace Helion.Host.Middleware;

using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

public class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TokenBucket _bucket;

    public RateLimiterMiddleware(RequestDelegate next)
    {
        _next = next;
        // Default: 100 capacity, 10 tokens/sec refill
        _bucket = new TokenBucket(100, 10);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_bucket.TryConsume(1))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("Too many requests.");
            return;
        }

        await _next(context);
    }
}

public class TokenBucket
{
    private readonly int _capacity;
    private readonly double _refillRatePerSecond;
    private double _tokens;
    private long _lastRefillTicks;

    public TokenBucket(int capacity, double refillRatePerSecond)
    {
        _capacity = capacity;
        _refillRatePerSecond = refillRatePerSecond;
        _tokens = capacity;
        _lastRefillTicks = DateTime.UtcNow.Ticks;
    }

    public bool TryConsume(int amount)
    {
        long currentTicks = DateTime.UtcNow.Ticks;
        long lastTicks = Interlocked.Exchange(ref _lastRefillTicks, currentTicks);
        
        double timePassed = TimeSpan.FromTicks(currentTicks - lastTicks).TotalSeconds;
        
        // Lock-free atomic update of tokens
        double currentTokens = _tokens;
        double newTokens = Math.Min(_capacity, currentTokens + timePassed * _refillRatePerSecond);
        
        if (newTokens >= amount)
        {
            Interlocked.Exchange(ref _tokens, newTokens - amount);
            return true;
        }
        
        Interlocked.Exchange(ref _tokens, newTokens);
        return false;
    }
}
