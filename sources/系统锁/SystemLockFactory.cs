using Medallion.Threading;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// for windows
class SystemLockFactory : ILockFactory
{
    class Lock : ILock
    {
		SystemDistributedLock lck;
		TimeSpan ts;
		IDisposable _d;
		bool ok;
    
        public string CK { get; }
        public string ID { get; }
        public bool IsAvailable => lck != null && ok;
        
        public int Ttl { get; } 
        public DateTime? LockAtTime { get; private set; }

        internal Lock(string ck, SystemDistributedLock lck, int ttl, TimeSpan ts)
        {
            this.CK = ck;
            this.Ttl = ttl;
            ID = Guid.NewGuid().ToString("n");
            this.lck = lck;
            this.ts = ts;
        }

        public void Dispose() 
        { 
			var _lck = Interlocked.Exchange(ref lck, null);
            if (_lck == null) return;
			ok = false;
			_d?.Dispose();
			_d = null;
        }
        
        public Task DisposeAsync() 
        {
			Dispose();
			return Task.CompletedTask;
		}
        
        internal async Task Acquire()
        {
			try 
			{ 
				_d = await lck.AcquireAsync(ts); 
				ok = true;
				LockAtTime = DateTime.Now;
				//auto extend
			}
			catch 
			{
				Dispose();
			}
        }
    }

    public async Task<ILock> LockAsync(string ck, int ttl = 20000, int retry = 20, int retryDelay = 1000)
    {
		var l = new SystemDistributedLock(ck, TimeSpan.FromMilliseconds(retryDelay));
        var lck = new Lock(ck, l, ttl, TimeSpan.FromMilliseconds(retry * retryDelay));
        await lck.Acquire();
        return lck;
    }

    public void Dispose()
    {
        //...
    }
}
