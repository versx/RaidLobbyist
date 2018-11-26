namespace T.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;

    using T.Diagnostics;
    using T.Models;

    public class Config
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger();

        #region Properties

        [JsonProperty("guildId")]
        public ulong GuildId { get; set; }

        [JsonProperty("ownerId")]
        public ulong OwnerId { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("raidChannelIdPool")]
        public List<ulong> RaidChannelIdPool { get; set; }

        [JsonProperty("raidLobbiesChannelId")]
        public ulong RaidLobbiesChannelId { get; set; }

        [JsonProperty("lobbies")]
        public Dictionary<ulong, RaidLobby> ActiveLobbies { get; set; }

        #endregion

        public Config()
        {
            ActiveLobbies = new Dictionary<ulong, RaidLobby>();
            RaidChannelIdPool = new List<ulong>();
        }

        public void Save(string filePath)
        {
            var data = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, data);
        }

        public static Config Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Config not loaded because file not found.", filePath);
            }

            return LoadInit<Config>(filePath, typeof(Config));
        }

        public static T LoadInit<T>(string filePath, Type type)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} file not found.", filePath);
            }

            var data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                _logger.Error($"{filePath} database is empty.");
                return default(T);
            }

            return (T)JsonConvert.DeserializeObject(data, type);
        }
    }
}