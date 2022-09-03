using Discord.Interactions;

namespace MathfinderBot
{
    public class CampaignNameModal: IModal
    {
        public string Title => "Campaign()";

        [ModalTextInput("campaign_name", maxLength:50)]
        public string CampaignName { get; set; }
    }
}
