using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILock : IDisposable
{
    string CK { get; }
    int Ttl { get; }
    string ID { get; }
    DateTime? LockAtTime { get; }

    bool IsAvailable { get; }
    
    Task DisposeAsync();
}

public interface ILockFactory : IDisposable
{
    Task<ILock> LockAsync(string ck, int ttl = 5000, int retry = 2, int retryDelay = 1000);
}
