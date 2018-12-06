namespace T
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using ServiceStack.OrmLite;

    using T.Configuration;
    using T.Data;
    using T.Data.Models;
    using T.Diagnostics;
    using T.Extensions;
    using T.Models;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RaidLobbyEta
    {
        NotSet = 0,
        Here,
        One,
        Two,
        Three,
        Four,
        Five,
        Ten,
        Fifteen,
        Twenty,
        Late
    }

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
            "1⃣",
            "2⃣",
            "3⃣",
            "4⃣",
            "5⃣",
            "🔟",
            "🔄"
        };

        public Dictionary<string, GymObject> RaidLobbies { get; }

        #endregion

        #region Constructor

        public RaidLobbyManager(DiscordClient client, Config config)
        {
            RaidLobbies = new Dictionary<string, GymObject>();
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

        public async Task<bool> DeleteExpiredRaidLobby(ulong originalMessageId)
        {
            _logger.Trace($"RaidLobbyManager::DeleteExpiredRaidLobby [OriginalMessageId={originalMessageId}]");

            if (!_config.ActiveLobbies.ContainsKey(originalMessageId))
                return false;

            var lobby = _config.ActiveLobbies[originalMessageId];
            var raidLobbyChannel = await _client.GetChannelAsync(_config.RaidLobbiesChannelId);
            if (raidLobbyChannel == null)
            {
                _logger.Error($"Failed to find raid lobby channel with id {_config.RaidLobbiesChannelId}, does it exist?");
                return false;
            }

            var lobbyMessage = await raidLobbyChannel.GetMessage(lobby.LobbyMessageId);
            if (!_config.ActiveLobbies.Remove(originalMessageId))
            {
                _logger.Error($"Failed to remove raid lobby with original message id {originalMessageId} from the list of active raid lobbies.");
                //return;
            }

            if (lobbyMessage == null)
            {
                _logger.Error($"Failed to find raid lobby message with id {lobby.LobbyMessageId}, must have already been deleted.");
                return true;
            }

            try
            {
                await lobbyMessage.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return false;
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
            if (!(_config.RaidChannelIdPool.Contains(channel.Id) || _config.RaidLobbiesChannelId == channel.Id))
                return;

            var embed = await channel.GetEmbedFromMessageId(message.Id);
            if (embed == null)
            {
                _logger.Error($"Failed to get embed message.");
                return;
            }

            //TODO: If reaction is from lobby channel, check against lobby categories children object.

            var gymObj = ParseEmbedTitle(embed.Title);
            if (!RaidLobbies.ContainsKey(gymObj.ChannelName))
            {
                RaidLobbies.Add(gymObj.ChannelName, gymObj);
            }

            var lobbyChannel = await CreateLobbyChannel(gymObj, user);
            if (lobbyChannel == null)
            {
                _logger.Error($"Failed to create new lobby channel for gym {gymObj.Gym.Name}.");
                return;
            }

            await _client.SetDefaultRaidReactions(message, false);

            //TODO: Add user to lobby list if otw or here.
            return;

            var originalMessageId = message.Id;
            var lobby = GetExistingOrCreateNewLobby(channel, ref originalMessageId);

            var settings = await GetRaidLobbySettings(lobby, originalMessageId, message, channel);
            if (settings == null)
            {
                _logger.Error($"Failed to find raid lobby settings for original raid message id {originalMessageId}.");
                return;
            }

            await message.DeleteAllReactionsAsync();

            var lobbyMessage = default(DiscordMessage);
            /*var*/ embed = settings.RaidMessage?.Embeds[0];
            if (embed == null)
            {
                _logger.Warn($"Discord message {settings.RaidMessage.Id} doesn't contain embed.");
                return;
            }

            switch (emoji.Name)
            {
                case "➡":
                    #region Coming
                    _logger.Warn($"LOBBY CHANNEL: {lobbyChannel.Name}");

                    //if (!lobby.UsersComing.ContainsKey(user.Id))
                    //{
                    //    lobby.UsersComing.Add(user.Id, new RaidLobbyUser { Id = user.Id, Eta = RaidLobbyEta.NotSet, Players = 1 });
                    //}

                    //if (lobby.UsersReady.ContainsKey(user.Id))
                    //{
                    //    lobby.UsersReady.Remove(user.Id);
                    //}

                    //lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    //await _client.SetAccountsReactions
                    //(
                    //    _config.RaidLobbiesChannelId == channel.Id
                    //    ? lobbyMessage
                    //    : message
                    //);
                    break;
                #endregion
                case "✅":
                    #region Ready
                    if (!lobby.UsersReady.ContainsKey(user.Id))
                    {
                        var players = lobby.UsersComing.ContainsKey(user.Id) ? lobby.UsersComing[user.Id].Players : 1;
                        lobby.UsersReady.Add(user.Id, new RaidLobbyUser { Id = user.Id, Eta = RaidLobbyEta.Here, Players = players });
                    }

                    if (lobby.UsersComing.ContainsKey(user.Id))
                    {
                        lobby.UsersComing.Remove(user.Id);
                    }

                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    if (_config.RaidLobbiesChannelId == channel.Id)
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                    }
                    else
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                        await _client.SetDefaultRaidReactions(message, false);
                    }
                    break;
                #endregion
                case "❌":
                    #region Remove User From Lobby
                    if (lobby.UsersComing.ContainsKey(user.Id)) lobby.UsersComing.Remove(user.Id);
                    if (lobby.UsersReady.ContainsKey(user.Id)) lobby.UsersReady.Remove(user.Id);

                    if (lobby.UsersComing.Count == 0 && lobby.UsersReady.Count == 0)
                    {
                        lobbyMessage = await settings.RaidLobbyChannel.GetMessage(lobby.LobbyMessageId);
                        if (lobbyMessage != null)
                        {
                            await lobbyMessage.DeleteAsync();
                            lobbyMessage = null;
                        }

                        _config.ActiveLobbies.Remove(lobby.OriginalRaidMessageId);
                    }
                    break;
                #endregion
                case "1⃣":
                    #region 1 Account
                    lobby.UsersComing[user.Id].Players = 1;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetEtaReactions
                    (
                        _config.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message
                    );
                    break;
                #endregion
                case "2⃣":
                    #region 2 Accounts
                    lobby.UsersComing[user.Id].Players = 2;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetEtaReactions
                    (
                        _config.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message
                    );
                    break;
                #endregion
                case "3⃣":
                    #region 3 Accounts
                    lobby.UsersComing[user.Id].Players = 3;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetEtaReactions
                    (
                        _config.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message
                    );
                    break;
                #endregion
                case "4⃣":
                    #region 4 Accounts
                    lobby.UsersComing[user.Id].Players = 4;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetEtaReactions
                    (
                        _config.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message
                    );
                    break;
                #endregion
                case "5⃣":
                    #region 5mins ETA
                    lobby.UsersComing[user.Id].Eta = RaidLobbyEta.Five;
                    lobby.UsersComing[user.Id].EtaStart = DateTime.Now;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    if (_config.RaidLobbiesChannelId == channel.Id)
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                    }
                    else
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                        await _client.SetDefaultRaidReactions(message, false);
                    }
                    break;
                #endregion
                case "🔟":
                    #region 10mins ETA
                    lobby.UsersComing[user.Id].Eta = RaidLobbyEta.Ten;
                    lobby.UsersComing[user.Id].EtaStart = DateTime.Now;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    if (_config.RaidLobbiesChannelId == channel.Id)
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                    }
                    else
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                        await _client.SetDefaultRaidReactions(message, false);
                    }
                    break;
                #endregion
                case "🔄":
                    #region Refresh
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetDefaultRaidReactions
                    (
                        _config.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message,
                        _config.RaidLobbiesChannelId == channel.Id
                    );
                    break;
                    #endregion
            }
            if (lobby != null)
            {
                if (_config.ActiveLobbies.ContainsKey(originalMessageId))
                {
                    _config.ActiveLobbies[originalMessageId] = lobby;
                }
            }
            _config.Save(Strings.ConfigFileName);
        }

        private GymObject ParseEmbedTitle(string title)
        {
            if (!title.Contains(":"))
                return new GymObject("", title); //TODO: Redo

            var parts = title.Split(':');
            if (parts.Length > 2 || parts.Length <= 0)
                return new GymObject("", title); //TODO: Redo

            var city = parts[0];
            var gymName = parts[1].TrimStart(' ');
            var gymObj = new GymObject(city, gymName);
            var gym = GetRaidByGymName(gymObj.Name);
            if (gym == null)
            {
                _logger.Error($"Failed to get gym from database from name {gymObj.Name}.");
                return gymObj;
            }
            gymObj.Gym = gym;

            return gymObj;
        }

        private async Task<DiscordChannel> CreateLobbyChannel(GymObject gymObj, DiscordUser user)
        {
            try
            {
                var lobbyCategory = await _client.GetChannelAsync(_config.LobbyCategoryId);
                if (lobbyCategory == null)
                {
                    _logger.Error($"Failed to find lobby category with LobbyCategoryId {_config.LobbyCategoryId}.");
                    return null;
                }

                var exists = lobbyCategory.Children.FirstOrDefault(x => string.Compare(x.Name, gymObj.ChannelName, true) == 0);
                //var exists = guild.Channels.FirstOrDefault(x => string.Compare(x.Name, channelName, true) == 0);
                DiscordChannel lobbyChannel;
                if (exists == null)
                {
                    lobbyChannel = await lobbyCategory.Guild.CreateChannelAsync(gymObj.ChannelName, ChannelType.Text, lobbyCategory);
                    //TODO: Check for null
                    if (lobbyChannel == null)
                    {
                        _logger.Error($"Failed to create raid lobby channel with channel name {gymObj.ChannelName}.");
                        return null;
                    }
                }
                else
                {
                    lobbyChannel = exists;
                    //TODO: Update pinned message.
                }

                await CreatePinnedLobbyMessage(gymObj, lobbyChannel, user);

                return lobbyChannel;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return null;
            }
        }

        private async Task CreatePinnedLobbyMessage(GymObject gymObj, DiscordChannel lobbyChannel, DiscordUser user)
        {
            var pkmnImage = gymObj.Gym.IsEgg ? string.Format(Strings.EggImage, gymObj.Gym.RaidLevel) : gymObj.Gym.RaidPokemonId.GetPokemonImage(gymObj.Gym.RaidPokemonForm.ToString());
            var eb = new DiscordEmbedBuilder
            {
                Title = gymObj.Gym.Name,
                Color = gymObj.Gym.RaidLevel.BuildRaidColor(),
                Url = string.Format(Strings.GoogleMaps, gymObj.Gym.Latitude, gymObj.Gym.Longitude),
                ImageUrl = string.Format(Strings.GoogleMapsStaticImage, gymObj.Gym.Latitude, gymObj.Gym.Longitude) + $"&key={_config.GmapsKey}",
                ThumbnailUrl = pkmnImage,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"versx | {DateTime.Now}",
                    IconUrl = _client.Guilds.FirstOrDefault().Value?.IconUrl
                }
            };

            var item = gymObj.Gym.IsEgg ? $"Level {gymObj.Gym.RaidLevel} Egg" : Database.Instance.Pokemon[gymObj.Gym.RaidPokemonId].Name;
            eb.AddField("Raid Boss", item, true);
            if (gymObj.Gym.ExRaidEligible)
            {
                eb.AddField("EX-Eligible Raid", "Yes", true);
            }

            if (gymObj.Gym.IsEgg)
            {
                eb.AddField("Starts", gymObj.Gym.RaidBattleTimestamp.FromUnix().ToLongTimeString(), true);
            }
            else
            {
                eb.AddField("CP", gymObj.Gym.RaidPokemonCP.ToString("N0"), true);
                eb.AddField("Ends", gymObj.Gym.RaidEndTimestamp.FromUnix().ToLongTimeString(), true);
                if (Database.Instance.Movesets.ContainsKey(gymObj.Gym.RaidPokemonMove1))
                {
                    var fastMove = Database.Instance.Movesets[gymObj.Gym.RaidPokemonMove1];
                    var fastMoveTypeId = lobbyChannel.Guild.GetEmojiId(fastMove.Type);
                    if (fastMoveTypeId > 0)
                    {
                        eb.AddField("Fast Move", $"<{fastMove.Type.ToLower()}:{fastMoveTypeId}> {fastMove.Name}", true);
                    }
                }
                if (Database.Instance.Movesets.ContainsKey(gymObj.Gym.RaidPokemonMove2))
                {
                    var chargeMove = Database.Instance.Movesets[gymObj.Gym.RaidPokemonMove2];
                    var chargeMoveTypeId = lobbyChannel.Guild.GetEmojiId(chargeMove.Type);
                    if (chargeMoveTypeId > 0)
                    {
                        eb.AddField("Charge Move", $"<{chargeMove.Type.ToLower()}:{chargeMoveTypeId}> {chargeMove.Name}", true);
                    }
                }
            }
            var teamId = lobbyChannel.Guild.GetEmojiId(gymObj.Gym.Team.ToString().ToLower());
            eb.AddField("Team", $"<{gymObj.Gym.Team.ToString().ToLower()}:{teamId}>", true);
            eb.AddField("Started By", $"{user.Username}#{user.Discriminator}", true);
            eb.AddField("Location", $"{gymObj.Gym.Latitude},{gymObj.Gym.Longitude}", true);

            var lobby = RaidLobbies[gymObj.ChannelName];
            var lobbyUser = lobby.Users.FirstOrDefault(x => string.Compare(x.Username, $"{user.Username}#{user.Discriminator}", true) == 0);
            if (lobbyUser == null)
            {
                lobby.Users.Add(new LobbyUser { Username = $"{user.Username}#{user.Discriminator}", IsOnTheWay = true, IsHere = false }); //TODO: Fix
            }

            if (lobby.Users.Count > 0)
            {
                //TODO: List who's going and who's already at the raid.
                var usersOtw = string.Join(Environment.NewLine, lobby.Users?.Where(x => x.IsOnTheWay).Select(x => x.Username));
                var usersHere = string.Join(Environment.NewLine, lobby.Users?.Where(x => x.IsHere).Select(x => x.Username));
                if (!string.IsNullOrEmpty(usersOtw))
                {
                    eb.AddField("Trainers On the Way:", usersOtw ?? "Unknown", true);
                }
                if (!string.IsNullOrEmpty(usersHere))
                {
                    eb.AddField("Trainers At the Raid:", usersHere ?? "Unknown", true);
                }
            }

            var pinned = await lobbyChannel.GetPinnedMessagesAsync();
            var lobbyMessage = default(DiscordMessage);
            if (pinned.Count > 0)
            {
                //TODO: Check if update existing message works.
                lobbyMessage = await pinned[0].ModifyAsync(string.Empty, eb);
            }
            else
            {
                lobbyMessage = await lobbyChannel.SendMessageAsync($"{user.Username}#{user.Discriminator} started a raid lobby for {item} at {gymObj.Gym.Name}", false, eb);
                await lobbyMessage.PinAsync();
            }
            await _client.SetDefaultRaidReactions(lobbyMessage, true);
        }

        private async Task CheckActiveLobbies()
        {
            try
            {
                //TODO: Check channels and delete them.
                //var lobbyCategory = await _client.GetChannelAsync(_config.LobbyCategoryId);
                //if (lobbyCategory == null)
                //{
                //    //TODO: Failed to get lobby category.
                //    return;
                //}

                //for (var i = 0; i < lobbyCategory.Children.ToList().Count; i++)
                //{

                //}

                var keys = _config.ActiveLobbies.Keys.ToList();
                for (int i = 0; i < keys.Count; ++i)
                {
                    var key = keys[i];
                    var lobby = _config.ActiveLobbies[key];
                    if (!lobby.IsExpired)
                        continue;

                    if (_config.ActiveLobbies.ContainsKey(key))
                    {
                        if (!await DeleteExpiredRaidLobby(key))
                        {
                            _logger.Error($"Failed to delete raid lobby message with id {key}.");
                        }

                        _config.ActiveLobbies.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private Gym GetRaidByGymName(string gymName)
        {
            using (var db = DataAccessLayer.CreateFactory(_config.ConnectionString))
            {
                var gyms = db.LoadSelect<Gym>();
                var gym = gyms.FirstOrDefault(x => string.Compare(gymName, x.Name, true) == 0);
                return gym;
            }
        }

        #endregion

        #region Raid Lobby

        private RaidLobby GetExistingOrCreateNewLobby(DiscordChannel channel, ref ulong originalMessageId)
        {
            RaidLobby lobby = null;
            if (channel.Id == _config.RaidLobbiesChannelId)
            {
                foreach (var item in _config.ActiveLobbies)
                {
                    if (item.Value.LobbyMessageId == originalMessageId)
                    {
                        originalMessageId = item.Value.OriginalRaidMessageId;
                        lobby = item.Value;
                        break;
                    }
                }
            }
            else
            {
                if (_config.ActiveLobbies.ContainsKey(originalMessageId))
                {
                    lobby = _config.ActiveLobbies[originalMessageId];
                }
                else
                {
                    lobby = new RaidLobby { OriginalRaidMessageId = originalMessageId, OriginalRaidMessageChannelId = channel.Id, Started = DateTime.Now };
                    _config.ActiveLobbies.Add(originalMessageId, lobby);
                }
            }

            return lobby;
        }

        private async Task<RaidLobbySettings> GetRaidLobbySettings(RaidLobby lobby, ulong originalMessageId, DiscordMessage message, DiscordChannel channel)
        {
            _logger.Trace($"RaidLobbyManager::GetRaidLobbySettings [OriginalMessageId={originalMessageId}, DiscordMessage={message.Content}, DiscordChannel={channel.Name}]");

            var raidLobbyChannel = await _client.GetChannelAsync(_config.RaidLobbiesChannelId);
            if (raidLobbyChannel == null)
            {
                _logger.Error($"Failed to retrieve the raid lobbies channel with id {_config.RaidLobbiesChannelId}.");
                return null;
            }

            if (lobby == null)
            {
                _logger.Error($"Failed to find raid lobby, it may have already expired, deleting message with id {message.Id}...");
                await message.DeleteAsync("Raid lobby does not exist anymore.");
                return null;
            }

            var origChannel = await _client.GetChannelAsync(lobby.OriginalRaidMessageChannelId);
            if (origChannel == null)
            {
                _logger.Error($"Failed to find original raid message channel with id {lobby.OriginalRaidMessageChannelId}.");
                return null;
            }

            var raidMessage = await origChannel.GetMessage(originalMessageId);
            if (raidMessage == null)
            {
                _logger.Warn($"Failed to find original raid message with {originalMessageId}, searching server...");
                raidMessage = await GetRaidMessage(originalMessageId);
            }

            _config.Save(Strings.ConfigFileName);

            return new RaidLobbySettings
            {
                OriginalRaidMessageChannel = origChannel,
                RaidMessage = raidMessage,
                RaidLobbyChannel = raidLobbyChannel
            };
        }

        private async Task<DiscordMessage> UpdateRaidLobbyMessage(RaidLobby lobby, DiscordChannel raidLobbyChannel, DiscordEmbed raidMessage)
        {
            _logger.Trace($"RaidLobbyManager::UpdateRaidLobbyMessage [RaidLobby={lobby.LobbyMessageId}, DiscordChannel={raidLobbyChannel.Name}, DiscordMessage={raidMessage.Title}]");

            try
            {
                var coming = await GetUsernames(lobby?.UsersComing);
                var ready = await GetUsernames(lobby?.UsersReady);

                var msg = $"**Trainers on the way:**{Environment.NewLine}```{string.Join(Environment.NewLine, coming)}  ```{Environment.NewLine}**Trainers at the raid:**{Environment.NewLine}```{string.Join(Environment.NewLine, ready)}  ```";
                var lobbyMessage = await raidLobbyChannel.GetMessage(lobby.LobbyMessageId);
                if (lobbyMessage != null)
                {
                    await lobbyMessage.DeleteAsync();
                }

                lobbyMessage = await raidLobbyChannel.SendMessageAsync(msg, false, raidMessage);
                if (lobbyMessage == null)
                {
                    _logger.Error($"Failed to set default raid reactions to message {lobby.LobbyMessageId}, couldn't find message...");
                    return null;
                }
                lobby.LobbyMessageId = lobbyMessage.Id;
                _config.Save(Strings.ConfigFileName);

                return lobbyMessage;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return null;
        }

        private ulong TimeLeft(DateTime etaStart)
        {
            try
            {
                return Convert.ToUInt64((etaStart.AddMinutes(5) - DateTime.Now).Minutes);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return 0;
            }
        }

        private async Task<DiscordMessage> GetRaidMessage(ulong messageId)
        {
            _logger.Trace($"RaidLobbyManager::GetRaidMessage [MessageId={messageId}]");

            foreach (var channelId in _config.RaidChannelIdPool)
            {
                var channel = await _client.GetChannelAsync(channelId);
                if (channel == null)
                {
                    _logger.Error($"Failed to find channel {channelId}.");
                    continue;
                }

                var message = await channel.GetMessage(messageId);
                if (message == null)
                    continue;

                return message;
            }

            return null;
        }

        private async Task<List<string>> GetUsernames(Dictionary<ulong, RaidLobbyUser> users)
        {
            var list = new List<string>();
            if (users == null)
                return list;

            foreach (var item in users)
            {
                var user = await _client.GetUserAsync(item.Key);
                if (user == null)
                {
                    _logger.Error($"Failed to find discord user with id {item.Key}.");
                    continue;
                }

                try
                {
                    var timeLeft = TimeLeft(item.Value.EtaStart);
                    if (timeLeft == 0)
                    {
                        if (item.Value.Eta != RaidLobbyEta.NotSet && item.Value.Eta != RaidLobbyEta.Here)
                        {
                            //User is late, send DM.
                            item.Value.Eta = RaidLobbyEta.Late;

                            var dm = await _client.SendDirectMessage(user, $"{user.Mention} you're late for the raid, do you want to extend your time? If not please click the red cross button below to remove yourself from the raid lobby.\r\n#{item.Key}#", null);
                            if (dm == null)
                            {
                                _logger.Error($"Failed to send {user.Username} a direct message letting them know they are late for the raid.");
                                continue;
                            }

                            await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":five:"));
                            await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":keycap_ten:"));
                            await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":x:"));
                        }
                    }

                    var eta = (item.Value.Eta != RaidLobbyEta.Here && item.Value.Eta != RaidLobbyEta.NotSet && item.Value.Eta != RaidLobbyEta.Late ? $"{timeLeft} minute{(timeLeft > 1 ? "s" : null)}" : item.Value.Eta.ToString());
                    list.Add($"{user.Username} ({item.Value.Players} account{(item.Value.Players == 1 ? "" : "s")}, ETA: {eta})");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
            return list;
        }

        #endregion
    }

    public class LobbyUser
    {
        public string Username { get; set; }

        public bool IsOnTheWay { get; set; }

        public bool IsHere { get; set; }

        public override string ToString()
        {
            return Username;
        }
    }

    public class GymObject
    {
        public string City { get; set; }

        public string Name { get; set; }

        public Gym Gym { get; set; }

        public List<LobbyUser> Users { get; set; }

        public string ChannelName
        {
            get
            {
                if (Gym == null)
                    return $"{City}_{Name.Replace(" ", "-")}";

                var isEgg = Gym.RaidPokemonId == 0 && Gym.RaidLevel > 0;
                var channelName = City + "_" + (isEgg ? $"lvl{Gym.RaidLevel}egg" : $"{Database.Instance.Pokemon[Gym.RaidPokemonId].Name}") + $"_{Gym.Name}";
                return channelName.Replace(" ", "-");
            }
        }


        public GymObject()
        {
            Users = new List<LobbyUser>();
        }

        public GymObject(string city, string name) 
            : this()
        {
            City = city;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class RaidLobbySettings
    {
        //public RaidLobby Lobby { get; set; }

        public DiscordChannel OriginalRaidMessageChannel { get; set; }

        public DiscordMessage RaidMessage { get; set; }

        public DiscordChannel RaidLobbyChannel { get; set; }
    }

    public static class PokemonExtensions
    {
        public static string GetPokemonImage(this int pokemonId, string form)
        {
            if (int.TryParse(form, out var formId))
            {
                return string.Format(Strings.PokemonImage, pokemonId, formId);
            }

            return string.Format(Strings.PokemonImage, pokemonId, 0);
        }
    }
}