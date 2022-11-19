using Discord.Interactions;

namespace MathfinderBot.SlashCommand
{
    public class Help : InteractionModuleBase
    {       
        const string basics =
@"__GETTING STARTED__
The first time you use `/eval`, it will generate a global-space for you to create variables and expressions.

If you'd like to use MF-Bot's character management, you can begin by using `/char` and inputting a name in the `character` field. After a character is generated, you will have access to a number of variables and expressions you might see on any Pathfinder character sheet. Furthermore, you can update this character with exported sheets from various programs (PCGen, Pathbuilder, Herolabs, Mottokrosh).

I've moved most of the help to my Github. [Check out the documentation for more detailed info.](https://github.com/Gellybean/Mathfinder-Bot)

This bot is a major work-in-progress, but I will do my best to stay informative of any recent changes.
You can find these changes in the Changelog:<https://github.com/Gellybean/Mathfinder-Bot/blob/main/CHANGELOG.md>.";

        [SlashCommand("mf-help", "help")]
        public async Task HelpCommand()
        {
            await RespondAsync(basics, ephemeral: true);                   
        }       
    }
}
