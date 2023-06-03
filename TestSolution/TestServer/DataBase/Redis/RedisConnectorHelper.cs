using StackExchange.Redis;
using TestServer.Helper;


public static class RedisConnectorHelper
{
    private static ConnectionMultiplexer _connection;

    private static readonly object _lock = new object();

    /// <summary>
    /// 0 ~ 15까지 있는 데이터 베이스 중 어디에 저장할지
    /// </summary>
    public enum DataBaseId
    {
        ServerStatus = 0,
        Session = 1,
        User = 2,
    }

    //ConnectionMultiplexer는 thread-safe하며, 
    // 내부적으로 연결 재사용, 요청 멀티플렉싱, 연결 장애 처리 등의 작업을 수행합니다.
    // 따라서, 매번 새로운 연결을 생성하는 대신에 ConnectionMultiplexer 인스턴스를 
    // 재사용하면 이러한 기능들을 효과적으로 활용할 수 있습니다.
    public static ConnectionMultiplexer Connection
    {
        get
        {
            if (_connection == null || !_connection.IsConnected)
            {
                lock (_lock)
                {
                    if (_connection == null || !_connection.IsConnected)
                    {
                        var connectionString = ConfigInstanceHelper.Instance.RedisConnectString;
                        _connection = ConnectionMultiplexer.Connect(connectionString);
                    }
                }
            }

            return _connection;
        }
    }
}
