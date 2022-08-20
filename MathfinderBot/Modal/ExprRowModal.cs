using Discord.Interactions;

namespace MathfinderBot
{
    public class ExprRowModal : IModal
    {
        public string Title => "New-Row()";            
               
        [InputLabel("Expression-One. `TITLE:EXPR` syntax")]
        [ModalTextInput("expr_one", minLength: 1, maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string ExprOne { get; set; }

        [RequiredInput(false)]
        [InputLabel("Expression-Two")]
        [ModalTextInput("expr_two", maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string ExprTwo { get; set; }

        [RequiredInput(false)]
        [InputLabel("Expression-Three")]
        [ModalTextInput("expr_three", maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string ExprThree { get; set; }

        [RequiredInput(false)]
        [InputLabel("Expression-Four")]
        [ModalTextInput("expr_four", maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string ExprFour { get; set; }

        [RequiredInput(false)]
        [InputLabel("Expression-Five")]
        [ModalTextInput("expr_five", maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string ExprFive { get; set; }
    }
}
