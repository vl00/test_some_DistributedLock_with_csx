using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class RedLockCSLockFactory : ILockFactory
{
    RedlockCSharp.Redlock redlock;

    public RedLockCSLockFactory(IEnumerable<Func<Task<ConnectionMultiplexer>>> connections)
    {
        redlock = new RedlockCSharp.Redlock(connections);
    }

    public async Task<ILock> LockAsync(string ck, int ttl = 5000, int retry = 2, int retryDelay = 1000)
    {
        var lck = await redlock.LockAsync(ck, TimeSpan.FromMilliseconds(ttl), retry, retryDelay).ConfigureAwait(false);
        var ok = lck != null;
        if (!ok) lck = new RedlockCSharp.Redlock.Lock(ck, Guid.NewGuid().ToString());
        return new RedLockCSLock(this, lck.Value, ok);
    }

    public void Dispose()
    {
        redlock = null;
    }

    class RedLockCSLock : ILock
    {
        RedLockCSLockFactory _this;
        RedlockCSharp.Redlock.Lock _redLock;
        bool _isAvailable;

        internal RedLockCSLock(RedLockCSLockFactory _this, RedlockCSharp.Redlock.Lock redLock, bool isAvailable)
        {
            this._this = _this;
            _redLock = redLock;
            Ttl = (int)redLock.Ttl.TotalMilliseconds;
            _isAvailable = isAvailable;
            if (_isAvailable) LockAtTime = redLock.LockAt;
        }

        public string CK => _redLock.Resource;
        public int Ttl { get; }
        public string ID => _redLock.Value;
        public DateTime? LockAtTime { get; }

        public bool IsAvailable => _isAvailable && (DateTime.Now < LockAtTime.Value.AddMilliseconds(Ttl));

        public void Dispose() => DisposeAsync(); //can't wait
        
        public async Task DisposeAsync()
        {
			if (_this == null) return;
            var self = Interlocked.Exchange(ref _this, null);
            if (self == null) return;
            await self.redlock.UnlockAsync(_redLock).ConfigureAwait(false);
        }
    }
}