﻿namespace T.Extensions
{
    using System;
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
                await message.DeleteAllReactionsAsync();
                await Task.Delay(10);
            }

            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":arrow_right:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":white_check_mark:"));
            if (isLobby)
            {
                await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":x:"));
                await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":arrows_counterclockwise:"));
            }
        }

        public static async Task SetAccountsReactions(this DiscordClient client, DiscordMessage message, bool deleteExisting = true)
        {
            if (client == null) return;

            if (deleteExisting)
            {
                await message.DeleteAllReactionsAsync();
            }

            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":one:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":two:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":three:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":four:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":five:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":six:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":seven:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":eight:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":nine:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":ten:"));
        }

        public static async Task SetEtaReactions(this DiscordClient client, DiscordMessage message, bool deleteExisting = true)
        {
            if (client == null) return;

            if (deleteExisting)
            {
                await message.DeleteAllReactionsAsync();
            }

            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":five:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":keycap_ten:"));
            //TODO: Add more ETA reactions.
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
    }
}