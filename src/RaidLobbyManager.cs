namespace RaidLobbyist
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using ServiceStack.OrmLite;

    using RaidLobbyist.Configuration;
    using RaidLobbyist.Data;
    using RaidLobbyist.Data.Models;
    using RaidLobbyist.Diagnostics;
    using RaidLobbyist.Extensions;

    public class RaidLobbyManager
    {
        #region Variables

        private static readonly IEventLogger _logger = EventLogger.GetLogger();
        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly Timer _timer;

        #endregion

        #region Properties

        public List<string> ValidRaidEmojis => new List<string>
        {
            "➡",
            "✅",
            "❌",
            //"1⃣",
            //"2⃣",
            //"3⃣",
            //"4⃣",
            //"5⃣",
            //"🔟",
            "🔄"
        };

        #endregion

        #region Constructor

        public RaidLobbyManager(DiscordClient client, Config config)
        {
            _client = client;
            _config = config;

            _timer = new Timer
            {
                Interval = 1000 * 60 * 10
            };
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
            _timer.Elapsed += async (sender, e) => await CheckActiveLobbies();
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
            _timer.Start();
        }

        #endregion

        #region Public Methods

        public async Task ProcessReaction(MessageReactionAddEventArgs e)
        {
            if (!ValidRaidEmojis.Contains(e.Emoji.Name))
                return;

            if (e.User.IsBot)
                return;

            if (e.Channel.Guild == null)
            {
                //await ProcessRaidLobbyReactionDM(e.User, e.Channel, e.Message, e.Emoji);
            }
            else
            {
                await ProcessRaidLobbyReaction(e.User, e.Channel, e.Message, e.Emoji);
            }
        }

        #endregion

        #region Private Methods

        //private async Task ProcessRaidLobbyReactionDM(DiscordUser user, DiscordChannel channel, DiscordMessage message, DiscordEmoji emoji)
        //{
        //    if (SupportersOnly)
        //    {
        //        var hasPrivilege = await _client.IsSupporterOrHigher(user.Id, _config);
        //        if (!hasPrivilege)
        //        {
        //            await message.RespondAsync($"{user.Mention} does not have the supporter role assigned.");
        //            return;
        //        }
        //    }

        //    var origMessageId = Convert.ToUInt64(Utils.GetBetween(message.Content, "#", "#"));
        //    var lobby = GetLobby(channel, ref origMessageId);

        //    var settings = await GetRaidLobbySettings(lobby, origMessageId, message, channel);
        //    if (settings == null)
        //    {
        //        _logger.Error($"Failed to find raid lobby settings for original raid message id {origMessageId}.");
        //        return;
        //    }

        //    await message.DeleteReactionAsync(emoji, user);

        //    var lobMessage = default(DiscordMessage);
        //    var embedMsg = settings.RaidMessage?.Embeds[0];

        //    switch (emoji.Name)
        //    {
        //        //case "1⃣":
        //        //    break;
        //        //case "2⃣":
        //        //    break;
        //        //case "3⃣":
        //        //    break;
        //        //case "4⃣":
        //        //    break;
        //        case "5⃣":
        //            lobby.UsersComing[user.Id].Eta = RaidLobbyEta.Five;
        //            lobby.UsersComing[user.Id].EtaStart = DateTime.Now;
        //            lobMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embedMsg);
        //            await message.DeleteAllReactionsAsync();
        //            break;
        //        case "🔟":
        //            lobby.UsersComing[user.Id].Eta = RaidLobbyEta.Ten;
        //            lobby.UsersComing[user.Id].EtaStart = DateTime.Now;
        //            lobMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embedMsg);
        //            await message.DeleteAllReactionsAsync();
        //            break;
        //        case "❌":
        //            if (!lobby.UsersComing.ContainsKey(user.Id))
        //            {
        //                lobby.UsersComing.Remove(user.Id);
        //                lobMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embedMsg);
        //            }
        //            break;
        //    }
        //    _config.RaidLobbies.ActiveLobbies[origMessageId] = lobby;
        //    _config.Save();
        //}

        private async Task ProcessRaidLobbyReaction(DiscordUser user, DiscordChannel channel, DiscordMessage message, DiscordEmoji emoji)
        {
            _logger.Trace($"RaidLobbyManager::ProcessRaidLobbyReaction [DiscordUser={user.Username}, DiscordChannel={channel.Name}, DiscordMessage={message.Content}, DiscordEmoji={emoji.Name}]");

            var isLobbyChannel = (await GetLobbyCategory()).Children.FirstOrDefault(x => x.Id == channel.Id) != null;
            if (!(_config.RaidChannelIdPool.Contains(channel.Id) || isLobbyChannel))
                return;

            var embed = await channel.GetEmbedMessage(message.Id);
            if (embed == null)
            {
                _logger.Warn($"Failed to get embed message.");
                return;
            }

            await _client.SetDefaultRaidReactions(message, false);

            var lobby = LobbyFromTitle(embed.Title);
            if (lobby.Gym?.RaidLevel == 0)
            {
                _logger.Warn($"Raid at gym '{embed.Title}' is over and doesn't exist.");
                return;
            }

            if (!_config.RaidLobbies.ContainsKey(lobby.ChannelName))
            {
                _config.RaidLobbies.Add(lobby.ChannelName, lobby);
            }

            lobby = _config.RaidLobbies[lobby.ChannelName];
            if (string.IsNullOrEmpty(lobby.StartedBy))
            {
                lobby.StartedBy = $"{user.Username}#{user.Discriminator}";
                lobby.CreatedAt = DateTime.Now;
            }

            var lobbyChannel = await CreateLobbyChannel(lobby);
            if (lobbyChannel == null)
            {
                _logger.Warn($"Failed to create or get existing lobby channel for gym {lobby.Gym.Name}.");
                return;
            }
            _logger.Debug($"LOBBY CHANNEL: {lobbyChannel.Name}");

            switch (emoji.Name)
            {
                case "➡": //Otw
                    await CreatePinnedLobbyMessage(lobby, lobbyChannel, user, true);
                    break;
                case "✅": //Here
                    await CreatePinnedLobbyMessage(lobby, lobbyChannel, user, false);
                    break;
                case "❌": //Remove
                    var raidLobby = _config.RaidLobbies[lobby.ChannelName];
                    var result = lobby.Users.RemoveAll(x => string.Compare(x.Username, $"{user.Username}#{user.Discriminator}", true) == 0);
                    if (result > 0)
                    {
                        await CreatePinnedLobbyMessage(lobby, lobbyChannel, null, false);
                    }
                    break;
                case "🔄": //Refresh
                    await CreatePinnedLobbyMessage(lobby, lobbyChannel, null, false);
                    break;
            }

            _config.Save(Strings.ConfigFileName);
        }

        private Lobby LobbyFromTitle(string title)
        {
            _logger.Trace($"RaidLobbyManager::LobbyFromTitle [Title={title}]");

            if (!title.Contains(":"))
                return new Lobby("Unknown", title); //TODO: Redo

            var parts = title.Split(':');
            if (parts.Length > 2 || parts.Length <= 0)
                return new Lobby("Unknown", title); //TODO: Redo

            var city = parts[0];
            var gymName = parts[1].TrimStart(' ');
            var lobby = new Lobby(city, gymName);
            var gym = GetRaidFromGymName(lobby.GymName);
            if (gym == null)
            {
                _logger.Warn($"Could not get gym from database with name {lobby.GymName}.");
                return lobby;
            }
            lobby.Gym = gym;

            return lobby;
        }

        private async Task<DiscordChannel> GetLobbyCategory()
        {
            var lobbyCategory = await _client.GetChannelAsync(_config.LobbyCategoryId);
            if (lobbyCategory == null)
            {
                _logger.Warn($"Could not find lobby category with id '{_config.LobbyCategoryId}'.");
                return null;
            }

            return lobbyCategory;
        }

        private async Task<DiscordChannel> CreateLobbyChannel(Lobby lobby)
        {
            _logger.Trace($"RaidLobbyManager::CreateLobbyChannel [Lobby={lobby.ChannelName}]");

            try
            {
                var lobbyCategory = await GetLobbyCategory();
                var exists = lobbyCategory.Children.FirstOrDefault(x => string.Compare(x.Name, lobby.ChannelName, true) == 0);
                var lobbyChannel = exists ?? await lobbyCategory.Guild.CreateChannelAsync(lobby.ChannelName, ChannelType.Text, lobbyCategory);

                if (lobbyChannel == null)
                {
                    _logger.Warn($"Could not create raid lobby channel with channel name {lobby.ChannelName}.");
                    return null;
                }

                return lobbyChannel;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return null;
        }

        private async Task CreatePinnedLobbyMessage(Lobby lobby, DiscordChannel lobbyChannel, DiscordUser user, bool isOtw)
        {
            _logger.Trace($"RaidLobbyManager::CreatePinnedLobbyMessage [Lobby={lobby.ChannelName}, DiscordChannel={lobbyChannel.Name}, DiscordUser={user}, IsOtw={isOtw}]");

            var pkmnImage = lobby.Gym.IsEgg
                ? string.Format(Strings.EggImage, lobby.Gym.RaidLevel)
                : lobby.Gym.RaidPokemonId.GetPokemonImage(lobby.Gym.RaidPokemonForm.ToString());
            var eb = new DiscordEmbedBuilder
            {
                Title = $"{lobby.City}: {lobby.Gym.Name}",
                Color = lobby.Gym.RaidLevel.BuildRaidColor(),
                Url = string.Format(Strings.GoogleMaps, lobby.Gym.Latitude, lobby.Gym.Longitude),
                ImageUrl = string.Format(Strings.GoogleMapsStaticImage, lobby.Gym.Latitude, lobby.Gym.Longitude),
                ThumbnailUrl = pkmnImage,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{lobbyChannel?.Guild.Name} | {DateTime.Now}",
                    IconUrl = _client.Guilds.ContainsKey(_config.GuildId) ? _client.Guilds[_config.GuildId].IconUrl : string.Empty
                }
            };

            var item = lobby.Gym.IsEgg ? $"Level {lobby.Gym.RaidLevel} Egg" : Database.Pokemon[lobby.Gym.RaidPokemonId].Name;
            eb.AddField("Raid Boss", item, true);
            if (lobby.Gym.ExRaidEligible)
            {
                eb.AddField("EX-Eligible Raid", "Yes", true);
            }

            eb.AddField("Level", lobby.Gym.RaidLevel.ToString(), true);
            if (lobby.Gym.IsEgg)
            {
                eb.AddField("Starts", lobby.Gym.RaidBattleTimestamp.FromUnix().ToLongTimeString(), true);
            }
            else
            {
                eb.AddField("CP", lobby.Gym.RaidPokemonCP.ToString("N0"), true);
                eb.AddField("Ends", lobby.Gym.RaidEndTimestamp.FromUnix().ToLongTimeString(), true);
                if (Database.Movesets.ContainsKey(lobby.Gym.RaidPokemonMove1))
                {
                    var fastMove = Database.Movesets[lobby.Gym.RaidPokemonMove1];
                    var fastMoveTypeId = _client.Guilds[_config.GuildId].GetEmojiId($"types_{fastMove.Type}");
                    if (fastMoveTypeId > 0)
                    {
                        eb.AddField("Fast Move", string.Format(Strings.TypeEmojiSchema, fastMove.Type.ToLower(), fastMoveTypeId) + $" {fastMove.Name}", true);
                    }
                }
                if (Database.Movesets.ContainsKey(lobby.Gym.RaidPokemonMove2))
                {
                    var chargeMove = Database.Movesets[lobby.Gym.RaidPokemonMove2];
                    var chargeMoveTypeId = _client.Guilds[_config.GuildId].GetEmojiId($"types_{chargeMove.Type}");
                    if (chargeMoveTypeId > 0)
                    {
                        eb.AddField("Charge Move", string.Format(Strings.TypeEmojiSchema, chargeMove.Type.ToLower(), chargeMoveTypeId) + $" {chargeMove.Name}", true);
                    }
                }

                var weaknessesEmojis = Database.Pokemon[lobby.Gym.RaidPokemonId].Types.GetWeaknessEmojiIcons(_client, _config.GuildId);
                if (!string.IsNullOrEmpty(weaknessesEmojis))
                {
                    eb.AddField("Weaknesses", weaknessesEmojis + "\r\n", true);
                }
            }

            if (_client.Guilds.ContainsKey(_config.GuildId))
            {
                if (lobby.Gym.ExRaidEligible)
                {
                    var exEmojiId = _client.Guilds.ContainsKey(_config.GuildId) ? _client.Guilds[_config.GuildId].GetEmojiId("ex") : 0;
                    var exEmoji = exEmojiId > 0 ? $"<:ex:{exEmojiId}>" : "EX";
                    eb.AddField("Ex-Eligible", exEmoji + " **Gym!**");
                }

                var teamId = _client.Guilds[_config.GuildId].GetEmojiId(lobby.Gym.Team.ToString().ToLower());
                eb.AddField("Team", $"<:{lobby.Gym.Team.ToString().ToLower()}:{teamId}>", true);
            }

            if (!string.IsNullOrEmpty(lobby.StartedBy))
            {
                eb.AddField("Started By", lobby.StartedBy, true);
            }
            eb.AddField("Location", $"{Math.Round(lobby.Gym.Latitude, 5)},{Math.Round(lobby.Gym.Longitude, 5)}\r\n" +
                                    $"**[[Google Maps]({string.Format(Strings.GoogleMaps, lobby.Gym.Latitude, lobby.Gym.Longitude)})]**\r\n" +
                                    $"**[[Apple Maps]({string.Format(Strings.AppleMaps, lobby.Gym.Latitude, lobby.Gym.Longitude)})]**", true);

            if (!_config.RaidLobbies.ContainsKey(lobby.ChannelName))
            {
                _logger.Warn($"Raid lobby does not exist yet for '{lobby.ChannelName}'.");
                return;
            }

            var raidLobby = _config.RaidLobbies[lobby.ChannelName];
            AddOrUpdateUser(raidLobby, user, isOtw);

            if (raidLobby.Users?.Count == 0)
            {
                //Delete the channel if no one is interested anymore.
                await lobbyChannel.DeleteAsync();
                return;
            }

            var usersOtw = string.Join(Environment.NewLine, raidLobby.Users?.Where(x => x.IsOnTheWay).Select(x => x.Username));
            eb.AddField("Trainers On the Way:", string.IsNullOrEmpty(usersOtw) ? "Unknown" : usersOtw, true);

            var usersHere = string.Join(Environment.NewLine, raidLobby.Users?.Where(x => x.IsHere).Select(x => x.Username));
            eb.AddField("Trainers At the Raid:", string.IsNullOrEmpty(usersHere) ? "Unknown" : usersHere, true);

            eb.Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"➡ On your way ✅ Here ❌ No longer interested 🔄 Refresh lobby message.",
                IconUrl = lobbyChannel.Guild?.IconUrl
            };

            var lobbyMessage = default(DiscordMessage);
            var pinned = await lobbyChannel.GetPinnedMessagesAsync();
            if (pinned?.Count > 0)
            {
                lobbyMessage = await pinned[0].ModifyAsync(string.Empty, eb);
            }
            else
            {
                lobbyMessage = await lobbyChannel.SendMessageAsync($"{user.Username}#{user.Discriminator} started a raid lobby for {item} at {lobby.Gym.Name}", false, eb);
                await lobbyMessage.PinAsync();
            }
            await _client.SetDefaultRaidReactions(lobbyMessage, true);
        }

        private async Task CheckActiveLobbies()
        {
            var lobbyCategory = await GetLobbyCategory();
            var lobbyChannels = lobbyCategory.Children.ToList();
            for (var i = 0; i < lobbyChannels.Count; i++)
            {
                var lobbyChannel = lobbyChannels[i];
                if (!_config.RaidLobbies.ContainsKey(lobbyChannel.Name))
                    continue;

                var lobby = _config.RaidLobbies[lobbyChannel.Name];
                if (!lobby.IsExpired)
                {
                    await UpdateLobbyChannelName(lobby, lobbyChannel);
                    await CreatePinnedLobbyMessage(lobby, lobbyChannel, null, false);
                    continue;
                }

                _logger.Debug($"Lobby {lobbyChannel.Name} has expired, deleting...");

                if (!_config.RaidLobbies.Remove(lobbyChannel.Name))
                {
                    _logger.Warn($"Could not remove raid lobby '{lobbyChannel.Name}'.");
                    continue;
                }

                await lobbyChannel.DeleteAsync("Automated: Raid lobby expired.");
                _logger.Debug($"Lobby {lobbyChannel.Name} was deleted.");
            }
        }

        private async Task UpdateLobbyChannelName(Lobby lobby, DiscordChannel lobbyChannel)
        {
            await lobbyChannel.ModifyAsync(lobby.ChannelName);
        }

        private Gym GetRaidFromGymName(string gymName)
        {
            _logger.Trace($"RaidLobbyManager::GetRaidFromGymName [GymName={gymName}]");

            using (var db = DataAccessLayer.CreateFactory(_config.ConnectionString))
            {
                var gyms = db.LoadSelect<Gym>();
                var gym = gyms.FirstOrDefault(x => string.Compare(gymName, x.Name, true) == 0);
                return gym;
            }
        }

        private void AddOrUpdateUser(Lobby lobby, DiscordUser user, bool isOtw)
        {
            _logger.Trace($"RaidLobbyManager::AddOrUpdateUser [Lobby={lobby.ChannelName}, DiscordUser={user}, IsOtw={isOtw}]");

            if (user == null)
                return;

            var username = $"{user.Username}#{user.Discriminator}";
            var lobbyUser = lobby.Users.FirstOrDefault(x => string.Compare(x.Username, username, true) == 0);
            if (lobbyUser == null)
            {
                lobbyUser = new User { Username = username, IsOnTheWay = isOtw, IsHere = !isOtw };
                lobby.Users.Add(lobbyUser);
                return;
            }

            lobbyUser.IsOnTheWay = isOtw;
            lobbyUser.IsHere = !isOtw;
        }

        #endregion
    }
}