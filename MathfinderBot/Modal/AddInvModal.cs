using Discord.Interactions;

namespace MathfinderBot
{
    public class AddInvModal : IModal
    {
        public string Title => "Add-Item";

        [ModalTextInput("name", maxLength:40)]
        public string Name { get; set; }

        [ModalTextInput("quantity", initValue: "1", maxLength: 10)]
        public string Quantity { get; set; }

        [ModalTextInput("weight", maxLength: 10)]
        public string Weight { get; set; }

        [ModalTextInput("value", maxLength: 10)]
        public string Value { get; set; }       

        [ModalTextInput("note", Discord.TextInputStyle.Paragraph)]
        public string Note { get; set; }
    }
}
