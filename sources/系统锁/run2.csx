#load "./refs.csx"

using Medallion.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

var myLock = new SystemDistributedLock("SystemLock", TimeSpan.FromSeconds(3));

using (myLock.Acquire(TimeSpan.FromSeconds(5)))
{
	Console.WriteLine($"held lock! at {DateTime.Now}");
	Console.ReadLine();
}
Console.WriteLine("release lock!");