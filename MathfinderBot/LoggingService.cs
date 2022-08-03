using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MathfinderBot
{
    public class LoggingService
    {
        public LoggingService(DiscordSocketClient client, CommandService commandService)
        {
            client.Log += LogAsync;
            commandService.Log += LogAsync;
        }

        private Task LogAsync(LogMessage msg)
        {
            if(msg.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{msg.Severity}] {cmdException.Command.Aliases.First()}"
                + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else Console.WriteLine($"[General/{msg.Severity}] {msg}");

            return Task.CompletedTask;
        }
    }
}

