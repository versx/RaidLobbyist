namespace T.Data.Models
{
    using ServiceStack.DataAnnotations;

    [Alias("gym")]
    public class Gym
    {
        [Alias("id")]
        public string Id { get; set; }

        [Alias("lat")]
        public double Latitude { get; set; }

        [Alias("lon")]
        public double Longitude { get; set; }

        [Alias("name")]
        public string Name { get; set; }

        [Alias("url")]
        public string Url { get; set; }

        [Alias("last_modified_timestamp")]
        public long LastModifiedTimestamp { get; set; }

        [Alias("raid_end_timestamp")]
        public long RaidEndTimestamp { get; set; }

        [Alias("raid_spawn_timestamp")]
        public long RaidSpawnTimestamp { get; set; }

        [Alias("raid_battle_timestamp")]
        public long RaidBattleTimestamp { get; set; }

        [Alias("updated")]
        public long Updated { get; set; }

        [Alias("raid_pokemon_id")]
        public int RaidPokemonId { get; set; }

        [Alias("guarding_pokemon_id")]
        public int GuardingPokemonId { get; set; }

        [Alias("availble_slots")]
        public int AvailableSlots { get; set; }

        [Alias("team_id")]
        public PokemonTeam Team { get; set; }

        [Alias("raid_level")]
        public int RaidLevel { get; set; }

        [Alias("enabled")]
        public bool Enabled { get; set; }

        [Alias("ex_Raid_eligible")]
        public bool ExRaidEligible { get; set; }

        [Alias("in_battle")]
        public bool InBattle { get; set; }

        [Alias("raid_pokemon_move_1")]
        public int RaidPokemonMove1 { get; set; }

        [Alias("raid_pokemon_move_2")]
        public int RaidPokemonMove2 { get; set; }

        [Alias("raid_pokemon_form")]
        public int RaidPokemonForm { get; set; }

        [Alias("raid_pokemon_cp")]
        public int RaidPokemonCP { get; set; }

        [Alias("raid_is_exclusive")]
        public bool RaidIsExclusive { get; set; }

        [Alias("cell_id")]
        public long CellId { get; set; }

        [Ignore]
        public bool IsEgg => RaidPokemonId == 0 && RaidLevel > 0;

        [Ignore]
        public bool HasRaid => RaidSpawnTimestamp > 0;

        public override string ToString()
        {
            return Name;
        }
    }

    public enum PokemonTeam
    {
        Neutral = 0,
        Mystic,
        Valor,
        Instinct,
        All = ushort.MaxValue
    }
}