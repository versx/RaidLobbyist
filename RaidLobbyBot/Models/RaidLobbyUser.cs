namespace T.Models
{
    using System;

    using Newtonsoft.Json;

    [JsonObject("raidLobbyUser")]
    public class RaidLobbyUser
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("eta")]
        public RaidLobbyEta Eta { get; set; }

        [JsonIgnore]
        public DateTime EtaStart { get; set; }

        [JsonProperty("players")]
        public int Players { get; set; }

        public RaidLobbyUser()
        {
            EtaStart = DateTime.MinValue;
        }
    }
}