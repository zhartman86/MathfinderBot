using Discord.Interactions;

namespace MathfinderBot
{
    public class CampaignEntryModal : IModal
    {  
        public string Title { get; set; }

        [ModalTextInput("entry", Discord.TextInputStyle.Paragraph, "New entry...")]
        public string Entry { get; set; }    
    }
}
