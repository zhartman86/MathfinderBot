using Discord;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace MathfinderBot
{
    public class Character : InteractionModuleBase
    {
        public enum CharacterCommand
        {
            Set,
            List,
            Export,

            [ChoiceDisplay("Change-Name")]
            ChangeName,

            Update,
            Add,
            New,
            Delete
        }

        public enum InventoryAction
        {
            [ChoiceDisplay("Add-New")]
            Add,

            [ChoiceDisplay("Add-List")]
            AddList,

            Edit,
            Import,
            Export,
            Remove,
            List
        }

        public enum SheetType
        {
            [ChoiceDisplay("Pathbuilder (PDF)")]
            Pathbuilder,

            [ChoiceDisplay("HeroLabs (XML)")]
            HeroLabs,

            [ChoiceDisplay("PCGen (XML)")]
            PCGen,

            [ChoiceDisplay("Mottokrosh (JSON)")]
            Mottokrosh,

            [ChoiceDisplay("Scribe (TXT)")]
            Scribe,

            [ChoiceDisplay("Mathfinder (JSON)")]
            Mathfinder,
        }

        static readonly Dictionary<ulong, ulong> secMessages = new Dictionary<ulong, ulong>();
        public static readonly Dictionary<string, ulong> challenges = new Dictionary<string, ulong>();
        static readonly Dictionary<ulong, ulong> interactionMessages = new Dictionary<ulong, ulong>();

        static Regex validName = new Regex(@"[a-zA-Z' ]{3,75}");
        static Regex validItemInput = new Regex(@"[a-zA-Z'0-9 :]{3,75}");

        ulong user;
        public InteractionService Service { get; set; }

        public override async void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;
            await Characters.GetCharacter(user);
        }

        async Task CharacterAdd(string character, SheetType sheetType, IAttachment file)
        {

            if(Characters.Database[user].Count == 0)
            {
                var global = new StatBlock() { Owner = user, CharacterName = "$GLOBAL" };
                await Program.InsertStatBlock(global);
            }

            if(!validName.IsMatch(character))
            {
                await RespondAsync("Invalid character name.", ephemeral: true);
                return;
            }

            var stats = await UpdateStats(sheetType, file, name: character);
            if(stats != null)
            {
                stats.Owner = user;
                await Program.InsertStatBlock(stats);
                var json = JsonConvert.SerializeObject(stats, Formatting.Indented);
                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(json));
                await FollowupWithFileAsync(stream, $"{stats.CharacterName}.txt", ephemeral: true);
                return;
            }
            else
                await FollowupAsync("Failed to create character", ephemeral: true);
        }

        async Task CharacterChangeName(string newName)
        {
            if(Characters.Active[user].CharacterName == "$GLOBAL")
            {
                await RespondAsync("Cannot rename the global space. If you wish to rename another character, first set it to active.", ephemeral: true);
                return;
            }

            if(newName != "" && validName.IsMatch(newName))
            {
                if(!Characters.Database[user].Any(x => x.CharacterName.ToUpper() == newName.ToUpper()))
                {
                    var oldName = Characters.Active[user].CharacterName;
                    Characters.Active[user].CharacterName = newName;
                    var update = Builders<StatBlock>.Update.Set(x => x.CharacterName, Characters.Active[user].CharacterName);
                    Program.Database.UpdateOne(Characters.Active[user], update);
                    await RespondAsync($"{oldName} changed to {Characters.Active[user].CharacterName}", ephemeral: true);
                    return;
                }
                else
                    await RespondAsync("Name already in use", ephemeral: true);

            }
            else
                await RespondAsync("Invalid name", ephemeral: true);
        }

        async Task CharacterDelete(string character, ulong user)
        {
            if(Characters.Database[user].Any(x => x.CharacterName.ToUpper() == character.ToUpper()))
            {
                var interactionId = Context.Interaction.Id;
                var cb = new ComponentBuilder()
                    .WithButton("DELETE", $"delete:{character},{interactionId}", style: ButtonStyle.Danger);
                await RespondAsync($"Press DELETE to permanently remove {character}", components: cb.Build(), ephemeral: true);
                var msg = await GetOriginalResponseAsync();
                interactionMessages[interactionId] = msg.Id;
                return;
            }
            await RespondAsync("Character not found.", ephemeral: true);
            return;
        }

        async Task CharacterExport(string character, List<StatBlock> characters)
        {
            var outVal = 0;
            var toUpper = character.ToUpper();
            StatBlock statblock = null;
            if(int.TryParse(toUpper, out outVal) && outVal >= 0 && outVal < characters.Count)
                statblock = characters[outVal];
            else if(validName.IsMatch(toUpper))
                statblock = characters.FirstOrDefault(x => x.CharacterName.ToUpper() == toUpper)!;

            if(statblock != null)
            {
                var json = JsonConvert.SerializeObject(statblock, Formatting.Indented);
                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(json));

                await RespondWithFileAsync(stream, $"{statblock.CharacterName}.txt", ephemeral: true);
                return;
            }
            await RespondAsync($"{character} not found.", ephemeral: true);
            return;
        }

        async Task CharacterList(List<StatBlock> chars)
        {
            var task = Task.Run(() =>
            {
                var sb = new StringBuilder();

                if(chars.Count == 0) sb.AppendLine("You don't have any characters.");
                for(int i = 0; i < chars.Count; i++)
                    sb.AppendLine($"{i} — {chars[i].CharacterName}");

                return $"```{sb}```";
            });
            await RespondAsync(await task, ephemeral: true);

        }

        async Task CharacterNew(string character, ulong user)
        {
            if(!validName.IsMatch(character))
            {
                await RespondAsync("Invalid character name.", ephemeral: true);
                return;
            }

            var stats = await Program.GetStatBlocks().FindAsync(x => x.Owner == user && x.CharacterName.ToUpper() == character.ToUpper());


            if(stats.Any())
            {
                await RespondAsync($"{character} already exists.", ephemeral: true);
                return;
            }

            if(Characters.Database[user].Count >= 20)
            {
                await RespondAsync("You have too many characters. Delete one before making another.");
                return;
            }
            if(Characters.Database[user].Count == 0)
            {
                var global = new StatBlock() { Owner = user, CharacterName = "$GLOBAL" };
                await Program.InsertStatBlock(global);
            }

            StatBlock statblock = StatBlock.DefaultPathfinder(character);

            statblock.Owner = user;
            await Program.InsertStatBlock(statblock);

            var quote = Quotes.Get(character);

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithTitle($"New-Character({character})")
                .WithDescription($"{Context.User.Mention} has created a new character, {character}.\n\n “{quote}”");

            Characters.Active[user] = statblock;

            await RespondAsync(embed: eb.Build());
            return;
        }

        async Task CharacterSet(string character, List<StatBlock> chars)
        {
            var index = -1;

            if(int.TryParse(character, out int outVal) && outVal >= 0 && outVal < chars.Count)
                index = outVal;
            else
                index = chars.FindIndex(x => x.CharacterName.ToUpper() == character.ToUpper());

            if(index != -1)
            {
                await Characters.SetActive(user, chars[index]);
                await RespondAsync($"{Characters.Active[user].CharacterName} set. {(Characters.Active[user].Global != null ? "[GLOBAL FOUND]" : "")} ", ephemeral: true);
            }
            else
                await RespondAsync("Character not found", ephemeral: true);
        }

        async Task CharacterUpdate(SheetType sheetType, IAttachment file, ulong user)
        {
            var stats = await UpdateStats(sheetType, file, Characters.Active[user]);
            if(stats != null)
            {
                await Characters.SetActive(user, stats);
                await Program.UpdateStatBlock(stats);
                await FollowupAsync("Updated!", ephemeral: true);
            }
            else
                await FollowupAsync("Failed to update sheet", ephemeral: true);
        }


        public async Task<StatBlock> UpdateStats(SheetType sheetType, IAttachment file, StatBlock stats = null, string name = "")
        {
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(file.Url);
            var stream = new MemoryStream(data);

            if(stream != null)
            {
                if(file.Filename.ToUpper().Contains(".PDF") || file.Filename.ToUpper().Contains(".XML") || file.Filename.ToUpper().Contains(".TXT") || file.Filename.ToUpper().Contains(".JSON"))
                {
                    await RespondAsync("Updating sheet...", ephemeral: true);

                    if(stats == null) stats = StatBlock.DefaultPathfinder(name);

                    if(sheetType == SheetType.Mathfinder)
                        stats = JsonConvert.DeserializeObject<StatBlock>(Encoding.UTF8.GetString(data))!;
                    if(sheetType == SheetType.Pathbuilder)
                        stats = await Utility.UpdateWithPathbuilder(stream, stats);
                    if(sheetType == SheetType.HeroLabs)
                        stats = Utility.UpdateWithHeroLabs(stream, stats);
                    if(sheetType == SheetType.PCGen)
                        stats = Utility.UpdateWithPCGen(stream, stats);
                    if(sheetType == SheetType.Mottokrosh)
                        stats = Utility.UpdateWithMotto(data, stats);
                    if(sheetType == SheetType.Scribe)
                        stats = await Utility.UpdateWithScribe(stream, stats);
                    Console.WriteLine("Done!");

                    return stats;
                }
            }
            return null!;
        }

        [SlashCommand("char", "Modify statblocks.")]
        public async Task CharCommand(CharacterCommand action, string character = "", SheetType sheetType = SheetType.Pathbuilder, IAttachment file = null, IUser target = null)
        {
            //find all documents that belong to user, load them into dictionary.
            var coll = await Program.GetStatBlocks().FindAsync(x => x.Owner == user);
            Characters.Database[user] = coll.ToList();
            var chars = Characters.Database[user];


            switch(action)
            {
                case CharacterCommand.Set:
                    await CharacterSet(character, chars).ConfigureAwait(false);
                    return;
                case CharacterCommand.Export:
                    await CharacterExport(character, chars).ConfigureAwait(false);
                    return;
                case CharacterCommand.Add:
                    await CharacterAdd(character, sheetType, file).ConfigureAwait(false);
                    return;
                case CharacterCommand.New:
                    await CharacterNew(character, user).ConfigureAwait(false);
                    return;
                case CharacterCommand.ChangeName:
                    await CharacterChangeName(character).ConfigureAwait(false);
                    return;
                case CharacterCommand.Update:
                    await CharacterUpdate(sheetType, file, user).ConfigureAwait(false);
                    return;
                case CharacterCommand.List:
                    await CharacterList(Characters.Database[user]).ConfigureAwait(false);
                    return;
                case CharacterCommand.Delete:
                    await CharacterDelete(character, user).ConfigureAwait(false);
                    return;
            }
        }

        [ComponentInteraction("delete:*,*")]
        public async Task ButtonPressedDelete(string name, ulong interactionId)
        {
            var user = Context.User.Id;
            Console.WriteLine(name);

            var stats = Characters.Database[user].FirstOrDefault(x => x.CharacterName.ToUpper() == name.ToUpper())!;
            if(stats != null)
            {
                Characters.Database[user].Remove(stats);
                await Program.DeleteOneStatBlock(stats);
                if(Characters.Active[user].Id == stats.Id)
                    await Characters.GetCharacter(user);
                await RespondAsync($"`{stats.CharacterName}` removed", ephemeral: true);
                if(interactionMessages.ContainsKey(interactionId))
                {
                    await Context.Channel.DeleteMessageAsync(interactionMessages[interactionId]);
                    interactionMessages.Remove(interactionId);
                }
                return;
            }
        }

    }
}