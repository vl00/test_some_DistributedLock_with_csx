#load "./refs.csx"

using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

var conns = new[] 
{
	new RedLockEndPoint(new DnsEndPoint("localhost", 6379))
	{
		ConnectionTimeout = 2000,
		SyncTimeout  = 2000,
		RedisDatabase = 2,
	},
	new RedLockEndPoint(new DnsEndPoint("localhost", 6379))
	{
		ConnectionTimeout = 2000,
		SyncTimeout  = 2000,
		RedisDatabase = 3,
	},
	new RedLockEndPoint(new DnsEndPoint("localhost", 6379))
	{
		ConnectionTimeout = 2000,
		SyncTimeout  = 2000,
		RedisDatabase = 4,
	},
};
var lockFactory = new RedLockNetLockFactory(conns);
var test = new Test(lockFactory);
await test.Run();
