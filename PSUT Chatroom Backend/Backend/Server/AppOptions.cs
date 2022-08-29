using Npgsql;

namespace Server
{
    public class AppOptions
    {
        public const string SectionName = "App";
        public string DbName { get; set; }
        public string PostgresUserId { get; set; }
        public string PostgresPassword { get; set; }
        public string PostgresServerAddress { get; set; }
        public int PostgresMaxAutoPrepare { get; set; }
        public string PostgresDefaultDbName { get; set; }

        /// <summary>
        /// Created so we could save files on another server and use multiple containers at the same time with a web server like nginx.
        /// </summary>
        public string DataSaveDirectory { get; set; }
        public string UniversitySystemAddress { get; set; }
        public string UniversityEmailDomain { get; set; }
        public string FilesEncryptionKey { get; set; }
        public string DbEncryptionKey { get; set; }
        public string DbDecryptionKey { get; set; }

        private string BuildConnectionString(string? dbName)
        {
            NpgsqlConnectionStringBuilder builder = new()
            {
                ApplicationName = "UniChatAppServer",
                Database = dbName,
                Host = PostgresServerAddress,
                Username = PostgresUserId,
                Password = PostgresPassword,
                MaxAutoPrepare = PostgresMaxAutoPrepare,
                AutoPrepareMinUsages = 2,
                IncludeErrorDetail = true
            };
            return builder.ConnectionString;
        }

        public string BuildAppConnectionString() => BuildConnectionString(DbName);

        public string BuildPostgresConnectionString() => BuildConnectionString(PostgresDefaultDbName);
    }
}