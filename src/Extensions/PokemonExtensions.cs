namespace T.Extensions
{
    public static class PokemonExtensions
    {
        public static string GetPokemonImage(this int pokemonId, string form)
        {
            return string.Format(Strings.PokemonImage, pokemonId, int.TryParse(form, out var formId) ? formId : 0);
        }
    }
}