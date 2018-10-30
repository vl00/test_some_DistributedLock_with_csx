using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class XckLockFactory : ILockFactory
{
    private readonly ConcurrentDictionary<string, Lock> kvs = new ConcurrentDictionary<string, Lock>();

    class Lock : ILock
    {
        XckLockFactory _this;
        CancellationTokenRegistration? _d;
        long _isAvail = 1L;

        public string CK { get; }
        public int Ttl { get; }
        public string ID { get; private set; }
        public DateTime? LockAtTime { get; private set; }

        public bool IsAvailable
        {
            get
            {
                var a = Interlocked.Read(ref _isAvail);
                return a == 1;
            }
        }

        internal Lock(XckLockFactory _this, string ck, int timeout)
        {
            this._this = _this;
            this.CK = ck;
            this.Ttl = timeout;
            ID = $"__xcklock3:{ck}@{Thread.CurrentThread.ManagedThreadId}@{DateTime.Now.Ticks}.{Guid.NewGuid().ToString("n")}";
        }

        internal void SetTimeout()
        {
            LockAtTime = DateTime.Now;
            if (Ttl != Timeout.Infinite)
            {
                var c = new CancellationTokenSource(Ttl);
                _d = c.Token.Register(o => ((Lock)o).Dispose(), this, false);
            }
        }

        public void Dispose()
        {
            var _this = Interlocked.Exchange(ref this._this, null);
            if (_this == null) return;
            
            Interlocked.Exchange(ref _isAvail, 0L);
            _d?.Dispose();
            _d = null;

            if (_this.kvs.TryGetValue(CK, out var lck))
            {
                if (lck.ID == this.ID)
                {
                    _this.kvs.TryRemove(CK, out _);
                    lck.Dispose();
                }
                else if (!lck.IsAvailable)
                {
                    lck.Dispose();
                }
            }
        }
        
        public Task DisposeAsync()
        {
			Dispose();
			return Task.CompletedTask;
        }
    }

    public async Task<ILock> LockAsync(string ck, int ttl = 5000, int retry = 2, int retryDelay = 1000)
    {
        var lck = new Lock(this, ck, ttl);
        retry = retry < 0 ? 0 : retry;

        for (var i = 0; i <= retry; i++)
        {
            var tmp = kvs.GetOrAdd(ck, lck);
            if (!tmp.IsAvailable)
            {
                tmp.Dispose();
                i--;
            }
            else if (!ReferenceEquals(tmp, lck))
            {
                await Task.Delay(retryDelay).ConfigureAwait(false);
            }
            else
            {
                lck.SetTimeout();
                return lck;
            }
        }

        lck.Dispose();
        return lck;
    }

	public Task<ILock> LockNotTimeoutAsync(string ck, int retry = 2, int retryDelay = 1000)
    {
        return LockAsync(ck, Timeout.Infinite, retry, retryDelay);
    }

    public void Dispose()
    {
        //...
    }
}
