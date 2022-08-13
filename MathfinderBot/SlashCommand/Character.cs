using System.Linq;
using System.Text;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MathfinderBot
{
    public class Character : InteractionModuleBase
    {
        public enum CharacterCommand
        {            
            Set,
            New,
            List,
            Delete
        }

        static Regex validName = new Regex(@"^[a-zA-Z]{3,25}$");
        static Dictionary<ulong, string> lastInputs = new Dictionary<ulong, string>();


        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public Character(CommandHandler handler) => this.handler = handler;


        [SlashCommand("char", "Set, create, delete characters.")]
        public async Task CharCommand(CharacterCommand mode, string characterName = "")
        {
            var user = Context.Interaction.User;
            var statblocks = Program.database.GetCollection<StatBlock>("statblocks");
            lastInputs[user.Id] = characterName;

            //find all documents that belong to user, load them into dictionary.
            Pathfinder.Database[user] = new Dictionary<string, StatBlock>();
            var collection = statblocks.Find(x => x.Owner == user.Id).ToList();         
            foreach(var statblock in collection)
            {
                Pathfinder.Database[user][statblock.CharacterName] = statblock;
            }




           

            //options
            if(mode == CharacterCommand.List)
            {
                if(Pathfinder.Database[user].Count == 0)
                {
                    await RespondAsync("You don't have any characters.", ephemeral: true);
                    return;
                }

                var sb = new StringBuilder();
                foreach(var character in Pathfinder.Database[user].Keys)
                {
                    sb.AppendLine(character);
                }
                await RespondAsync(sb.ToString(), ephemeral: true);
            }
            
            if(mode == CharacterCommand.Set)
            {
                if(!validName.IsMatch(characterName))
                {
                    await RespondAsync("Invalid character name.", ephemeral: true);
                    return;
                }

                if(!Pathfinder.Database[user].ContainsKey(characterName))
                {
                    await RespondAsync("Character not found", ephemeral: true);
                    return;
                }

                Pathfinder.Active[user] = Pathfinder.Database[user][characterName];
                await RespondAsync("Character set!", ephemeral: true);
            }

            if(mode == CharacterCommand.New)
            {
                if(!validName.IsMatch(characterName))
                {
                    await RespondAsync("Invalid character name.", ephemeral: true);
                    return;
                }

                if(Pathfinder.Database[user].ContainsKey(characterName))
                {
                    await RespondAsync($"{characterName} already exists.", ephemeral: true);
                    return;
                }

                if(Pathfinder.Database[user].Count >= 5)
                {
                    await RespondAsync("You have too many characters. Delete one before making another.");
                }

                var statblock = StatBlock.DefaultPathfinder(characterName);
                statblock.Owner = user.Id;
                statblocks.InsertOne(statblock);

                await RespondAsync($"Successfully created '{characterName}'", ephemeral: true);                 
            }

            if(mode == CharacterCommand.Delete)
            {                             
                
                if(!Pathfinder.Database[user].ContainsKey(characterName))
                {
                    await RespondAsync("Character not found.", ephemeral: true);
                    return;
                }

                await RespondWithModalAsync<ConfirmModal>("confirm_delete");           
            }
            
        }

        [ModalInteraction("confirm_delete")]
        public async Task ConfirmDeleteChar(ConfirmModal modal)
        {
            var user = Context.Interaction.User;

            if(modal.Confirm != "CONFIRM")
            {
                await RespondAsync("You didn't die. Try again", ephemeral: true);
                return;
            }


            Program.database.GetCollection<StatBlock>("statblocks").DeleteOne(x => x.Owner == user.Id && x.CharacterName == lastInputs[user.Id]);

            Pathfinder.Database[user].Remove(lastInputs[user.Id]);
            
            await RespondAsync($"{lastInputs[user.Id]} removed", ephemeral: true);
        }

    }
}
