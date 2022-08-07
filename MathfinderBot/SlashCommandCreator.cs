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
            //Roll(client);
            NewChar(client);
            SelectChar(client);
        }
        
        
        public async Task NewChar(DiscordSocketClient client)
        {
            var newCharCommandBuilder = new SlashCommandBuilder()
                .WithName("new-char")
                .WithDescription("Create a new character");


            try
            {
                await client.CreateGlobalApplicationCommandAsync(newCharCommandBuilder.Build());
            }
            catch(HttpException ex)
            {
                var error = JsonConvert.SerializeObject(ex.Errors, Formatting.Indented);
                Console.WriteLine(error);
            }
        }

        public async Task SelectChar(DiscordSocketClient client)
        {
            var selectCharCommandBuilder = new SlashCommandBuilder()
                .WithName("select-char")
                .WithDescription("Select an existing character");

            
            try
            {
                await client.CreateGlobalApplicationCommandAsync(selectCharCommandBuilder.Build());
            }
            catch(HttpException ex)
            {
                var error = JsonConvert.SerializeObject(ex.Errors, Formatting.Indented);
                Console.WriteLine(error);
            }

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
