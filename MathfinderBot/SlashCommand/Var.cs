using System.Text;
using System.Text.RegularExpressions;
using Discord.Interactions;
using Gellybeans.Pathfinder;
using Gellybeans.Expressions;
using MongoDB.Driver;
using Discord;

namespace MathfinderBot
{
    public class Variable : InteractionModuleBase
    {
        public enum VarAction
        {
            [ChoiceDisplay("Set-Expression")]
            SetExpr,

            [ChoiceDisplay("Set-Row")]
            SetRow,
            
            [ChoiceDisplay("List-Stats")]
            ListStats,

            [ChoiceDisplay("List-Expressions")]
            ListExpr,

            [ChoiceDisplay("List-Rows")]
            ListRows,

            [ChoiceDisplay("Remove-Variable")]
            Remove
        }


        static Regex ValidVar = new Regex(@"^[A-Z_]{1,17}$");
        static Regex validExpr = new Regex(@"^[0-9a-zA-Z_:+*/%=!<>() ]{1,300}$");

        CommandHandler handler;
        ulong user;
        IMongoCollection<StatBlock> collection;

        static Dictionary<ulong, string> lastInputs;

        static Variable()
        {
            lastInputs = new Dictionary<ulong, string>();
        }


        public Variable(CommandHandler handler) => this.handler = handler;

        public async override void BeforeExecute(ICommandInfo command)
        {
            user = Context.Interaction.User.Id;

            if(!Pathfinder.Active.ContainsKey(user) || Pathfinder.Active[user] == null)
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }
        }


        [SlashCommand("var", "Create, modify, list, remove.")]
        public async Task Var(VarAction action, string varName = "", string value = "")
        {
            collection = Program.database.GetCollection<StatBlock>("statblocks");

            if(action == VarAction.ListStats)
            {
                var builder = new StringBuilder();

                foreach(var stat in Pathfinder.Active[user].Stats)
                {
                    builder.AppendLine(stat.Key + ":" + ((int)stat.Value).ToString());
                }             
                                          
                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
                await RespondWithFileAsync(stream, $"Stats.{Pathfinder.Active[user].CharacterName}.txt", ephemeral: true);                    
            }

            if(action == VarAction.ListExpr)
            {
                var builder = new StringBuilder();

                foreach(var expr in Pathfinder.Active[user].Expressions)
                {
                    builder.AppendLine(expr.Key + ":" + expr.Value.ToString());
                }                             

                using var stream = new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
                await RespondWithFileAsync(stream, $"Expressions.{Pathfinder.Active[user].CharacterName}.txt", ephemeral: true);
            }

            
            if(action == VarAction.ListRows)
            {
                var eb = new EmbedBuilder();
                if(varName != "")
                {
                    var toUpper = $"${varName.ToUpper()}";
                    if(Pathfinder.Active[user].ExprRows.ContainsKey(toUpper))
                    {
                        eb = new EmbedBuilder()
                            .WithColor(Color.DarkGreen)
                            .WithTitle($"List-Row({toUpper})")
                            .WithDescription(Pathfinder.Active[user].ExprRows[toUpper].ToString());
                        
                        await RespondAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                }

                var sb = new StringBuilder();
                foreach(var attack in Pathfinder.Active[user].ExprRows.Keys)
                {
                    sb.AppendLine(attack);
                }

                eb = new EmbedBuilder()
                        .WithColor(Color.DarkGreen)
                        .WithTitle($"List-Attacks()")
                        .WithDescription($"```{sb.ToString()}```");

                await RespondAsync(embed: eb.Build(), ephemeral: true);
            }
            
            var varToUpper = varName.ToUpper();
            if(!ValidVar.IsMatch(varToUpper))
            {
                await RespondAsync($"Invalid variable `{varToUpper}`. A-Z and underscores only. Values will be automatically capitalized.", ephemeral: true);
                return;
            }
        
            
            if(action == VarAction.Remove)
            {
                if(Pathfinder.Active[user].Stats.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Stats.Remove(varToUpper);

                    var update = Builders<StatBlock>.Update.Set(x => x.Stats, Pathfinder.Active[user].Stats);
                    await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                    await RespondAsync($"`{varToUpper}` removed from stats.", ephemeral: true);
                    return;
                }
                else if(Pathfinder.Active[user].Expressions.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].Expressions.Remove(varToUpper);

                    var update = Builders<StatBlock>.Update.Set(x => x.Expressions, Pathfinder.Active[user].Expressions);
                    await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                    await RespondAsync($"`{varToUpper}` removed from expressions.", ephemeral: true);
                    return;
                }
                else if(Pathfinder.Active[user].ExprRows.ContainsKey(varToUpper))
                {
                    Pathfinder.Active[user].ExprRows.Remove(varToUpper);
                    
                    var update = Builders<StatBlock>.Update.Set(x => x.ExprRows, Pathfinder.Active[user].ExprRows);
                    await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                    await RespondAsync($"`{varToUpper}` removed from rows.", ephemeral: true);
                    return;

                }


                await RespondAsync($"No variable `{varToUpper}` found.", ephemeral: true);
                return;
            }


            if(action == VarAction.SetExpr && !varToUpper.Contains('$'))
            {
                if(Pathfinder.Active[user].Stats.ContainsKey(varToUpper))
                {
                    await RespondAsync($"`{varToUpper}` already exists as a stat.", ephemeral: true);
                    return;
                }

                Pathfinder.Active[user].Expressions[varToUpper] = value;

                var update = Builders<StatBlock>.Update.Set(x => x.Expressions[varToUpper], Pathfinder.Active[user].Expressions[varToUpper]);                                     
                await collection.UpdateOneAsync(x => x.Id == Pathfinder.Active[user].Id, update);
                await RespondAsync($"Updated expression:`{varToUpper}`", ephemeral: true);
            }                                            
        }


        [SlashCommand("row", "Get a row or rows")]
        public async Task GetRowCommand(string rowOne, string rowTwo = "", string rowThree = "", string rowFour = "", string rowFive = "")
        {
            var rowStrings = new string[5] { rowOne, rowTwo, rowThree, rowFour, rowFive };
            var rows = new List<ActionRowBuilder>();

            for(int i = 0; i < rowStrings.Length; i++)
            {         
                if(rowStrings[i] != "")
                {
                    var toUpper = rowStrings[i];

                    if(!Pathfinder.Active[user].ExprRows.ContainsKey(toUpper))
                    {
                        await RespondAsync($"`{toUpper}` not found.", ephemeral: true);
                        return;
                    }

                    rows.Add(BuildRow(Pathfinder.Active[user].ExprRows[toUpper], i*i));
                }
            }

            var builder = new ComponentBuilder()
                .WithRows(rows);
                
            await RespondAsync(components: builder.Build(), ephemeral: true);      
        }

        [ComponentInteraction("row:*,*,*")]
        public async Task ButtonPressed(ulong user, string expr, int id)
        {
            Console.WriteLine("TEST");
            
            var sb = new StringBuilder();
            var result = Parser.Parse(expr).Eval(Pathfinder.Active[user], sb);

            var ab = new EmbedAuthorBuilder()
                .WithName(Context.Interaction.User.Username)
                .WithIconUrl(Context.Interaction.User.GetAvatarUrl());

            var builder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor(ab)
                .WithTitle($"{result}")
                .WithDescription($"{Pathfinder.Active[user].CharacterName}")
                .WithFooter($"{expr}");

            if(sb.Length > 0) builder.AddField($"__Events__", $"{sb}");

            Console.WriteLine(sb.ToString());

            await RespondAsync(embed: builder.Build());
        }

        [SlashCommand("grid", "Call a saved set of rows")]
        public async Task GridGetCommand(string gridName)
        {
            var toUpper = $"#{gridName.ToUpper()}";
            if(!Pathfinder.Active[user].Grids.ContainsKey(toUpper))
            {
                await RespondAsync($"{toUpper} not found.", ephemeral: true);
                return;
            }

            var grid = Pathfinder.Active[user].Grids[toUpper];
            var rows = new List<ActionRowBuilder>();
            //only 5 rows are allowed by discord
            for(int i = 0; i < 5; i++)
            {
                if(!Pathfinder.Active[user].ExprRows.ContainsKey(grid[i].RowName))
                {
                    await RespondAsync($"{grid[i].RowName} not found", ephemeral: true);
                    return;
                } 
                rows.Add(BuildRow(grid[i], i*i));

            }
            var builder = new ComponentBuilder()
                .WithRows(rows);
            
            await RespondAsync(components: builder.Build(), ephemeral: true);
        }



        [SlashCommand("row-set", "Set or modify attacks")]
        public async Task RowCommand(string rowName)
        {
            var toUpper = rowName.ToUpper();
            if(!ValidVar.IsMatch(toUpper))
            {
                await RespondAsync($"Invalid variable `{toUpper}`. A-Z and underscores only. Values will be automatically capitalized.", ephemeral: true);
                return;
            }

            lastInputs[user] = toUpper;
            
            await RespondWithModalAsync<ExprRowModal>("new_row");
        }

       
        
        [ModalInteraction("new_row")]
        public async Task NewRow(ExprRowModal modal)
        {
            string[] exprs = new string[5] { modal.ExprOne, modal.ExprTwo, modal.ExprThree, modal.ExprFour, modal.ExprFive };
            string[] rowExprs = new string[5];
            string[] rowExprNames = new string[5];

            for(int i = 0; i < exprs.Length; i++)
            {
                if(validExpr.IsMatch(exprs[i]))
                {
                    var split = exprs[i].Split(':');
                    if(split.Length == 2)
                    {
                        rowExprNames[i] = split[0];
                        rowExprs[i] = split[1];
                    }
                    else if(split.Length == 1)
                    {
                        rowExprNames[i] = split[0];
                        rowExprs[i] = split[0];
                    }                
                    else
                    {
                        await RespondAsync($"Invalid Input @ Expression {i + 1}", ephemeral: true);
                        return;
                    }                      
                }
                else if(string.IsNullOrEmpty(exprs[i]))
                {
                    rowExprNames[i] = $"Expr{i + 1}";
                    rowExprs[i]     = "EMPTY";
                }
                else
                {
                    rowExprNames[i] = $"ERROR{i}";
                    rowExprs[i]     = "EMPTY";
                }
            }
            var row = new ExprRow()
            {
                RowName = $"${lastInputs[user]}",

                Set = new List<Expr>()
                {
                    new Expr(rowExprNames[0], rowExprs[0]),
                    new Expr(rowExprNames[1], rowExprs[1]),
                    new Expr(rowExprNames[2], rowExprs[2]),
                    new Expr(rowExprNames[3], rowExprs[3]),
                    new Expr(rowExprNames[4], rowExprs[4]),
                }


            };

            Pathfinder.Active[user].ExprRows[row.RowName] = row;
            collection = Program.database.GetCollection<StatBlock>("statblocks");
            var update = Builders<StatBlock>.Update.Set(x => x.ExprRows[row.RowName], row);
            await collection.FindOneAndUpdateAsync(x => x.Id == Pathfinder.Active[user].Id, update);

            var eb = new EmbedBuilder()
                .WithColor(Color.Gold)
                .WithTitle($"VAR-ROW({row.RowName})");

            eb.AddField(name: row.Set[0].Name,      value: row.Set[0].Expression,     inline: true);
            eb.AddField(name: row.Set[1].Name,      value: row.Set[1].Expression,     inline: true);
            eb.AddField(name: row.Set[2].Name,      value: row.Set[2].Expression,     inline: true);
            eb.AddField(name: row.Set[3].Name,      value: row.Set[3].Expression,     inline: true);
            eb.AddField(name: row.Set[4].Name,      value: row.Set[4].Expression,     inline: true);


            await RespondAsync(embed: eb.Build());
        }
    
        ActionRowBuilder BuildRow(ExprRow exprRow, int id)
        {
            var ar = new ActionRowBuilder()
                .WithButton(customId: $"row:{user},{exprRow.Set[0].Expression.Replace(" ", "")},{id}1", label: exprRow.Set[0].Name, disabled: (exprRow.Set[0].Expression == "EMPTY") ? true : false)
                .WithButton(customId: $"row:{user},{exprRow.Set[1].Expression.Replace(" ", "")},{id}1", label: exprRow.Set[0].Name, disabled: (exprRow.Set[1].Expression == "EMPTY") ? true : false)
                .WithButton(customId: $"row:{user},{exprRow.Set[2].Expression.Replace(" ", "")},{id}1", label: exprRow.Set[0].Name, disabled: (exprRow.Set[2].Expression == "EMPTY") ? true : false)
                .WithButton(customId: $"row:{user},{exprRow.Set[3].Expression.Replace(" ", "")},{id}1", label: exprRow.Set[0].Name, disabled: (exprRow.Set[3].Expression == "EMPTY") ? true : false)
                .WithButton(customId: $"row:{user},{exprRow.Set[4].Expression.Replace(" ", "")},{id}1", label: exprRow.Set[0].Name, disabled: (exprRow.Set[4].Expression == "EMPTY") ? true : false);


            foreach(var item in ar.Components)
            {
                Console.WriteLine(item.CustomId);
            }

            return ar;
        }
    }
}
