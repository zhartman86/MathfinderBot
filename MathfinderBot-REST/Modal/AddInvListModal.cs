using Discord.Interactions;

namespace MathfinderBot
{
    public class AddInvListModal : IModal
    {
        public string Title => "Add List";

        [ModalTextInput("inv_list",  Discord.TextInputStyle.Paragraph, placeholder: "NAME:QTY:VALUE:WEIGHT:NOTE (Only NAME required)\nSword:1:15:4\nDiamond:4:500\nGeneric Fighter's Item")]
        public string List { get; set; }
    }
}
