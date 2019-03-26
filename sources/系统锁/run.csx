#load "./refs.csx"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

var lockFactory = new SystemLockFactory();
var test = new Test(lockFactory);
await test.Run();
