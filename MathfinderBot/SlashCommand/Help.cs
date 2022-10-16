using Discord.Interactions;

namespace MathfinderBot.SlashCommand
{
    public class Help : InteractionModuleBase
    {
        
        public InteractionService   Service { get; set; }
        private CommandHandler      handler;
        
        public Help(CommandHandler handler) => this.handler = handler;

        const string basics =
@"__GETTING STARTED__
To create a character type `/char` and select the 'New' option in the first 
field, as well as specifying a name in the `char-name-or-number` field (You can TAB 
through  the different options)

You can use `/char` and the `Update` option to upload a copy of your character sheet from
different programs, selecting a `sheet-type` and using the `attachment` field to add a file.

The rest of the help has been moved to my Github. [Check out the documentation for more detailed info.](https://github.com/Gellybean/Mathfinder-Bot)";


        [SlashCommand("mf-help", "help")]
        public async Task HelpCommand()
        {
            await RespondAsync(basics, ephemeral: true);                   
        }       
    }
}
