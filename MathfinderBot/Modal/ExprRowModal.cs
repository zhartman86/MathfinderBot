using Discord.Interactions;
using Gellybeans.Pathfinder;

namespace MathfinderBot
{
    public class ExprRowModal : IModal
    {    
        public string Title => "New-Row()";

        [InputLabel("Expressions. `LABEL:EXPR`. Up to 5 lines.")]
        [ModalTextInput("expressions", minLength: 1, maxLength: 1000, style: Discord.TextInputStyle.Paragraph, placeholder: "LABEL:EXPR\r\nEXATTACK:ATK_S+2\r\nEXDAMAGE:DMG_S+2\r\nEXCRIT:(DMG_S*2) + 4\r\nEXSKILL:UMD\r\nEXEXPR:1d20+9999")]
        public string Expressions { get; set; }        
    }
}
