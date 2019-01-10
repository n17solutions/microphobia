namespace N17Solutions.Microphobia.Dashboard
{
    public class WebsocketCachingOptions
    {
        /// <summary>
        /// Whether to use the Cache or not
        /// </summary>
        public bool UseCache { get; set; }
        
        /// <summary>
        /// The connection string to connect to the Redis cache instance
        /// </summary>
        public string RedisConnectionString { get; set; }

        /// <summary>
        /// The ChannelPrefix to use within Redis
        /// </summary>
        /// <remarks>Defaults to microphobia - make this unique per microphobia app that uses the cache</remarks>
        public string ChannelPrefix { get; set; } = "microphobia";
    }
}