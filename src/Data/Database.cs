namespace T.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using T.Data.Models;
    using T.Diagnostics;

    using Newtonsoft.Json;

    public static class Database
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger();

        public static Dictionary<int, Pokemon> Pokemon
        {
            get
            {
                return LoadInit<Dictionary<int, Pokemon>>(Path.Combine(Strings.DataFolder, Strings.PokemonFileName));
            }
        }

        public static Dictionary<int, Moveset> Movesets
        {
            get
            {
                return LoadInit<Dictionary<int, Moveset>>(Path.Combine(Strings.DataFolder, Strings.MovesetsFileName));
            }
        }

        public static T LoadInit<T>(string filePath)
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

            return (T)JsonConvert.DeserializeObject(data, typeof(T));
        }
    }
}