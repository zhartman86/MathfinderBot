using System.Text;
using Discord;
using Discord.Interactions;
using Gellybeans.Expressions;

namespace MathfinderBot
{
    public class Evaluate : InteractionModuleBase
    {


        private CommandHandler handler;

        private ulong user;

        public Evaluate(CommandHandler handler) => this.handler = handler;

        public override void BeforeExecute(ICommandInfo command)
        {
            base.BeforeExecute(command);
            user = Context.User.Id;
        }


        [SlashCommand("eval", "Do math with active character.")]
        public async Task EvalCommand(string expr)
        {
            Console.WriteLine(expr);
            if(!Pathfinder.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            var sb = new StringBuilder();

            var parser = Parser.Parse(expr);
            var result = parser.Eval(Pathfinder.Active[user], sb);


            var builder = new EmbedBuilder()
                .WithTitle(result.ToString())
                .WithDescription(expr + "\n\n" + sb.ToString());

            Console.WriteLine(sb.ToString());

            await RespondAsync(embed: builder.Build());


        }

        [SlashCommand("opp", "Oppsing evaluation")]
        public async Task OpposingCommand(string expr, IUser target, string against = "")
        {
            if(!Pathfinder.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
                return;
            }

            if(!Pathfinder.Active.ContainsKey(target.Id))
            {
                await RespondAsync("No active target", ephemeral: true);
                return;
            }

            expr = expr.Replace(" ", "");
            against = against.Replace(" ", "");

            //if against is left blank, assume the same check on both sides
            if(against == "") against = expr;



            var message = $"{Context.Interaction.User.Mention} has requested a {expr} check against {target.Mention}'s {against}";

            var cb = new ComponentBuilder()
                .WithButton(customId: @$"opp:{user},{expr},{target.Id},{against}", label: "Accept");

            await RespondAsync(message, components: cb.Build());
        }

        [ComponentInteraction("opp:*,*,*,*")]
        public async Task OpposingAccept(string callerId, string callerExpr, string targetId, string targetExpr)
        {          
            ulong caller = ulong.Parse(callerId);
            ulong target = ulong.Parse(targetId);



            if(user == target)
            {
                var sbCaller = new StringBuilder();
                var sbTarget = new StringBuilder();

                if(!Pathfinder.Active.ContainsKey(caller) || !Pathfinder.Active.ContainsKey(target))
                {
                    await RespondAsync("whut", ephemeral: true);
                    return;
                }

                var parser = Parser.Parse(callerExpr);
                var callerResults = parser.Eval(Pathfinder.Active[caller], sbCaller);

                parser = Parser.Parse(targetExpr);
                var targetResults = parser.Eval(Pathfinder.Active[target], sbTarget);

                var results = $"{Pathfinder.Active[caller].CharacterName}:{callerResults} {sbCaller.ToString()}\n\n" +
                    $"{Pathfinder.Active[target].CharacterName}:{targetResults} {sbTarget.ToString()}";



                var eb = new EmbedBuilder()
                    .WithDescription(results);

                await RespondAsync(embed: eb.Build());
            }
        }

        [RequireRole("DM")]
        [SlashCommand("req", "Calls for an evaluation")]
        public async Task RequestCommand(string expr)
        {

            expr = expr.Replace(" ", "");

            var message = $"{Context.Interaction.User.Mention} has requested a {expr} check";

            var cb = new ComponentBuilder()
                .WithButton(customId: $"req:{expr}", label: "Accept");

            await RespondAsync(message, components: cb.Build());
        }

        [ComponentInteraction("req:*")]
        public async Task RequestAccept(string expr)
        {
            if(!Pathfinder.Active.ContainsKey(user))
            {
                await RespondAsync("No active character", ephemeral: true);
            }

            var sb = new StringBuilder();
            var parser = Parser.Parse(expr);
            var result = parser.Eval(Pathfinder.Active[user], sb);

            var builder = new EmbedBuilder()
                .WithTitle($"{Pathfinder.Active[user].CharacterName}:{result}")
                .WithDescription(sb.ToString() + "\n\n" + expr);

            await RespondAsync(embed: builder.Build());
        }
    }
}
