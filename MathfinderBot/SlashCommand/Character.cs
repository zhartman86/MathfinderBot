using System.Linq;
using System.Text;
using Discord;
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

        public enum GameType
        {
            Pathfinder,
            FifthEd
        }

        static Regex validName = new Regex(@"^[a-zA-Z' ]{3,25}$");
        static Dictionary<ulong, string> lastInputs = new Dictionary<ulong, string>();

        public InteractionService Service { get; set; }

        private CommandHandler handler;

        public Character(CommandHandler handler) => this.handler = handler;


        [SlashCommand("char", "Set, create, delete characters.")]
        public async Task CharCommand(CharacterCommand mode, string characterName = "", string options = "")
        {
            var user = Context.Interaction.User.Id;
            var statblocks = Program.database.GetCollection<StatBlock>("statblocks");
            var nameToUpper = characterName.ToUpper();
            lastInputs[user] = characterName;

            //find all documents that belong to user, load them into dictionary.
            Pathfinder.Database[user] = new Dictionary<string, StatBlock>();
            var collection = statblocks.Find(x => x.Owner == user).ToList();         
            foreach(var statblock in collection)
            {
                Pathfinder.Database[user][statblock.CharacterName] = statblock;
            }          

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

                Pathfinder.SetActive(user, Pathfinder.Database[user][characterName]);
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

                var split = options.Split(new char[] { ',', ' ', ':' });
                List<int> scores = new List<int>();
                if(split.Length == 6)
                {
                    
                    int intOut = 0;
                    for(int i = 0; i < split.Length; i++)
                    {
                        if(int.TryParse(split[i], out intOut))
                        {
                            scores.Add(intOut);
                        }
                    }
                }

                var statblock = StatBlock.DefaultPathfinder(characterName);
                statblock.Owner = user;
                var scoreArray = scores.ToArray();
                if(scoreArray.Length == 6)
                {
                    statblock.Stats["STR_SCORE"] = scores[0];
                    statblock.Stats["DEX_SCORE"] = scores[1];
                    statblock.Stats["CON_SCORE"] = scores[2];
                    statblock.Stats["INT_SCORE"] = scores[3];
                    statblock.Stats["WIS_SCORE"] = scores[4];
                    statblock.Stats["CHA_SCORE"] = scores[5];
                }
                               
                await statblocks.InsertOneAsync(statblock);


                var eb = new EmbedBuilder()
                    .WithColor(Color.DarkPurple)
                    .WithTitle("New Character")
                    .WithDescription($"{Context.User.Mention} has created a new character, {characterName}.\n\n “{Stuff.GetRandomQuote(characterName)}”" );

                await RespondAsync(embed: eb.Build());                 
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
            var user = Context.Interaction.User.Id;

            if(modal.Confirm != "CONFIRM")
            {
                await RespondAsync("You didn't die. Try again", ephemeral: true);
                return;
            }


            Program.database.GetCollection<StatBlock>("statblocks").DeleteOne(x => x.Owner == user && x.CharacterName == lastInputs[user]);

            Pathfinder.Database[user].Remove(lastInputs[user]);
            
            await RespondAsync($"{lastInputs[user]} removed", ephemeral: true);
        }

    }
}
