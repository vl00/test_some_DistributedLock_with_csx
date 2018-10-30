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
var test = new Test(lockFactory);
await test.Run();
