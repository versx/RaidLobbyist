namespace T.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [JsonObject("raidLobby")]
    public class RaidLobby
    {
        [JsonProperty("originalRaidMessageId")]
        public ulong OriginalRaidMessageId { get; set; }

        [JsonProperty("originalRaidMessageChannelId")]
        public ulong OriginalRaidMessageChannelId { get; set; }

        [JsonProperty("lobbyMessageId")]
        public ulong LobbyMessageId { get; set; }

        [JsonProperty("usersComing")]
        public Dictionary<ulong, RaidLobbyUser> UsersComing { get; set; }

        [JsonProperty("usersReady")]
        public Dictionary<ulong, RaidLobbyUser> UsersReady { get; set; }

        [JsonProperty("started")]
        public DateTime Started { get; set; }

        [JsonIgnore]
        public bool IsExpired
        {
            get
            {
                return Started.AddHours(1) <= DateTime.Now;
            }
        }

        public RaidLobby()
        {
            UsersComing = new Dictionary<ulong, RaidLobbyUser>();
            UsersReady = new Dictionary<ulong, RaidLobbyUser>();
        }
    }
}