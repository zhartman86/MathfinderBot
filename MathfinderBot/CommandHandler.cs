using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MathfinderBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient    client;
        private readonly CommandService         commands;


        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            this.client = client;
            this.commands = commands;
            Setup();
        }
    
        public void Setup()
        {
            client.MessageReceived      += MessageReceived;
            client.SlashCommandExecuted += SlashHandler;
        }
       
        private async Task MessageReceived(SocketMessage param)
        {


        }

        private async Task SlashHandler(SocketSlashCommand command)
        {
            switch(command.Data.Name)
            {
                case "roll":
                    await Roll(command);
                    break;
            }
        }

        private async Task Roll(SocketSlashCommand command)
        {
            var expr = (string)command.Data.Options.First().Value;

            var embeded = new EmbedBuilder()
                .WithAuthor(command.User.ToString(), command.User.GetAvatarUrl() ?? command.User.GetDefaultAvatarUrl())
                .WithTitle("Roll")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();

            await command.RespondAsync(embed: embeded.Build());
        }
    }
}
