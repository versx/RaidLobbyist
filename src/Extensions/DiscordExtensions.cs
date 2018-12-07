namespace T.Extensions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using T.Diagnostics;

    public static class DiscordExtensions
    {
        private static readonly IEventLogger _logger = EventLogger.GetLogger();

        public static async Task SetDefaultRaidReactions(this DiscordClient client, DiscordMessage message, bool isLobby, bool deleteExisting = true)
        {
            if (client == null) return;

            if (deleteExisting)
            {
                try
                {
                    await message?.DeleteAllReactionsAsync();
                    await Task.Delay(10);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":arrow_right:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":white_check_mark:"));
            if (isLobby)
            {
                await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":x:"));
                await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":arrows_counterclockwise:"));
            }
        }

        public static async Task<DiscordMessage> SendDirectMessage(this DiscordClient client, DiscordUser user, string message, DiscordEmbed embed)
        {
            if (string.IsNullOrEmpty(message) && embed == null)
                return null;

            try
            {
                var dm = await client.CreateDmAsync(user);
                if (dm != null)
                {
                    var msg = await dm.SendMessageAsync(message, false, embed);
                    return msg;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return null;
        }

        public static async Task<DiscordMessage> GetMessage(this DiscordChannel channel, ulong messageId)
        {
            try
            {
                return await channel.GetMessageAsync(messageId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return null;
            }
        }

        public static async Task<DiscordEmbed> GetEmbedMessage(this DiscordChannel channel, ulong messageId)
        {
            var message = await channel.GetMessage(messageId);
            if (message.Embeds.Count > 0)
            {
                return message.Embeds[0];
            }

            return null;
        }

        public static ulong? GetEmojiId(this DiscordGuild guild, string emojiName)
        {
            return guild.Emojis.FirstOrDefault(x => string.Compare(x.Name, emojiName, true) == 0)?.Id;
        }

        public static DiscordColor BuildRaidColor(this int level)
        {
            switch (level)
            {
                case 1:
                    return DiscordColor.HotPink;
                case 2:
                    return DiscordColor.HotPink;
                case 3:
                    return DiscordColor.Yellow;
                case 4:
                    return DiscordColor.Yellow;
                case 5:
                    return DiscordColor.Purple;
            }

            return DiscordColor.White;
        }
    }
}