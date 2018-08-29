using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.InteractiveCommands;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using UtilityBot.Services.Logging;

namespace UtilityBot
{
    internal class Program
    {
        private Dictionary<ulong, SocketGuild> socketGuildDict = new Dictionary<ulong, SocketGuild>();
        private Dictionary<ulong, IEnumerable<SocketGuildChannel>> socketChannelDict = new Dictionary<ulong, IEnumerable<SocketGuildChannel>>();


        private static void Main(string[] args) =>
           new Program().RunAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandHandler _handler;

        private async Task RunAsync()
        {
            if (!bool.TryParse(ConfigurationManager.AppSettings["CloneChannels"], out var cloneChannels))
            {
                throw new ArgumentException("CloneChannels");
            }


            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
            });


            var serviceProvider = ConfigureServices();

            await _client.LoginAsync(TokenType.User, "YOUR_TOKEN_HERE");
            await _client.StartAsync();

            _client = serviceProvider.GetService<DiscordSocketClient>();

            while (_client.ConnectionState != ConnectionState.Connected)
            {
                Thread.Sleep(1000);
            }

            if (cloneChannels)
            {
                var success = CloneChannels(_client);
                Console.WriteLine(success ? "Channel Cloning Succeeded!" : "Channel Cloning Failed!");
            }

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
            });

            _handler = new CommandHandler(serviceProvider);
            await _handler.ConfigureAsync();

            await Task.Delay(-1);
        }

        private bool CloneChannels(DiscordSocketClient client)
        {
            try
            {
                var toFromGuildIds = new Dictionary<ulong, ulong>()
                {
                    { /* FROM GUILD */ 11111, /* TO GUILD */ 22222 },
                };

                var guildIds = toFromGuildIds.Select(e => e.Key).Concat(toFromGuildIds.Select(e => e.Value)).Distinct();
                foreach (var guildId in guildIds)
                {
                    var guild = _client.GetGuild(guildId);
                    socketGuildDict.Add(guildId, guild);
                    socketChannelDict.Add(guildId, guild.Channels);
                }

                foreach (var kvp in toFromGuildIds)
                {
                    var fromId = kvp.Key;
                    var toId = kvp.Value;

                    var fromChannels = socketChannelDict[fromId].Select(e => e.Name);
                    var toChannels = socketChannelDict[toId].Select(e => e.Name);
                    var toGuild = socketGuildDict[toId];

                    foreach (var fromChannel in fromChannels)
                    {
                        var name = fromChannel;
                        if (!string.IsNullOrEmpty(name) && !toChannels.Contains(name, StringComparer.OrdinalIgnoreCase))
                        {
                            toGuild.CreateTextChannelAsync(name).Wait();
                            Console.WriteLine($"Cloning {name}.");
                        }
                        else
                        {
                            Console.WriteLine($"{name} already exists.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
                return false;
            }
            return true;
        }


        private IServiceProvider ConfigureServices()
        {
            // Configure logging
            var logger = LogAdaptor.CreateLogger();
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new SerilogLoggerProvider(logger));
            // Configure services
            var services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false, ThrowOnError = false }))
                .AddSingleton(logger)
                .AddSingleton<LogAdaptor>()
                .AddSingleton<InteractiveService>();

            var provider = services.BuildServiceProvider();
            // Autowire and create these dependencies now
            provider.GetService<LogAdaptor>();
            return provider;
        }
    }
}