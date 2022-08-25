using Discord.Interactions;

namespace MathfinderBot
{
    public class CraftingModal : IModal
    {

        public string Title => "New-Craft()";


        [ModalTextInput("item_name", maxLength:50)]
        public string ItemName { get; set; }

        [ModalTextInput("difficulty", maxLength:3)]
        public int Difficulty { get; set; }

        [ModalTextInput("silver_price", maxLength:10)]
        public int SilverPrice { get; set; }
    
    }
}
