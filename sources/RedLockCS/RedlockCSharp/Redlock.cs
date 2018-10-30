using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedlockCSharp
{
    public class Redlock
    {
        public struct Lock
        {
            public string Resource { get; }
            public string Value { get; }
            public TimeSpan Ttl { get; }
            public DateTime LockAt { get; }

            public Lock(string resource, string val) : this(resource, val, new DateTime(), TimeSpan.Zero) { }

            public Lock(string resource, string val, DateTime lockAt, TimeSpan validity)
            {
                Resource = resource;
                Value = val;
                LockAt = lockAt;
                Ttl = validity;
            }
        }

        const string lua_Unlock = 
@"if redis.call(""get"",KEYS[1]) == ARGV[1] then
    return redis.call(""del"",KEYS[1])
else
    return 0
end";

        private const double ClockDriveFactor = 0.01;
        private readonly IList<Func<Task<ConnectionMultiplexer>>> _connections;
        private int Quorum => (_connections.Count / 2) + 1;

        public Redlock(IEnumerable<Func<Task<ConnectionMultiplexer>>> connections)
        {
            _connections = connections.ToList().AsReadOnly();
        }

        public async Task<Lock?> LockAsync(string resource, TimeSpan ttl, int retry = 3, int retryDelay = 200)
        {
            var val = CreateUniqueLockId();
            Lock? lockObject = null;
            await fn_Retry(retry, retryDelay, async () =>
            {
                try
                {
                    var n = 0;
                    var startTime = DateTime.Now;

                    // Use keys
                    await for_each_redis_registered(
                        async connection =>
                        {
                            if (await LockInstance(connection, resource, val, ttl).ConfigureAwait(false))
                                Interlocked.Increment(ref n); //n++;
                        }
                    ).ConfigureAwait(false);

                    /*
                     * Add 2 milliseconds to the drift to account for Redis expires
                     * precision, which is 1 milliescond, plus 1 millisecond min drift 
                     * for small TTLs.        
                     */
                    var drift = Convert.ToInt32((ttl.TotalMilliseconds * ClockDriveFactor) + 2);
                    var now = DateTime.Now;
                    var validityTime = ttl - (now - startTime) - new TimeSpan(0, 0, 0, 0, drift);

                    if (n >= Quorum && validityTime.TotalMilliseconds > 0)
                    {
                        lockObject = new Lock(resource, val, now, validityTime);
                        return true;
                    }
                    await for_each_redis_registered(connection => UnlockInstance(connection, resource, val)).ConfigureAwait(false);
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }).ConfigureAwait(false);

            return lockObject;
        }

        public Task UnlockAsync(Lock lockObject)
        {
            return for_each_redis_registered(connection => UnlockInstance(connection, lockObject.Resource, lockObject.Value));
        }

        //TODO: Refactor passing a ConnectionMultiplexer
        private static async Task<bool> LockInstance(ConnectionMultiplexer connection, string resource, string val, TimeSpan ttl)
        {
            try
            {
                return await connection.GetDatabase().StringSetAsync(resource, val, ttl, When.NotExists, CommandFlags.DemandMaster).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return false;
            }
        }

        //TODO: Refactor passing a ConnectionMultiplexer
        private static Task UnlockInstance(ConnectionMultiplexer connection, string resource, string val)
        {
            RedisKey[] key = { resource };
            RedisValue[] values = { val };
            var redis = connection;
            return redis.GetDatabase().ScriptEvaluateAsync(lua_Unlock, key, values);
        }

        private Task for_each_redis_registered(Func<ConnectionMultiplexer, Task> action)
        {
            return Task.WhenAll(_connections.Select(connection => do_redis(connection, action)));
        }
		
		private static async Task do_redis(Func<Task<ConnectionMultiplexer>> getConnection, Func<ConnectionMultiplexer, Task> action)
		{
			await action(await getConnection().ConfigureAwait(false)).ConfigureAwait(false);
		}
		
        private static async Task<bool> fn_Retry(int retryCount, int retryDelay, Func<Task<bool>> action)
        {
            var rnd = new Random();
            var currentRetry = 0;

            while (currentRetry++ < retryCount)
            {
                if (await action().ConfigureAwait(false)) return true;
                await Task.Delay(rnd.Next(retryDelay)).ConfigureAwait(false);
            }

            return false;
        }

        private static string CreateUniqueLockId()
        {
            return Guid.NewGuid().ToString("n");
        }
    }
}