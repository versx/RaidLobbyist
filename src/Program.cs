namespace T
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = Diagnostics.EventLogger.GetLogger();
            var config = Configuration.Config.Load(Strings.ConfigFileName);
            if (config == null)
            {
                logger.Error($"Failed to load config file '{Strings.ConfigFileName}'.");
                return;
            }

            var bot = new Bot(config);
            bot.Start();

            System.Diagnostics.Process.GetCurrentProcess().WaitForExit();
        }
    }
}