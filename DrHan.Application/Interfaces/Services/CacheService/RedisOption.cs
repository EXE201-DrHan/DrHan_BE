using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.Interfaces.Services.CacheService
{
    public class RedisOptions
    {
        public string ConnectionString { get; set; } = "localhost:6379";
        public string InstanceName { get; set; } = "DrHanApp";
        public int Database { get; set; } = 0;
        public bool AbortOnConnectFail { get; set; } = false;
        public int ConnectTimeout { get; set; } = 5000;
        public int SyncTimeout { get; set; } = 5000;
    }

    public class CacheSettings
    {
        public int DefaultExpirationMinutes { get; set; } = 30;
        public int ShortExpirationMinutes { get; set; } = 5;
        public int LongExpirationMinutes { get; set; } = 120;

        public TimeSpan DefaultExpiration => TimeSpan.FromMinutes(DefaultExpirationMinutes);
        public TimeSpan ShortExpiration => TimeSpan.FromMinutes(ShortExpirationMinutes);
        public TimeSpan LongExpiration => TimeSpan.FromMinutes(LongExpirationMinutes);
    }
}
