using Discord.Interactions;

namespace MathfinderBot
{
    public class AddInvModal : IModal
    {
        public string Title => "Add-Inv()";

        [ModalTextInput("inv_list", Discord.TextInputStyle.Paragraph, placeholder: "NAME:WEIGHT:VALUE (One item per line)")]
        public string List { get; set; }
    }
}
