using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using UtilityBot.Services.Logging;

namespace UtilityBot
{
    public class CommandHandler
    {

        private readonly IServiceProvider _provider;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;

        private Dictionary<ulong, ulong> toFromGuildIds = new Dictionary<ulong, ulong>()
        {
            { 111111111111111111, 111111111111111111 },
            { 111111111111111111, 111111111111111111 },
        };

        private Dictionary<ulong, SocketGuild> socketGuildDict = new Dictionary<ulong, SocketGuild>();
        private Dictionary<ulong, IEnumerable<SocketGuildChannel>> socketChannelDict = new Dictionary<ulong, IEnumerable<SocketGuildChannel>>();

        private readonly ILogger _logger;

        public CommandHandler(IServiceProvider provider)
        {
            _provider = provider;
            _client = _provider.GetService<DiscordSocketClient>();
            _client.MessageReceived += _client_MessageReceived;
            _commands = _provider.GetService<CommandService>();
            var log = _provider.GetService<LogAdaptor>();
            _commands.Log += log.LogCommand;
            _logger = _provider.GetService<Logger>().ForContext<CommandService>();
            _client.SetGameAsync("	୧༼ಠ益ಠ༽︻╦╤─", "https://www.youtube.com/watch?v=dQw4w9WgXcQ", StreamType.Twitch);

            while (_client.ConnectionState != ConnectionState.Connected)
            {
                Thread.Sleep(1000);
            }

            var guildIds = toFromGuildIds.Select(e => e.Key).Concat(toFromGuildIds.Select(e => e.Value)).Distinct();
            foreach (var guildId in guildIds)
            {
                var guild = _client.GetGuild(guildId);
                socketGuildDict.Add(guildId, guild);
                socketChannelDict.Add(guildId, guild.Channels);
            }

        }


        private async Task _client_MessageReceived(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            if (message == null)
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);
            int argPos = 0;

            try
            {
                var contextGuildId = context.Guild.Id;
                if (!toFromGuildIds.ContainsKey(contextGuildId))
                {
                    return;
                }

                var toChannel = socketChannelDict[toFromGuildIds[contextGuildId]].FirstOrDefault(e => e.Name == context.Channel.Name);
                if (toChannel == null)
                {
                    return;
                }

                await (toChannel as IMessageChannel).SendMessageAsync($"**{context.User.Username}**:  {context.Message.Content}");
            }
            catch (Exception)
            {

            }

            var result = await _commands.ExecuteAsync(context, argPos, _provider);


            if (result is SearchResult search && !search.IsSuccess) { }
            else if (result is PreconditionResult precondition && !precondition.IsSuccess)
                await message.Channel.SendMessageAsync("Invoked {" + message + "} in {" + context.Channel + "} with {" + result + "}");
            else if (result is ParseResult parse && !parse.IsSuccess)
                await message.Channel.SendMessageAsync("Invoked {" + message + "} in {" + context.Channel + "} with {" + result + "}");
            else if (result is TypeReaderResult reader && !reader.IsSuccess)
                await message.Channel.SendMessageAsync("Invoked {" + message + "} in {" + context.Channel + "} with {" + result + "}");
            else if (!result.IsSuccess)
                await message.Channel.SendMessageAsync("Invoked {" + message + "} in {" + context.Channel + "} with {" + result + "}");

            _logger.Debug("Invoked {Command} in {Context} with {Result}", message, context.Channel, result);

        }

        public async Task ConfigureAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }
    }
}