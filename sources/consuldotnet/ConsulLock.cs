using Consul;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class ConsulLockFactory : ILockFactory
{
    ConsulClient _client;
	
	public ConsulLockFactory(ConsulClient client)
	{
		_client = client;
	}
	
    class Lock : ILock
    {
        ConsulClient _client;
        LockOptions _o;
        IDistributedLock _lck;
        CancellationToken _ct;
        CancellationTokenRegistration? _d;

        public string CK => _o.Key;
        public int Ttl => (int)_o.SessionTTL.TotalMilliseconds;
        public string ID => _o.SessionName;
        public DateTime? LockAtTime { get; }

        public bool IsAvailable => _lck.IsHeld;

        internal Lock(ConsulClient _client, LockOptions _o, IDistributedLock _lck, CancellationToken _ct)
        {
            this._client = _client;
            this._o = _o;
            this._ct = _ct;
            this._lck = _lck;
            if (_lck.IsHeld) LockAtTime = DateTime.Now;
            if (!_ct.IsCancellationRequested) _d = _ct.Register(o => ((Lock)o).Dispose(), this, false);
        }
		
		public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
		
        public async Task DisposeAsync()
        {
            var client = Interlocked.Exchange(ref this._client, null);
            if (client == null) return;
            _d?.Dispose();
            _d = null;
            try 
            {
				await _lck.Release().ConfigureAwait(false);
				await _lck.Destroy().ConfigureAwait(false);
            }
            catch { }
        }
    }

    public async Task<ILock> LockAsync(string ck, int ttl = 5000, int retry = 2, int retryDelay = 1000)
    {
		var id = Guid.NewGuid();
        var o = new LockOptions(ck)
        {
			Value = id.ToByteArray(),
			SessionName = id.ToString("n"),
			SessionTTL = TimeSpan.FromMilliseconds(ttl),
			LockRetryTime = TimeSpan.FromMilliseconds(retryDelay),
			LockWaitTime = TimeSpan.FromMilliseconds(retry * retryDelay),
        };
        var lck = _client.CreateLock(o);
        var ct = await lck.Acquire(CancellationToken.None).ConfigureAwait(false);
        return new Lock(_client, o, lck, ct);
    }

    public void Dispose()
    {
        _client = null;
    }
}
