using Discord.Interactions;
using Gellybeans.Pathfinder;

namespace MathfinderBot
{
    public class ExprRowModal : IModal
    {    
        public string Title => "New-Row()";

        [InputLabel("Give it a name")]
        [ModalTextInput("expr_row_name", minLength: 2, maxLength: 20)]
        public string Name { get; set; }


        [InputLabel("Expressions. `LABEL:EXPR` One per line, up to 5")]
        [ModalTextInput("expressions", minLength: 1, maxLength: 750, style: Discord.TextInputStyle.Paragraph, placeholder: "LABEL:EXPR\r\nEXATTACK:ATK_S+2\r\nEXDAMAGE:DMG_S+2\r\nEXCRIT:(DMG_S*2) + 4\r\nEXSKILL:UMD\r\nEXEXPR:1d20+9999")]
        public string Expressions { get; set; }        
    }
}
