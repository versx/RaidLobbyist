namespace RaidLobbyist.Data.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class Lobby
    {
        public static readonly DateTime DefaultCreatedAt = DateTime.MaxValue.Subtract(new TimeSpan(2, 0, 0));

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("gymName")]
        public string GymName { get; set; }

        [JsonProperty("gym")]
        public Gym Gym { get; set; }

        [JsonProperty("users")]
        public List<User> Users { get; set; }

        [JsonProperty("channelName")]
        public string ChannelName
        {
            get
            {
                if (Gym == null)
                    return $"{City}_{GymName.Replace(" ", "-")}";

                var isEgg = Gym.RaidPokemonId == 0 && Gym.RaidLevel > 0;
                var channelName = $"{City}_{(isEgg ? $"lvl{Gym.RaidLevel}egg" : Database.Pokemon[Gym.RaidPokemonId].Name)}_{Gym.Name}";
                return channelName.Replace(" ", "-").ToLower();
            }
        }

        [JsonProperty("startedBy")]
        public string StartedBy { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonIgnore]
        public bool IsExpired => CreatedAt.AddHours(2).AddMinutes(10) < DateTime.Now;

        public Lobby()
        {
            CreatedAt = DefaultCreatedAt;
            Users = new List<User>();
        }

        public Lobby(string city, string name)
            : this()
        {
            City = city;
            GymName = name;
        }

        public override string ToString()
        {
            return GymName;
        }
    }
}