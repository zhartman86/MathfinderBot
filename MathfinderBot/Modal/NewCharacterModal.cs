using Discord.Interactions;

namespace MathfinderBot
{
    public class NewCharacterModal : IModal
    {
        public string Title => "New Character";

        [InputLabel("Character Name")]
        [ModalTextInput("char_name", placeholder: "name me", minLength: 1, maxLength: 50)]
        public string CharacterName { get; set; }
    }
}
