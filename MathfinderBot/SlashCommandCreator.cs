using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace MathfinderBot
{
    public class SlashCommandCreator
    {         
        public SlashCommandCreator(DiscordSocketClient client)
        {
            Roll(client);
        }
        
        
        public async Task Roll(DiscordSocketClient client)
        {
            var rollCommandBuilder = new SlashCommandBuilder()
            .WithName("roll")
            .WithDescription("Dice Roller")
            .AddOption("expression", ApplicationCommandOptionType.String, "XdY type expression with optional +- modifier", isRequired: true);
           
            try
            {
                await client.CreateGlobalApplicationCommandAsync(rollCommandBuilder.Build());
            }
            catch(HttpException ex)
            {
                var error = JsonConvert.SerializeObject(ex.Errors, Formatting.Indented);
                Console.WriteLine(error);
            }

          
        }
    }
}
