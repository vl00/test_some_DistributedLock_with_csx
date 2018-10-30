using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

static readonly object _sync_log = new object();
static void Log(object str)
{
	lock (_sync_log) 
		Console.WriteLine(str);
}

class N
{
    int i;

    public int Increment()
    {
        return Interlocked.Increment(ref i);
    }

    public override string ToString()
    {
        var _i = Interlocked.Add(ref i, 0);
        return _i.ToString();
    }
}

class TF
{
	readonly object _o = new object();
    readonly Dictionary<string, object> c_ls = new Dictionary<string, object>();
    int ci = 0, _miss = 0, _a = 0, _err = 0;
	
	bool _find_(string ck)
	{
		//lock (_o)
		{
			return c_ls.ContainsKey(ck);
		}
	}
	
	void _add_((string ck, object v) o)
	{
		//lock (_o)
		{
			c_ls[o.ck] = o.v;
		}
	}
	
    public async Task<bool> _api_(DateTime st, ILock lck, object _i, string ck, int delay = 0)
    {
		Interlocked.Increment(ref ci);
        if (delay > 0) await Task.Delay(delay);
        try
        {
            if (_find_(ck))
            {
                var _ci2 = Interlocked.Increment(ref _miss);
                //Log($"{_i}:{_ci2}:{nameof(c_ls)} has {ck}");
                return false;
            }

            var a = Interlocked.Increment(ref _a);
            Log($"{_i}:call c_ls not has {ck},  {st.ToString("mm.ss.fff")},{lck.IsAvailable}{lck.LockAtTime?.ToString("mm.ss.fff")} lckid={lck.ID}");
            _add_((ck, _i));
            return true;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _err);
            throw ex;
        }
    }

    public override string ToString()
    {
		var sb = new StringBuilder($"tf::c_ls={c_ls.Count()},{c_ls.GroupBy(_ => _.Key).Count()} ci={ci} _miss={_miss} _a={_a} _err={_err}\n");
		foreach (var (key, v) in c_ls)
		{
			sb.AppendLine($"{key}={v};");
		}
        return sb.ToString();
    }
}

class Test
{
	ILockFactory lockFactory;

	public Test(ILockFactory lockFactory)
	{
		this.lockFactory = lockFactory;
	}
	
	public async Task Run()
	{
		while (true)
        {
            var tasks = new Task[10000];
            var _TF = new TF();
            var _n0 = new N();
            var _n = new N();
            var _n2 = new N();
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew(async (_i) =>
                {
                    await Task.Delay(101);
                    var k = $"{new Random().Next(0, 10)}"; //[,)
                    //var k = "1";
                    {
                        _n0.Increment();
                        var now = DateTime.Now;
                        using (var lck = await lockFactory.LockAsync(k, ttl: 10000))
                        {
                            if (lck?.IsAvailable == true)
                            {
                                await _TF._api_(now, lck, _i, k);
                            }
                            else
                            {
                                _n.Increment();
                                //Log($"{(lck == null ? "null" : "false")} & i={_i} k={k}");
                            }
                        }
                        _n2.Increment();
                    }
                }, i);
            }
            await Task.WhenAll(tasks);
            await Task.Delay(1000 * 4);
            Log(_TF.ToString());
            Log($"n0={_n0} n={_n} n2={_n2}");
            Console.ReadLine();
            Log(_TF.ToString());
            Log($"n0={_n0} n={_n} n2={_n2}");
            Console.ReadLine();
        }
	}
}