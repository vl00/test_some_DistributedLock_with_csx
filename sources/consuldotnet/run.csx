#load "./refs.csx"

using Consul;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

var client = new ConsulClient();
var lockFactory = new ConsulLockFactory(client);

var test = new Test(lockFactory);
await test.Run();
