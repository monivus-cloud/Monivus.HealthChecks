namespace Monivus.HealthChecks.MongoDb
{
    /// <summary>
    /// Provides configuration options for the MongoDB health check.
    /// </summary>
    public class MongoDbHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the MongoDB connection string or the name of a connection string.
        /// If omitted, an <see cref="MongoDB.Driver.IMongoClient"/> registered in DI will be used.
        /// </summary>
        public string? ConnectionStringOrName { get; set; }

        /// <summary>
        /// Gets or sets the MongoDB database name to target for the ping command.
        /// Defaults to "admin".
        /// </summary>
        public string DatabaseName { get; set; } = "admin";

        /// <summary>
        /// Gets or sets the optional ping latency threshold in milliseconds.
        /// </summary>
        public int? PingLatencyThresholdMs { get; set; }
    }
}
