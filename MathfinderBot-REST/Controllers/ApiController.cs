using Discord;
using Discord.WebSocket;
using Gellybeans.Expressions;
using Gellybeans.Pathfinder;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Text;

namespace MathfinderBot
{
    
    [ApiController]
    [Route("[controller]")]
    public class ApiController : ControllerBase
    {
        DiscordSocketClient dClient;
        
        [HttpGet("character/{id:regex(^[[0-9]]{{1,20}}$)}/{name}")]
        public async Task<JsonResult> GetCharacter(string id, string name) 
        { 
            if(ulong.TryParse(id, out var discID)) 
            {
                var results = await Program.GetStatBlocks().FindAsync(x => x.Owner == discID && x.CharacterName == name);
                var stats = results.ToList();
                if(stats.Count > 0)
                    return new JsonResult(stats[0]);
            }
            return new JsonResult(new Status 
            { 
                Code = 404, 
                Name = "Not Found", 
                Body = "The requested resource was not found." 
            });
            
        }

        [HttpPost("channel/{id:regex(^[[0-9]]{{1,20}}$)}")]
        public async Task<JsonResult> HandleChannelAction(string id, [FromBody] WebAction action)
        {            
            if(action != null && ulong.TryParse(id, out var chanID))
            {
                var guild = Program.client.GetGuild(265650672794468352);
                var chan = guild.GetTextChannel(chanID);
                if(chan != null)
                {                    
                    if(action.Action == "expr")
                    {
                        var sb = new StringBuilder();
                        var description = "";
                        string result = "";

                        var exprs = action.Target.Split(';', StringSplitOptions.RemoveEmptyEntries);

                        for(int i = 0; i < exprs.Length; i++)
                        {
                            if(i > 0 && i < exprs.Length)
                                sb.AppendLine("-:-");
                            var node = Parser.Parse(exprs[i]);
                            result += $"{node.Eval(null!, sb)};";
                        }
                        result = result.Trim(';');

                        var ab = new EmbedAuthorBuilder()
                            .WithName(action.Source);

                        var title = result.ToString();

                        var builder = new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithAuthor(ab)
                            .WithTitle(title)
                            .WithDescription(description)
                            .WithFooter(action.Target);
                        builder.AddField($"__Events__", $"{sb}", inline: true);


                        await chan.SendMessageAsync(embed: builder.Build());
                    }
                    return new JsonResult(new Status
                    {
                        Code = 200,
                        Name = "Accepted",
                        Body = "Your request was accepted."
                    });
                }
                
            }
            return new JsonResult(new Status
            {
                Code = 404,
                Name = "Not Found",
                Body = "The requested resource was not found."
            });
        }
    }
}
