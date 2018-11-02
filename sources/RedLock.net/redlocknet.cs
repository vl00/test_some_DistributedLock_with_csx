using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class RedLockNetLock : ILock
{
    IRedLock _redLock;

    public RedLockNetLock(IRedLock redLock, int ttl)
    {
        _redLock = redLock;
        Ttl = ttl;
        if (redLock.IsAcquired) LockAtTime = DateTime.Now;
    }

    public string CK => _redLock.Resource;

    public int Ttl { get; }

    public string ID => _redLock.LockId;

    public DateTime? LockAtTime { get; }

    public bool IsAvailable => _redLock.IsAcquired;

    public void Dispose()
    {
        if (_redLock == null) return;
        var rl = Interlocked.Exchange(ref _redLock, null);
        rl?.Dispose();
    }
    
    public Task DisposeAsync()
    {
		Dispose();
		return Task.CompletedTask;
    }
}

public class RedLockNetLockFactory : ILockFactory
{
    IDistributedLockFactory _factory;

    public RedLockNetLockFactory(IList<RedLockEndPoint> endPoints)
        : this(RedLockFactory.Create(endPoints))
    { }

    public RedLockNetLockFactory(IList<RedLockMultiplexer> existingMultiplexers)
        : this(RedLockFactory.Create(existingMultiplexers))
    { }

    public RedLockNetLockFactory(IDistributedLockFactory factory)
    {
        _factory = factory;
    }

    public async Task<ILock> LockAsync(string ck, int ttl = 5000, int retry = 2, int retryDelay = 1500)
    {
        var rl = await _factory.CreateLockAsync(ck, TimeSpan.FromMilliseconds(ttl),
            TimeSpan.FromMilliseconds(retry * retryDelay), TimeSpan.FromMilliseconds(retryDelay)).ConfigureAwait(false);

        return new RedLockNetLock(rl, ttl);
    }

    public void Dispose()
    {
        _factory = null;
    }
}

