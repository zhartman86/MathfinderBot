using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;

namespace MathfinderBot
{
    public class CommandHandler
    {
        readonly DiscordSocketClient    client;
        readonly InteractionService     interactionService;
        readonly IServiceProvider       services;

        public CommandHandler(DiscordSocketClient client, InteractionService interactionService, IServiceProvider services)
        {
            this.client = client;
            this.interactionService = interactionService;
            this.services = services;
        }
    
        public async Task InitializeAsync()
        {
            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            client.InteractionCreated += HandleInteraction;
        }
    
        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(client, interaction);
                await interactionService.ExecuteCommandAsync(context, services);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);               
            }
        }
    }
}
