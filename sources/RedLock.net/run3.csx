#load "./refs.csx"

using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

var conns = new List<RedLockMultiplexer>
{
	ConnectionMultiplexer.Connect("127.0.0.1:6379,DefaultDatabase=2,syncTimeout=2000,asyncTimeout=2000"),
	ConnectionMultiplexer.Connect("127.0.0.1:6379,DefaultDatabase=3,syncTimeout=2000,asyncTimeout=2000"),
	ConnectionMultiplexer.Connect("127.0.0.1:6379,DefaultDatabase=4,syncTimeout=2000,asyncTimeout=2000"),
};
var lockFactory = new RedLockNetLockFactory(conns);
using (var lck = await lockFactory.LockAsync("eee", 1000 * 2))
{	
	F();
	if (lck.IsAvailable) 
		await Task.Delay(1000 * 3);
	if (lck.IsAvailable)
		Console.WriteLine("extend ok");
}
Console.ReadLine();

async void F()
{
	var t1 = DateTime.Now;
	using (var lck = await lockFactory.LockAsync("eee", 1000 * 2, 100))
	{
		if (lck.IsAvailable)
			Console.WriteLine($"get ok t1={t1.ToString("mm:ss.fff")} t2={DateTime.Now.ToString("mm:ss.fff")}");
	}
}