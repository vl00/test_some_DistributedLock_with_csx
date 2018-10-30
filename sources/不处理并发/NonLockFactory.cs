using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class NonLockFactory : ILockFactory
{
    private static readonly string _cck = $"{DateTime.Now.Ticks}.{Guid.NewGuid().ToString("n")}";

    class Lock : ILock
    {
        public string CK { get; }
        public int Ttl { get; }
        public string ID { get; private set; }
        public DateTime? LockAtTime { get; private set; }

        public bool IsAvailable => true;

        internal Lock(string ck)
        {
            this.CK = ck;
            this.Ttl = int.MaxValue;
            ID = _cck;
        }

        public void Dispose() { }
        public Task DisposeAsync() => Task.CompletedTask;
    }

    public async Task<ILock> LockAsync(string ck, int ttl = 5000, int retry = 2, int retryDelay = 1000)
    {
        return new Lock(ck);
    }

    public void Dispose()
    {
        //...
    }
}
