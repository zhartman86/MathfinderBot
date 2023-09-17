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


        [InputLabel("Syntax is LABEL#EXPR. One per line, up to 25")]
        [ModalTextInput("expressions", minLength: 1, maxLength: 750, style: Discord.TextInputStyle.Paragraph, placeholder: "LABEL#EXPR\r\nATTACK#ATK_STR+2\r\nDAMAGE#DMG_STR+2\r\nCRIT#((DMG_STR+2)*2)\r\nUMD\r\nEXEXPR#1d20+9999")]
        public string Expressions { get; set; }        
    }
}
