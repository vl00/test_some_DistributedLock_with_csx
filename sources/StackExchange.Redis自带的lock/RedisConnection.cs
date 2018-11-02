using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

public class RedisConnection
{
    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
    readonly ConfigurationOptions _options;
    ConnectionMultiplexer _connection;
	
	public RedisConnection(string conStr) : this(ConfigurationOptions.Parse(conStr)) { }

    public RedisConnection(ConfigurationOptions options)
    {
        _options = options ?? throw new Exception("RedisConnection no option");
    }

    public async Task<ConnectionMultiplexer> TryOpenAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_connection == null || (!_connection.IsConnecting && !_connection.IsConnected))
            {
                _connection = await ConnectionMultiplexer.ConnectAsync(_options).ConfigureAwait(false);
            }
            return _connection;              
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task CloseAsync(bool allowCommandsToComplete = true)
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_connection?.IsConnected == true)
            {
                await _connection.CloseAsync(allowCommandsToComplete).ConfigureAwait(false);
                _connection = null;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }
    
    public ConnectionMultiplexer TryOpen()
    {
        _connectionLock.Wait();
        try
        {
            if (_connection == null || (!_connection.IsConnecting && !_connection.IsConnected))
            {
                _connection = ConnectionMultiplexer.Connect(_options);
            }
            return _connection;              
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public void Close(bool allowCommandsToComplete = true)
    {
        _connectionLock.Wait();
        try
        {
            if (_connection?.IsConnected == true)
            {
                _connection.Close(allowCommandsToComplete);
                _connection = null;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }
}
