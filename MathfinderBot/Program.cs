using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

namespace MathfinderBot
{
    public class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();
        
        public static DiscordSocketClient  client;
        public static InteractionService   interactionService;

        public LoggingService logger;

        public async Task MainAsync()
        {
            using(var services = CreateServices())
            {
                client              = services.GetRequiredService<DiscordSocketClient>();
                interactionService  = services.GetRequiredService<InteractionService>();
                logger              = new LoggingService(client);

                client.Ready += ReadyAsync;

                var token = "MTAwMzg0NDYyODg0MTIzODU4OA.G7kN_9.Q5WgQp222LF52A5_uge958ElOzePLOtNq6TOzo";

                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();
                
                await services.GetRequiredService<CommandHandler>().InitializeAsync();
                
                await Task.Delay(Timeout.Infinite);
            }                                
        }
        
        private async Task ReadyAsync()
        {;
            await interactionService.RegisterCommandsGloballyAsync();
        }

        public static ServiceProvider CreateServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();
        }
        
    }
}

