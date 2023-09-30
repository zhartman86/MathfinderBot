using System.Text;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using MongoDB.Driver;
using Discord;
using Discord.WebSocket;

namespace MathfinderBot
{
    public class Variable : InteractionModuleBase
    {
        public enum AbilityScoreDmg
        {
            [ChoiceDisplay("None")]
            BONUS,
            
            STR,
            DEX,
            CON,
            INT,
            WIS,
            CHA
        }

        public enum ModAction
        {
            List,
            Add,
            Remove
        }
    
        public enum AbilityScoreHit
        {          
            STR,
            DEX,
            CON,
            INT,
            WIS,
            CHA
        }

        public enum EquipAction
        {
            Add,
            List
        }

        public enum VarAction
        {
            [ChoiceDisplay("Set-Expression")]
            SetExpr,

            [ChoiceDisplay("Set-Row")]
            SetRow,
            
            [ChoiceDisplay("List-Vars")]
            ListVars,        

            [ChoiceDisplay("List-Bonuses")]
            ListBonus,            
            
            [ChoiceDisplay("Remove-Variable")]
            Remove
        }
        
        static readonly Dictionary<string, int> sizes = new Dictionary<string, int>(){
            { "Fine",        0 },
            { "Diminutive",  1 },
            { "Tiny",        2 },
            { "Small",       3 },
            { "Medium",      4 },
            { "Large",       5 },
            { "Huge",        6 },
            { "Gargantuan",  7 },
            { "Colossal",    8 }};

        static readonly Regex validVar  = new Regex(@"^[^\[\]<>(){}^@:+*/%=!&|;$#?\-.'""0-9]*$");
        static readonly Regex validExpr = new Regex(@"^(.*){1,400}$");
              
        public static ExprRow                   exprRowData = null!;
        ulong                                   user;       

        static byte[] bestiary  = null!;
        static byte[] items     = null!;
        static byte[] rules     = null!;
        static byte[] shapes    = null!;       
        static byte[] spells    = null!;

        public async override void BeforeExecute(ICommandInfo command)
        { 
            user = Context.Interaction.User.Id;
            await Characters.GetCharacter(user);
        }
        
        async Task VarList()
        {
            var sb = new StringBuilder();

            sb.AppendLine("__STATS__");
            foreach(var stat in Characters.Active[user].Stats)
                sb.AppendLine($"|{stat.Key,-20} |{stat.Value,-25}");
            sb.AppendLine();
            sb.AppendLine("__EXPRESSIONS__");
            foreach(var expr in Characters.Active[user].Expressions)
                sb.AppendLine($"|{expr.Key,-15} |{expr.Value.ToString(),-50}");
            sb.AppendLine();
            sb.AppendLine("__ROWS__");
            foreach(var row in Characters.Active[user].ExprRows.Keys)
                sb.AppendLine($"{row}");

            using var stream = new MemoryStream(Encoding.Default.GetBytes(sb.ToString()));
            await RespondWithFileAsync(stream, $"Vars.{Characters.Active[user].CharacterName}.txt", ephemeral: true);
        }

        async Task VarListBonuses()
        {
            var sb = new StringBuilder();
            foreach(var stat in Characters.Active[user].Stats)
            {
                if(stat.Value.Bonuses.Count > 0 || stat.Value.Override != null)
                {
                    sb.AppendLine("```");
                    sb.AppendLine(stat.Key);
                    if(stat.Value.Override != null)
                        sb.AppendLine($"  |OVERRIDE: {stat.Value.Override.Name,-9} |{stat.Value.Override.Value,-3}");
                    foreach(var bonus in stat.Value.Bonuses)
                        sb.AppendLine($"  |{bonus.Name,-9} |{bonus.Type,-10} |{bonus.Value,-3}");
                    sb.Append("```");
                }
            }

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkGreen)
                .WithTitle("List-Bonuses()")
                .WithDescription(sb.ToString());

            await RespondAsync(embed: eb.Build(), ephemeral: true);
        }

        async Task VarSetExpr(string varName)
        {
            if(Characters.Active[user].Stats.ContainsKey(varName) || Characters.Active[user].ExprRows.ContainsKey(varName))
            {
                await RespondAsync($"`{varName}` already exists as a stat or row. If you intend to change the variable type, first remove the old one.", ephemeral: true);
                return;
            }

            var mValue = "";
            if(Characters.Active[user].Expressions.ContainsKey(varName))
                mValue = Characters.Active[user].Expressions[varName];

            var mb = new ModalBuilder("Set-Expression", $"set_expr:{varName}")
                .AddTextInput(new TextInputBuilder($"{varName}", "expr", value: mValue));
            
            await RespondWithModalAsync(mb.Build());
            return;
        }

        async Task VarRemove(string varName)
        {
            if(Characters.Active[user].Stats.ContainsKey(varName))
            {
                Characters.Active[user].RemoveStat(varName);      
                await RespondAsync($"`{varName}` removed from stats.", ephemeral: true);
                return;
            }
            else if(Characters.Active[user].Expressions.ContainsKey(varName))
            {
                Characters.Active[user].RemoveExpr(varName);           
                await RespondAsync($"`{varName}` removed from expressions.", ephemeral: true);
                return;
            }
            else if(Characters.Active[user].ExprRows.ContainsKey(varName))
            {
                Characters.Active[user].RemoveExprRow(varName);
                await RespondAsync($"`{varName}` removed from rows.", ephemeral: true);
                return;
            }        

            await RespondAsync($"No variable `{varName}` found.", ephemeral: true);
            return;
        }

        [SlashCommand("var", "Manage variables.")]
        public async Task Var(VarAction action, string varName = "")
        {
            user = Context.Interaction.User.Id;         

            var varToUpper = varName.ToUpper().Replace(' ', '_');
            if(varName != "" && !validVar.IsMatch(varToUpper))
            {
                await RespondAsync($"Invalid variable `{varToUpper}`. Numbers and most special characters are forbidden.", ephemeral: true);
                return;
            }

            switch(action)
            {
                case VarAction.ListVars:
                    await VarList();
                    return;
                case VarAction.ListBonus:
                    await VarListBonuses();
                    return;
                case VarAction.SetRow:
                    await RespondWithModalAsync<ExprRowModal>("set_row");
                    return;
                case VarAction.SetExpr:
                    await VarSetExpr(varToUpper);
                    return;
                case VarAction.Remove:
                    await VarRemove(varToUpper);
                    return;
            }
        }

        [ModalInteraction("set_row")]
        public async Task NewRow(ExprRowModal modal)
        {
            user = Context.Interaction.User.Id;

            var exprs = modal.Expressions.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var row = new ExprRow() { RowName = modal.Name };
            
            for(int i = 0; i < exprs.Length; i++)
            {
                if(validExpr.IsMatch(exprs[i]))
                {
                    var split = exprs[i].Split('#');
                    if(split.Length == 2)
                        row.Set.Add(new Expr { Name = split[0], Expression = split[1] });
                    else if(split.Length == 1)
                        row.Set.Add(new Expr { Name = split[0], Expression = split[0] });
                }
                else
                {
                    await RespondAsync($"Invalid Input @ Expression {i + 1}", ephemeral: true);
                    return;
                }
            }                                 

            Characters.Active[user].AddExprRow(row);
            var eb = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithTitle($"New-Row({row.RowName})");

            for(int i = 0; i < row.Set.Count; i++)
                if(!string.IsNullOrEmpty(row.Set[i].Name))
                    eb.AddField(name: row.Set[i].Name, value: row.Set[i].Expression, inline: true);

            await RespondAsync(embed: eb.Build(), ephemeral: true);
        }

        [SlashCommand("row", "Get one or many rows (up to 5)")]
        public async Task GetRowCommand([Autocomplete] string rowName)
        {
            user = Context.Interaction.User.Id;
            var character = await Characters.GetCharacter(user);
           
            if(character.ExprRows.Keys.Any(x => x.Replace(" ", "_").ToUpper() == rowName.Replace(" ", "_").ToUpper()))
            {
                var cb = BuildRow(character.ExprRows[rowName]);
                await RespondAsync(components: cb.Build(), ephemeral: true);
            }
            else
                await RespondAsync("Row now found", ephemeral: true);    
        }  
      
        [SlashCommand("best", "List creature by name or index number")]
        public async Task BestiaryCommand([Summary("creature_name"), Autocomplete] string nameOrNumber = "", bool showInfo = false)
        {
            
            if(nameOrNumber == "")
            {
                if(bestiary == null)   
                    bestiary = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListBestiary());
                using var stream = new MemoryStream(bestiary);
                await RespondWithFileAsync(stream, $"Bestiary.txt", ephemeral: true);
                return;
            }      

            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Bestiary.FirstOrDefault(x => x.Name!.ToUpper() == nameOrNumber.ToUpper());
            if(nameVal != null)
                outVal = DataMap.BaseCampaign.Bestiary.IndexOf(nameVal);
            else if(!int.TryParse(nameOrNumber, out outVal))
            {
                await RespondAsync($"{nameOrNumber} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Bestiary.Count)
            {
                var creature = DataMap.BaseCampaign.Bestiary[outVal];
                Embed[] ebs = null;
                var sa = creature.GetSpecialAbilities();

                if(showInfo)
                {
                    if(sa != null)
                        ebs = new Embed[2] { new EmbedBuilder().WithDescription(creature.ToString()).Build(), new EmbedBuilder().WithDescription(sa).Build() };
                    else
                        ebs = new Embed[1] { new EmbedBuilder().WithDescription(creature.ToString()).Build() };
                }
                else ebs = new Embed[1] { new EmbedBuilder().WithDescription(creature.GetSmallBlock()).Build()};


                var regex = new Regex(@"(^(?:or )?[+]?[0-9a-z ]*)(?:([-+][0-9]{1,2})?[/]?)* \(([0-9]{1,2}d[0-9]{1,2}(?:[-+][0-9]{1,3})?)(?:.*([+][0-9]{1,2}d[0-9]{1,2}).*\))*");
                string[] melee  = new string[5] { creature.MeleeOne!, creature.MeleeTwo!, creature.MeleeThree!, creature.MeleeFour!, creature.MeleeFive! };
                string[] ranged = new string[2] { creature.RangedOne!, creature.RangedTwo! };

                
                var cb = new ComponentBuilder();
                if(melee[0] != "")
                    for(int i = 0; i < melee.Length; i++)
                        if(melee[i] != "")
                        {
                            var match = regex.Match(melee[i]);
                            if(match.Success && match.Groups.Count > 3)
                            {
                                var row = new ActionRowBuilder();
                                for(int j = 0; j < match.Groups[2].Captures.Count; j++)
                                {
                                    Console.WriteLine($"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i + i * i}");
                                    if(j == 0)
                                        row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i}", label: $"{match.Groups[1].Value} {match.Groups[2].Captures[j].Value}");
                                    else if(j < 4)
                                        row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i}", label: match.Groups[2].Captures[j].Value);
                                }
                                row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},{match.Groups[3].Value}{(match.Groups[4].Success ? match.Groups[4].Value : "")},Damage{i}", label: $"{match.Groups[3].Value}{(match.Groups[4].Success ? match.Groups[4].Value : "")}");
                                cb.AddRow(row);
                            }
                        }
                

                await RespondAsync(embeds: ebs, components: cb.Build(), ephemeral: true);

                if(ranged[0] != "")
                {                    
                    cb = new ComponentBuilder();
                    for(int i = 0; i < ranged.Length; i++)
                        if(ranged[i] != "")
                        {
                            var match = regex.Match(ranged[i]);
                            if(match.Success) Console.WriteLine("match");
                            if(match.Success && match.Groups.Count > 3)
                            {
                                var row = new ActionRowBuilder();
                                for(int j = 0; j < match.Groups[2].Captures.Count; j++)
                                {
                                    if(j == 0)
                                        row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i}", label: $"{match.Groups[1].Value} {match.Groups[2].Captures[j].Value}");
                                    else
                                        row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},1d20{match.Groups[2].Captures[j].Value},{j + i}", label: match.Groups[2].Captures[j].Value);
                                }
                                row.WithButton(customId: $"rowbest:{creature.Name!.Replace(" ", "")},{match.Groups[3].Value}{(match.Groups[4].Success ? match.Groups[4].Value : "")},Damage{i}", label: $"{match.Groups[3].Value}{(match.Groups[4].Success ? match.Groups[4].Value : "")}");
                                cb.AddRow(row);
                            }
                        }
                    await FollowupAsync(components: cb.Build(), ephemeral: true);
                }             
                
                return;            
            }
        }
        
        [ComponentInteraction("rowbest:*,*,*")]
        public async Task ButtonPressedBest(string creatureName, string expr, string name)
        {
            user = Context.Interaction.User.Id;

            var sb = new StringBuilder();
            var result = Parser.Parse(expr).Eval(null, sb);

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle($"{result}")
                .WithDescription($"{creatureName}")
                .WithFooter($"{expr}");

            if(sb.Length > 0) builder.AddField($"__Events__", $"{sb}");

            await RespondAsync(embed: builder.Build());
        }

        static async Task<InvItem> ConvertItem(Item item)
        {
            var task = Task.Run(() =>
            {
                return new InvItem()
                {
                    Base = item.Name!,
                    Name = item.Name!,
                    Quantity = 1,
                    Value = decimal.TryParse(item.Price, out decimal outVal) ? outVal : 0m,
                    Weight = item.Weight!.Value,
                };
            });
            return await task;
        }

        [ComponentInteraction("add_item:*,*")]
        public async Task ButtonPressedAddItem(int index, int custom = 0)
        {
            if(!Characters.Active.ContainsKey(user)) return;
            
            var item = DataMap.BaseCampaign.Items[index];
            if(custom != 0)
                await RespondWithModalAsync(CreateBaseItemModal(item).Build());
            else
            {
                Characters.Active[user].InventoryAdd(await ConvertItem(item));
                await RespondAsync($"{item.Name} added", ephemeral: true);
            }
        }

        [ComponentInteraction("apply_item:*")]
        public async Task ButtonPressedApplyItem(int index)
        {
            if(!Characters.Active.ContainsKey(user)) return;

            var formulae = DataMap.BaseCampaign.Items[index].Formulae!.Split(';');
            var sb = new StringBuilder();
            sb.AppendLine(); sb.AppendLine();
            for(int i = 0; i < formulae.Length; i++)
                Parser.Parse(formulae[i]).Eval(Characters.Active[user], sb);
            var eb = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithAuthor(Context.Interaction.User.Username, Context.Interaction.User.GetAvatarUrl())
                .WithDescription($"*{Characters.Active[user].CharacterName}* {sb}");
            await RespondAsync(embed: eb.Build(), ephemeral: true);
        }

        [SlashCommand("item", "Item and inventory management")]
        public async Task ItemCommand([Summary("item_name"), Autocomplete] string nameOrNumber = "", SizeType size = SizeType.Medium, bool isHidden = true)
        {
            user = Context.User.Id;
            var index = -1;    

            if(nameOrNumber != "")
            {
                if(int.TryParse(nameOrNumber, out int outVal) && outVal >= 0 && outVal < DataMap.BaseCampaign.Items.Count)
                    index = outVal;
                else
                    index = DataMap.BaseCampaign.Items.FindIndex(x => x.Name!.ToUpper() == nameOrNumber.ToUpper())!;              
            }

            if(index == -1)
            {
                if(items == null)
                    items = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListItems());
                using var stream = new MemoryStream(items!);
                await RespondWithFileAsync(stream, $"Items.txt", ephemeral: true);
            }
            else
            {
                var item = DataMap.BaseCampaign.Items[index];
                var eb = new EmbedBuilder()
                    .WithDescription(item.ToString());             

                if(Characters.Active.ContainsKey(user))
                {
                    var cb = new ComponentBuilder()
                        .WithButton("Add", $"add_item:{index},0")
                        .WithButton("Custom", $"add_item:{index},1");
                    if(item.Formulae != "")
                        cb.WithButton("Apply", $"apply_item:{index}");
                    if(item.Type == "Weapon")
                        cb.WithButton("Expressions", $"new_row:{index},{(int)size}");
                    await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: isHidden);
                }
                else
                    await RespondAsync(embed: eb.Build(), ephemeral: true);
            }
            return;
        }

        [ComponentInteraction("new_row:*,*")]
        async public Task NewRowInteraction(int id, int size)
        {
;           var item = DataMap.BaseCampaign.Items[id];
            await RespondWithModalAsync(CreateRowModal(item.Name!, CreateWeaponExpressions(item, size)).Build());
        }

        static ModalBuilder CreateRowModal(string name, string exprs)
        {         
            var mb = new ModalBuilder()
                .WithCustomId($"new_row")
                .WithTitle("New-Row")
                .AddTextInput("Name", "row_name", value: name.ToUpper())
                .AddTextInput("Expressions", "item_exprs", TextInputStyle.Paragraph, value: exprs);
            return mb;
        }

        static ModalBuilder CreateBaseItemModal(Item item)
        {
            var mb = new ModalBuilder()
                    .WithCustomId($"base_item:{item.Name}")
                    .WithTitle($"Add-Item: {item.Name}")
                    .AddTextInput("Custom Name", "item_custom", value: item.Name, maxLength: 50, required: false)
                    .AddTextInput("Quantity", "item_qty", value: "1")                    
                    .AddTextInput("Weight", "item_weight", value: item.Weight.ToString(), maxLength: 20)
                    .AddTextInput("Value", "item_value", value: item.Price)                    
                    .AddTextInput("Notes", "item_notes", TextInputStyle.Paragraph, required: false);
            return mb;        
        }

        static string CreateWeaponExpressions(Item item, int size)
        {
            var split = item.Offense!.Split('/');            
            var weaponSize = split[sizes[Enum.GetName(typeof(SizeType), size)!]];

            var qualities = split[11].Split('&');
            var damages = weaponSize!.Split('&', options: StringSplitOptions.RemoveEmptyEntries);
            var categories = split[14].Split('&');
            var sb = new StringBuilder();
            for(int i = 0; i < categories.Length; i++)
            {
                for(int j = 0; j < damages.Length; j++)
                {
                    switch(categories[i])
                    {
                        case "Light":
                        case "One-Handed":
                            if(j == 0) sb.AppendLine($"{item.Name}#ATK_STR");
                            if(j == 0 || (j > 0 && damages[j] != damages[j - 1])) sb.AppendLine($"{damages[j]}#{damages[j]}+DMG_STR");
                            break;
                        case "Two-Handed":
                            if(j == 0) sb.AppendLine($"{item.Name}#ATK_STR");
                            if(j == 0 || (j > 0 && damages[j] != damages[j - 1])) sb.AppendLine($"{damages[j]}#{damages[j]}+th(DMG_STR)");
                            Console.WriteLine(damages[j]);
                            break;
                        case "Ranged":
                            if(j == 0) sb.AppendLine($"{item.Name}#ATK_DEX");
                            break;
                        case "Thrown":
                            if(j == 0) sb.AppendLine($"Throw#ATK_DEX");
                            if(i == 0 && (j == 0 || (j > 0 && damages[j] != damages[j - 1]))) sb.AppendLine($"{damages[j]}#{damages[j]}+DMG_STR");
                            break;
                    }
                }              
            }
            
            for(int i = 0; i < qualities.Length; i++)
            {
                switch(qualities[i])
                {
                    case "disarm":
                        sb.AppendLine("Disarm:DISARM + 2");
                        break;
                    case "distracting":
                        sb.AppendLine("Distracting:BLF + 2");
                        break;
                    case "sunder":
                        sb.Append("Sunder:SUNDER + 2");
                        break;
                }
            }
            
            return sb.ToString();                                     
        }
      
        [SlashCommand("rule", "General rules, conditions, class abilities")]
        public async Task RuleCommand([Summary("rule_name"), Autocomplete] string name = "", bool isHidden = true)
        {
            if(name == "")
            {
                if(rules == null)
                    rules = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListRules());
                using var stream = new MemoryStream(rules);
                await RespondWithFileAsync(stream, $"Rules.txt", ephemeral: true);
                return;
            }

            var toUpper = name.ToUpper().Replace('_', ' ');
            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Rules.FirstOrDefault(x => x.Name!.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.BaseCampaign.Rules.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Rules.Count)
            {
                var rule = DataMap.BaseCampaign.Rules[outVal];

                var eb = new EmbedBuilder()
                    .WithColor(255, 130, 130)
                    .WithDescription(rule.ToString());
                
                var cb = await BuildFormulaeComponents(rule.Formulae!);

                await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: isHidden);
            }
        }

        [SlashCommand("shape", "Generate attacks based on a creature's shape")]
        public async Task PresetShapeCommand([Summary("shape_name"), Autocomplete] string nameOrNumber = "", AbilityScoreHit hitMod = AbilityScoreHit.STR, bool multiAttack = false)
        { 
            if(nameOrNumber == "")
            {
                if(shapes == null)
                    shapes = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListShapes());
                using var stream = new MemoryStream(shapes);
                await RespondWithFileAsync(stream, $"Shapes.txt", ephemeral: true);
                return;
            }
   
            user = Context.Interaction.User.Id;

            var toUpper = nameOrNumber.ToUpper().Replace(' ', '_');
            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Shapes.FirstOrDefault(x => x.Name.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.BaseCampaign.Shapes.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Shapes.Count)
            {
                var shape = DataMap.BaseCampaign.Shapes[outVal];

                var primary = new List<(string, string)>();
                var secondary = new List<(string, string)>();

                if(shape.Bite != "")        primary.Add(("bite", shape.Bite!));
                if(shape.Claws != "")       primary.Add(("claw", shape.Claws!));
                if(shape.Gore != "")        primary.Add(("gore", shape.Gore!));
                if(shape.Slam != "")        primary.Add(("slam", shape.Slam!));
                if(shape.Sting != "")       primary.Add(("sting", shape.Sting!));
                if(shape.Talons != "")      primary.Add(("talon", shape.Talons!));

                if(shape.Hoof != "")        secondary.Add(("hoof", shape.Hoof!));
                if(shape.Tentacle != "")    secondary.Add(("tentacle", shape.Tentacle!));
                if(shape.Wing != "")        secondary.Add(("wing", shape.Wing!));
                if(shape.Pincers != "")     secondary.Add(("pincer", shape.Pincers!));
                if(shape.Tail != "")        secondary.Add(("tail", shape.Tail!));

                if(shape.Other != "")
                {
                    var oSplit = shape.Other!.Split('/');
                    for(int i = 0; i < oSplit.Length; i++)
                    {
                        var split = oSplit[i].Split(':');
                        if(split.Length > 2)
                            primary.Add((split[1], split[2]));
                        else if(split.Length > 1)
                            secondary.Add((split[0], split[1]));
                        else
                            secondary.Add(("other", split[0]));
                    }
                }

                var list = new List<ExprRow>();

                if(primary.Count > 0)
                {
                    var row = new ExprRow();
                    row.Set.Add(new Expr()
                    {
                        Name = $"primary",
                        Expression = $"ATK_{Enum.GetName(typeof(AbilityScoreHit), hitMod)}"
                    });

                    for(int i = 0; i < primary.Count; i++)
                    {
                        var split = primary[i].Item2.Split('/');
                        for(int j = 0; j < split.Length; j++)
                        {

                            var splitCount = split[j].Split(':');
                            if(splitCount.Length > 1) row.Set.Add(new Expr() { Name = $"{splitCount[0]} {primary[i].Item1}s ({splitCount[1]})", Expression = $"{splitCount[1]}+DMG_STR" });
                            else row.Set.Add(new Expr() { Name = $"{primary[i].Item1} ({splitCount[0]})", Expression = $"{splitCount[0]}+DMG_STR" });
                        }
                    }
                    list.Add(row);
                }
                if(secondary.Count > 0)
                {
                    var row = new ExprRow();
                    var secondaryMod = multiAttack ? "2" : "5";
                    row.Set.Add(new Expr()
                    {
                        Name = "secondary",
                        Expression = $"ATK_{Enum.GetName(typeof(AbilityScoreHit), hitMod)}-{secondaryMod}"
                    });

                    for(int i = 0; i < secondary.Count; i++)
                    {
                        var split = secondary[i].Item2.Split('/');
                        for(int j = 0; j < split.Length; j++)
                        {
                            Console.WriteLine(split[j]);
                            var splitCount = split[j].Split(':');
                            if(splitCount.Length > 1) row.Set.Add(new Expr() { Name = $"{splitCount[0]} {secondary[i].Item1}s ({splitCount[1]})", Expression = splitCount[1] });
                            else row.Set.Add(new Expr() { Name = $"{secondary[i].Item1} ({splitCount[0]})", Expression = splitCount[0] });
                        }
                    }
                    list.Add(row);
                }

                if(shape.Breath != "")
                {
                    var row = new ExprRow();
                    row.Set.Add(new Expr() { Name = $"breath[{shape.Breath}]", Expression = shape.Breath });
                    list.Add(row);
                }

                var cb = BuildRow(list);
                var eb = new EmbedBuilder()
                    .WithDescription(shape.ToString());

                await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: true);
            }
        }

        [SlashCommand("spell", "Get spell info")]
        public async Task PresetSpellCommand([Summary("spell_name"), Autocomplete] string nameOrNumber = "", uint? casterLevel = null, bool isHidden = true, bool metamagic = false)
        {     
            if(nameOrNumber == "")
            {
                if(spells == null)
                    spells = Encoding.ASCII.GetBytes(DataMap.BaseCampaign.ListSpells());
                using var stream = new MemoryStream(spells);
                await RespondWithFileAsync(stream, $"Spells.txt", ephemeral: true);
                return;
            }
                  
            var toUpper = nameOrNumber.ToUpper().Replace('_', ' ');
            var outVal = -1;
            var nameVal = DataMap.BaseCampaign.Spells.FirstOrDefault(x => x.Name!.ToUpper() == toUpper);
            if(nameVal != null)
                outVal = DataMap.BaseCampaign.Spells.IndexOf(nameVal);
            else if(!int.TryParse(toUpper, out outVal))
            {
                await RespondAsync($"{toUpper} not found", ephemeral: true);
                return;
            }

            if(outVal >= 0 && outVal < DataMap.BaseCampaign.Spells.Count)
            {                         
                if(metamagic && casterLevel != null)
                {
                    var selb = new SelectMenuBuilder()
                        .WithCustomId($"metamagic:{outVal},{casterLevel.Value}")
                        .WithMaxValues(4)
                        .AddOption("Empowered", "emp")
                        .AddOption("Enlarged", "enl")
                        .AddOption("Extended", "ext")
                        .AddOption("Intensified", "int");

                    var cb = new ComponentBuilder()
                        .WithSelectMenu(selb);

                    await RespondAsync(components: cb.Build(), ephemeral: true);
                    return;
                }
                
                var spell = DataMap.BaseCampaign.Spells[outVal];
                
                var eb = new EmbedBuilder();
                if(casterLevel != null)
                {
                    eb.WithDescription(spell.ToCasterLevel(casterLevel.Value));
                    var cb = await BuildFormulaeComponents(spell.Formulae!.Replace(".CL", casterLevel.Value.ToString()));
                    await RespondAsync(embed: eb.Build(), components: cb.Build(), ephemeral: isHidden);
                    return;
                }
                else
                {
                    eb.WithDescription(spell.ToString());
                    await RespondAsync(embed: eb.Build(), ephemeral: isHidden);
                    return;
                }
            }
        }

        [ComponentInteraction("metamagic:*,*")]
        public async Task MetamagicSelected(int spellIndex, uint casterLevel, string[] selectedMetamagic)
        {           
            var spell = DataMap.BaseCampaign.Spells[spellIndex];

            if(selectedMetamagic.Contains("emp"))
                spell = spell.Empowered();
            if(selectedMetamagic.Contains("enl"))
                spell = spell.Enlarged();
            if(selectedMetamagic.Contains("ext"))
                spell = spell.Extended();
            if(selectedMetamagic.Contains("int"))
                spell = spell.Intensified();
     
            
            var eb = new EmbedBuilder();
            eb.WithDescription(spell.ToCasterLevel(casterLevel));
            var cb = await BuildFormulaeComponents(spell.Formulae!.Replace(".CL", casterLevel.ToString()));          
            await RespondAsync(embed: eb.Build(), components: cb.Build());         
            return;

        }

        //shortened to 'e' to eek out as many chars as i can with expressions (custom ids have a 100 char limit)
        [ComponentInteraction("e:*")]
        public async Task ButtonPressedExpression(string expr)
        {
            var character = await Characters.GetCharacter(user);
            var sb = new StringBuilder();           
            var result = await Evaluate(expr, sb, user);
            
            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var eb = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle($"{result}")
                .WithDescription(character.CharacterName != "$GLOBAL" ? character.CharacterName : "")
                .WithFooter(expr);

            if(sb.Length > 0) eb.AddField($"__Events__", $"{sb}");
            await RespondAsync(embed: eb.Build());
        }      

        public static async Task<string> Evaluate(string expr, StringBuilder sb, ulong user)
        {
            var exprs = expr.Split(';');
            var result = "";
            var character = await Characters.GetCharacter(user);
            for(int i = 0; i < exprs.Length; i++)
            {
                var node = Parser.Parse(exprs[i]);
                result += $"{node.Eval(character, sb)};";
            }
            return result.Trim(';');
        }

        [ComponentInteraction("row:*,*")]
        public async Task ButtonPressedExpr(string expr, string name)
        {
            user = Context.Interaction.User.Id;
            var character = await Characters.GetCharacter(user);
            var sb = new StringBuilder();
            var result = await Evaluate(expr, sb, user);
            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle($"{result.Trim(';')}")
                .WithDescription(character.CharacterName != "$GLOBAL" ? character.CharacterName : "")
                .WithFooter($"{expr}");

            if(sb.Length > 0) builder.AddField($"__Events__", $"{sb}");

            await RespondAsync(embed: builder.Build());
        }

        static ComponentBuilder BuildRow(List<ExprRow> exprRows)
        {
            var cb = new ComponentBuilder();

            for(int i = 0; i < exprRows.Count; i++)
            {
                for(int j = 0; j < exprRows[i].Set.Count; j++)
                {
                    if(!string.IsNullOrEmpty(exprRows[i].Set[j].Expression))
                        cb.WithButton(customId: $"row:{exprRows[i].Set[j].Expression.Replace(" ", "")},{exprRows[i].Set[j].Name.Replace(" ", "")}", label: exprRows[i].Set[j].Name, disabled: (exprRows[i].Set[j].Expression == "") ? true : false, row: i);
                }
            }
            return cb;
        }
        
        static ComponentBuilder BuildRow(ExprRow exprRow)
        {
            var cb = new ComponentBuilder();

            for(int i = 0; i < exprRow.Set.Count; i++)
            {
                if(!string.IsNullOrEmpty(exprRow.Set[i].Expression))
                    cb.WithButton(customId: $"row:{exprRow.Set[i].Expression.Replace(" ", "")},{exprRow.Set[i].Name.Replace(" ", "")}", label: exprRow.Set[i].Name, disabled: (exprRow.Set[i].Expression == "") ? true : false);
            }          
            return cb;
        }

        public async Task<ComponentBuilder> BuildFormulaeComponents(string formulae)
        {
            return await Task.Run(() =>
            {
                var cb = new ComponentBuilder();
                var split = formulae.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for(int i = 0; i < split.Length; i++)
                {
                    var f = split[i].Split('#');
                    cb.WithButton(f[0], $"e:{f[1].Replace(" ", "")}");
                }
                return cb;
            });

        }

        [AutocompleteCommand("row-name", "row")]
        public async Task AutoCompleteRowOne()
        {                  
            var input = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = new List<AutocompleteResult>();
            if(Characters.Active.ContainsKey(Context.User.Id))
                foreach(string name in Characters.Active[Context.User.Id].ExprRows.Keys)
                    results.Add(new AutocompleteResult(name, name));
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(5));                            
        }          

        [AutocompleteCommand("creature_name", "best")]
        public async Task AutoCompleteBestiary()
        {
            var input = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = DataMap.autoCompleteCreatures.Where(x => x.Name.StartsWith(input!, StringComparison.InvariantCultureIgnoreCase));
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(5));
        }

        [AutocompleteCommand("item_name", "item")]
        public async Task AutoCompleteItem()
        {
            var input = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = DataMap.autoCompleteItems.Where(x => x.Name.StartsWith(input!, StringComparison.InvariantCultureIgnoreCase));
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(5));
        }

        [AutocompleteCommand("rule_name", "rule")]
        public async Task AutoCompleteRules()
        {
            var input = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = DataMap.autoCompleteRules.Where(x => x.Name.Contains(input!, StringComparison.InvariantCultureIgnoreCase));
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(5));
        }

        [AutocompleteCommand("shape_name", "shape")]
        public async Task AutoCompleteShape()
        {
            var input = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = DataMap.autoCompleteShapes.Where(x => x.Name.StartsWith(input!, StringComparison.InvariantCultureIgnoreCase));
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(5));
        }

        [AutocompleteCommand("spell_name", "spell")]
        public async Task AutoCompleteSpell()
        {
            var input = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = DataMap.autoCompleteSpells.Where(x => x.Name.StartsWith(input!, StringComparison.InvariantCultureIgnoreCase));
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(5));
        }
        
    }

}
