namespace T.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using T.Diagnostics;

    using Newtonsoft.Json;

    public class Database
    {
        #region Constants

        const string PokemonFileName = "pokemon.json";
        const string MovesetsFileName = "moves.json";

        #endregion

        #region Variables

        private static readonly IEventLogger _logger = EventLogger.GetLogger();

        #endregion

        #region Singleton

        private static Database _instance;
        public static Database Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Database();
                }

                return _instance;
            }
        }

        #endregion

        #region Properties

        public Dictionary<int, PokemonInfo> Pokemon { get; set; }

        public Dictionary<int, MovesetModel> Movesets { get; }

        #endregion

        #region Constructor

        public Database()
        {
            Pokemon = LoadInit<Dictionary<int, PokemonInfo>>(Path.Combine("Data", PokemonFileName), typeof(Dictionary<int, PokemonInfo>));
            Movesets = LoadInit<Dictionary<int, MovesetModel>>(Path.Combine("Data", MovesetsFileName), typeof(Dictionary<int, MovesetModel>));
        }

        #endregion

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

    public class PokemonInfo
    {
        //TODO: Parse id
        //public string TemplateId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("types")]
        public List<PokemonType> Types { get; set; }

        [JsonProperty("baseStats")]
        public BaseStats BaseStats { get; set; }

        [JsonProperty("quickMoves")]
        public List<string> QuickMoves { get; set; }

        [JsonProperty("cinematicMoves")]
        public List<string> CinematicMoves { get; set; }

        [JsonProperty("evolutions")]
        public List<string> Evolutions { get; set; }

        [JsonProperty("familyId")]
        public string FamilyId { get; set; }

        [JsonProperty("candyToEvolve")]
        public int CandyToEvolve { get; set; }

        [JsonProperty("kmBuddyDistance")]
        public double KmBuddyDistance { get; set; }

        [JsonProperty("evolutionBranch")]
        public List<EvolutionBranch> EvolutionBranch { get; set; }
    }

    public class BaseStats
    {
        [JsonProperty("attack")]
        public int Attack { get; set; }

        [JsonProperty("defense")]
        public int Defense { get; set; }

        [JsonProperty("stamina")]
        public int Stamina { get; set; }

        [JsonProperty("captureRate")]
        public double CaptureRate { get; set; }

        [JsonProperty("fleeRate")]
        public double FleeRate { get; set; }

        [JsonProperty("legendary")]
        public bool Legendary { get; set; }

        [JsonProperty("generation")]
        public int Generation { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }
    }

    public class EvolutionBranch
    {
        [JsonProperty("evolution")]
        public string Evolution { get; set; }

        [JsonProperty("candy_cost")]
        public int CandyCost { get; set; }
    }

    public enum PokemonType
    {
        None = 0,
        Normal,
        Fighting,
        Flying,
        Poison,
        Ground,
        Rock,
        Bug,
        Ghost,
        Steel,
        Fire,
        Water,
        Grass,
        Electric,
        Psychic,
        Ice,
        Dragon,
        Dark,
        Fairy
    }

    public class MovesetModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("damage")]
        public int Damage { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("energy")]
        public int Energy { get; set; }

        [JsonProperty("dps")]
        public double Dps { get; set; }
    }
}