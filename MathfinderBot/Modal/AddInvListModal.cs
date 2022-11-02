using Discord.Interactions;

namespace MathfinderBot
{
    public class AddInvListModal : IModal
    {
        public string Title => "Add-Inventory-List";

        [ModalTextInput("inv_list", Discord.TextInputStyle.Paragraph, placeholder: "NAME:WEIGHT:VALUE (One item per line)")]
        public string List { get; set; }
    }
}
