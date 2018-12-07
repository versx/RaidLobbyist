namespace T.Data.Models
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class Lobby
    {
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

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonIgnore]
        public bool IsExpired => CreatedAt.AddHours(2) < DateTime.Now;

        public Lobby()
        {
            CreatedAt = DateTime.MaxValue;
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