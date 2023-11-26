﻿namespace Singleton;

public class CacheProcessor : IDisposable
{
    private object lockMembersObject { get; set; }
    private static CacheProcessor _instance { get; set; } = new();
    private Dictionary<string, object> _members { get; set; }
    private CancellationTokenSource? cancelTokenSource;
    private CancellationToken cancelToken;
    
    private int defaultTimeoutSec { get; set; }

    private CacheProcessor()
    {
        _members = new Dictionary<string, object>();
        cancelTokenSource = new CancellationTokenSource();
        cancelToken = cancelTokenSource.Token;
        lockMembersObject = new();

        defaultTimeoutSec = 10;

        RunPeriodicallyAsync(PerformRemoveOldObjects, TimeSpan.FromSeconds(defaultTimeoutSec), cancelToken);
    }

    public static CacheProcessor Instance => _instance;
    
    private Task PerformRemoveOldObjects()
    {
        lock (lockMembersObject)
        {
            var mustRemoveKeys = _members
                .Where(e => (e.Value as CacheObject)?.ExpiresTo < DateTime.Now)
                .Select(e => e.Key);
            foreach (var key in mustRemoveKeys)
            {
                _members.Remove(key);
            }
        }

        return Task.CompletedTask;
    }


    public bool TryGetValueFromCache<T>(string key, out T? result)
    {
        result = default;
        if (_members.TryGetValue(key, out object? res))
        {
            if ((res as CacheObject)?.ExpiresTo >= DateTime.Now)
            {
                result = (T)(res as CacheObject)?.Value;
            }
            else
            {
                lock (lockMembersObject)
                {
                    _members.Remove(key);
                }
            }
        }

        return result != null;
    }

    public bool SaveDataCache<T>(string key, T obj)
    {
        var result = false;
        if (obj != null)
        {
            var cacheObject = new CacheObject
            {
                Value = obj,
                ExpiresTo = DateTime.Now.AddSeconds(defaultTimeoutSec)
            };
            lock (lockMembersObject)
            {
                result = _members.TryAdd(key, cacheObject);
            }
        }

        return result;
    }

    private async Task RunPeriodicallyAsync(
        Func<Task> func,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);
            try
            {
                await func();
            }
            catch (Exception e)
            {
                // пока что игнорируем
            }
        }
    }

    public void Dispose()
    {
        cancelTokenSource?.Dispose();
    }
}