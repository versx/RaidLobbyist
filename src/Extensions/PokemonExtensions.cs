namespace RaidLobbyist.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DSharpPlus;

    using RaidLobbyist.Data.Models;
    using RaidLobbyist.Extensions;

    public static class PokemonExtensions
    {
        public static string GetPokemonImage(this int pokemonId, string form)
        {
            return string.Format(Strings.PokemonImage, pokemonId, int.TryParse(form, out var formId) ? formId : 0);
        }

        public static List<PokemonType> GetWeaknesses(this PokemonType type)
        {
            var types = Array.Empty<PokemonType>();
            switch (type)
            {
                case PokemonType.Normal:
                    types = new PokemonType[] { PokemonType.Fighting };
                    break;
                case PokemonType.Fighting:
                    types = new PokemonType[] { PokemonType.Flying, PokemonType.Psychic, PokemonType.Fairy };
                    break;
                case PokemonType.Flying:
                    types = new PokemonType[] { PokemonType.Rock, PokemonType.Electric, PokemonType.Ice };
                    break;
                case PokemonType.Poison:
                    types = new PokemonType[] { PokemonType.Ground, PokemonType.Psychic };
                    break;
                case PokemonType.Ground:
                    types = new PokemonType[] { PokemonType.Water, PokemonType.Grass, PokemonType.Ice };
                    break;
                case PokemonType.Rock:
                    types = new PokemonType[] { PokemonType.Fighting, PokemonType.Ground, PokemonType.Steel, PokemonType.Water, PokemonType.Grass };
                    break;
                case PokemonType.Bug:
                    types = new PokemonType[] { PokemonType.Flying, PokemonType.Rock, PokemonType.Fire };
                    break;
                case PokemonType.Ghost:
                    types = new PokemonType[] { PokemonType.Ghost, PokemonType.Dark };
                    break;
                case PokemonType.Steel:
                    types = new PokemonType[] { PokemonType.Fighting, PokemonType.Ground, PokemonType.Fire };
                    break;
                case PokemonType.Fire:
                    types = new PokemonType[] { PokemonType.Ground, PokemonType.Rock, PokemonType.Water };
                    break;
                case PokemonType.Water:
                    types = new PokemonType[] { PokemonType.Grass, PokemonType.Electric };
                    break;
                case PokemonType.Grass:
                    types = new PokemonType[] { PokemonType.Flying, PokemonType.Poison, PokemonType.Bug, PokemonType.Fire, PokemonType.Ice };
                    break;
                case PokemonType.Electric:
                    types = new PokemonType[] { PokemonType.Ground };
                    break;
                case PokemonType.Psychic:
                    types = new PokemonType[] { PokemonType.Bug, PokemonType.Ghost, PokemonType.Dark };
                    break;
                case PokemonType.Ice:
                    types = new PokemonType[] { PokemonType.Fighting, PokemonType.Rock, PokemonType.Steel, PokemonType.Fire };
                    break;
                case PokemonType.Dragon:
                    types = new PokemonType[] { PokemonType.Ice, PokemonType.Dragon, PokemonType.Fairy };
                    break;
                case PokemonType.Dark:
                    types = new PokemonType[] { PokemonType.Fighting, PokemonType.Bug, PokemonType.Fairy };
                    break;
                case PokemonType.Fairy:
                    types = new PokemonType[] { PokemonType.Poison, PokemonType.Steel };
                    break;
            }
            return types.ToList();
        }

        public static string GetWeaknessEmojiIcons(this List<PokemonType> pokemonTypes, DiscordClient client, ulong guildId)
        {
            var list = new List<string>();
            foreach (var type in pokemonTypes)
            {
                var weaknessLst = type.ToString().StringToObject<PokemonType>().GetWeaknesses().Distinct();
                foreach (var weakness in weaknessLst)
                {
                    if (!client.Guilds.ContainsKey(guildId))
                        continue;

                    var emojiId = client.Guilds[guildId].GetEmojiId($"types_{weakness.ToString().ToLower()}");
                    var emojiName = emojiId > 0 ? string.Format(Strings.TypeEmojiSchema, weakness.ToString().ToLower(), emojiId) : weakness.ToString();
                    if (!list.Contains(emojiName))
                    {
                        list.Add(emojiName);
                    }
                }
            }

            return string.Join(" ", list);
        }
    }
}