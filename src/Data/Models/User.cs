namespace T.Data.Models
{
    using Newtonsoft.Json;

    public class User
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("isOtw")]
        public bool IsOnTheWay { get; set; }

        [JsonProperty("isHere")]
        public bool IsHere { get; set; }

        public override string ToString()
        {
            return Username;
        }
    }
}