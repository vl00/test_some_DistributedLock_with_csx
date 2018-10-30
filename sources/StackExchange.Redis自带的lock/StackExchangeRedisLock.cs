using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class StackExchangeRedisLockFactory : ILockFactory
{
    Func<Task<ConnectionMultiplexer>> _factory;

    public StackExchangeRedisLockFactory(Func<Task<ConnectionMultiplexer>> factory)
    {
		_factory = factory;
    }

    public async Task<ILock> LockAsync(string ck, int ttl = 5000, int retry = 2, int retryDelay = 1000)
    {
		var db = (await _factory().ConfigureAwait(false)).GetDatabase();
		var id = Guid.NewGuid().ToString("n");
		for (var i = 0; i <= retry; i++)
		{
			if (await db.LockTakeAsync(ck, id, TimeSpan.FromMilliseconds(ttl)).ConfigureAwait(false))
			{
				return new Lock(_factory, true, ck, id, ttl);
			}
			await Task.Delay(retryDelay).ConfigureAwait(false);
		}
        return new Lock(_factory, false, ck, id, -1);
    }

    public void Dispose()
    {
        _factory = null;
    }
    
    class Lock : ILock
	{
		Func<Task<ConnectionMultiplexer>> _factory;
		bool _isAvailable;

		public Lock(Func<Task<ConnectionMultiplexer>> factory, bool isAvailable, string ck, string id, int ttl)
		{
			_factory = factory;
			_isAvailable = isAvailable;
			if (isAvailable) LockAtTime = DateTime.Now;
			Ttl = ttl;
			CK = ck;
			ID = id;
		}

		public string CK { get; }
		public int Ttl { get; }
		public string ID { get; }
		public DateTime? LockAtTime { get; }

		public bool IsAvailable => _isAvailable && (DateTime.Now < LockAtTime.Value.AddMilliseconds(Ttl));
		
		public void Dispose() => DisposeAsync().Wait();
		
		public async Task DisposeAsync()
		{
			if (_factory == null) return;
			var fy = Interlocked.Exchange(ref _factory, null);
			if (fy == null || !_isAvailable) return;
			await (await fy().ConfigureAwait(false)).GetDatabase().LockReleaseAsync(CK, ID).ConfigureAwait(false);
		}
	}
}

