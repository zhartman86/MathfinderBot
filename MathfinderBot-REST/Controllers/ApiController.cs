using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace MathfinderBot
{
    
    [ApiController]
    [Route("[controller]")]
    public class ApiController : ControllerBase
    {
        [HttpGet("character/{id:regex(^[[0-9]]{{1,20}}$)}/{name}")]
        public JsonResult GetCharacter(string id, string name) 
        { 
            if(ulong.TryParse(id, out var discID)) 
            {
                var results = Program.GetStatBlocks().Find(x => x.Owner == discID && x.CharacterName == name);
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
        

    }
}
