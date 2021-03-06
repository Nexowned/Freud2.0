﻿#region DIRECTIVES

using Newtonsoft.Json;
using static Freud.Database.Db.DatabaseContextBuilder;

#endregion DIRECTIVES

namespace Freud.Database.Db
{
    public sealed class DatabaseConfiguration
    {
        [JsonProperty("database")]
        public string DatabaseName { get; set; }

        [JsonProperty("provider")]
        public DatabaseProvider Provider { get; set; }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonIgnore]
        public static DatabaseConfiguration Default => new DatabaseConfiguration
        {
            DatabaseName = "Freud",
            Provider = DatabaseProvider.SQLite,
            Hostname = "localhost",
            Password = "dev2019",
            Port = 5000,
            Username = ""
        };
    }
}
