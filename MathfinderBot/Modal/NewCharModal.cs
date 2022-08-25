using Discord.Interactions;

namespace MathfinderBot
{
    public class NewChar : IModal
    {

        public string Title => "New-Character()";

        [RequiredInput(false)]
        [ModalTextInput("score", minLength:10, maxLength:17, initValue: "10,10,10,10,10,10")]
        public string Scores { get; set; }

        [RequiredInput(false)]
        [ModalTextInput("saves", minLength: 4, maxLength:9, placeholder: "For 5e, use `STR,DEX` syntax. For 3.5/PF/SF, use `0,0,0`" )]
        public string Saves { get; set; }

        [RequiredInput(false)]
        [ModalTextInput("")]
        public string OtherP { get; set; }

    }
}
