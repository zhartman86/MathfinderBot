using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MathfinderBot
{
    public class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();

        private static CommandHandler       commandHandler;
        private static CommandService       commands;

        private static DiscordSocketClient  client;
        

        public LoggingService logger;

        public async Task MainAsync()
        {
            client =    new DiscordSocketClient();
            commands =  new CommandService();
            logger = new LoggingService(client, commands);
            commandHandler = new CommandHandler(client, commands);
            client.Ready += async () => { var creator = new SlashCommandCreator(client); };

            var token = "MTAwMzg0NDYyODg0MTIzODU4OA.G7kN_9.Q5WgQp222LF52A5_uge958ElOzePLOtNq6TOzo";

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            await Task.Delay(-1);
        }
    
    
       
    }
}

