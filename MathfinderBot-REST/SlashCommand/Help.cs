using Discord.Interactions;

namespace MathfinderBot.SlashCommand
{
    public class Help : InteractionModuleBase
    {       
        const string basics =
@"__GETTING STARTED__
The first time you use `/eval`, it will generate a global space for you to create variables and expressions (more info at <https://github.com/Gellybean/Mathfinder-Bot/wiki/eval>).

If you want to create a managed character, as well as details on how to import data from other programs, [Check out the documentation for more info.](https://github.com/Gellybean/Mathfinder-Bot)

This bot is a major work-in-progress, but I will do my best to stay informative of any recent changes.
You can find these changes in the Changelog:<https://github.com/Gellybean/Mathfinder-Bot/blob/main/CHANGELOG.md>.";

        [SlashCommand("mf-help", "help")]
        public async Task HelpCommand()
        {
            await RespondAsync(basics, ephemeral: true);                   
        }       
    }
}
