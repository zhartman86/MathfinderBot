using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;

namespace MathfinderBot
{
    public class LoggingService
    {
        public LoggingService(DiscordSocketClient client)
        {
            client.Log += LogAsync;
            client.Rest.Log += LogAsync;
            client.SlashCommandExecuted += LogCommand;
        }

        private async Task LogAsync(LogMessage msg)
        {
            await Task.Run(() =>
            {
                if(msg.Exception is CommandException cmdException)
                {
                    Console.WriteLine($"[Command/{msg.Severity}] {cmdException.Command.Aliases.First()}"
                    + $" failed to execute in {cmdException.Context.Channel}.");
                    Console.WriteLine(cmdException);
                }
                else Console.WriteLine($"[General/{msg.Severity}] {msg}");
            });
        }
    
        private Task LogCommand(SocketSlashCommand command)
        {
            Console.WriteLine($"{command.User.Username} -> {command.CommandName}");
            return Task.CompletedTask;
        }
    }
}

