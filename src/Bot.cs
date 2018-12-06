namespace T
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.EventArgs;

    using T.Configuration;
    using T.Diagnostics;
    using T.Extensions;

    public class Bot
    {
        #region Variables

        private readonly RaidLobbyManager _lobbyMgr;
        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #endregion

        #region Constructor

        public Bot(Config config)
        {
            var name = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName;
            _logger = EventLogger.GetLogger(name);
            _logger.Trace($"Bot::Bot [WhConfig={config.GuildId}, OwnerId={config.OwnerId}]");

            _config = config;

#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
            AppDomain.CurrentDomain.UnhandledException += async (sender, e) =>
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
            {
                _logger.Debug("Unhandled exception caught.");
                _logger.Error((Exception)e.ExceptionObject);

                if (e.IsTerminating)
                {
                    if (_client != null)
                    {
                        var owner = await _client.GetUserAsync(_config.OwnerId);
                        if (owner == null)
                        {
                            _logger.Warn($"Failed to get owner from id {_config.OwnerId}.");
                            return;
                        }

                        await _client.SendDirectMessage(owner, Strings.CrashMessage, null);
                    }
                }
            };

            _client = new DiscordClient(new DiscordConfiguration
            {
                AutomaticGuildSync = true,
                AutoReconnect = true,
                EnableCompression = true,
                Token = _config.Token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true
            });
            _client.Ready += Client_Ready;
            _client.MessageReactionAdded += Client_MessageReactionAdded;
            _client.ClientErrored += Client_ClientErrored;
            _client.DebugLogger.LogMessageReceived += DebugLogger_LogMessageReceived;

            _lobbyMgr = new RaidLobbyManager(_client, _config);
        }

        #endregion

        #region Discord Events

        private async Task Client_Ready(ReadyEventArgs e)
        {
            _logger.Info($"Connected.");

            await Task.CompletedTask;
        }

        private async Task Client_MessageReactionAdded(MessageReactionAddEventArgs e)
        {
            if (!_config.Enabled)
                return;

            await _lobbyMgr.ProcessReaction(e);
        }

        private async Task Client_ClientErrored(ClientErrorEventArgs e)
        {
            _logger.Error(e.Exception);

            await Task.CompletedTask;
        }

        private void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
        {
            //Color
            ConsoleColor color;
            switch (e.Level)
            {
                case LogLevel.Error: color = ConsoleColor.DarkRed; break;
                case LogLevel.Warning: color = ConsoleColor.Yellow; break;
                case LogLevel.Info: color = ConsoleColor.White; break;
                case LogLevel.Critical: color = ConsoleColor.Red; break;
                case LogLevel.Debug: default: color = ConsoleColor.DarkGray; break;
            }

            //Source
            var sourceName = e.Application;

            //Text
            var text = e.Message;

            //Build message
            var builder = new System.Text.StringBuilder(text.Length + (sourceName?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }

            for (var i = 0; i < text.Length; i++)
            {
                //Strip control chars
                var c = text[i];
                if (!char.IsControl(c))
                    builder.Append(c);
            }

            if (text != null)
            {
                builder.Append(": ");
                builder.Append(text);
            }

            text = builder.ToString();
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        #endregion

        #region Public Methods

        public async Task Start()
        {
            _logger.Trace("Bot::Start");
            _logger.Info("Connecting to Discord...");

            await _client.ConnectAsync();
        }

        public async Task Stop()
        {
            _logger.Trace($"Bot::Stop");
            _logger.Info("Disconnecting from Discord...");

            await _client.DisconnectAsync();
        }

        #endregion
    }
}