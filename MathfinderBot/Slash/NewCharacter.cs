using Discord.Interactions;
using Gellybeans.Pathfinder;

namespace MathfinderBot
{
    public class NewCharacter : InteractionModuleBase
    {
        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public NewCharacter(CommandHandler handler) => this.handler = handler;

        [SlashCommand("new-char", "Create a new character")]
        public async Task Command() => await Context.Interaction.RespondWithModalAsync<NewCharacterModal>("new_character");


        [ModalInteraction("new_character")]
        public async Task ModalResponse(NewCharacterModal modal)
        {
            var user = Context.Interaction.User;
            Console.WriteLine(user.ToString());
            if(Pathfinder.Database.ContainsKey(user))
            {
                if(Pathfinder.Database[user].ContainsKey(modal.CharacterName))
                    await RespondAsync("Character " + modal.CharacterName +   " already exists.");
            }
            Pathfinder.Database[user] = new Dictionary<string, StatBlock>() {{ modal.CharacterName, StatBlock.DefaultPathfinder() }};
            await RespondAsync("Character " + modal.CharacterName + " created.");
        }
      
    }
}
