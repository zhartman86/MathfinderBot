using Discord.Interactions;

namespace MathfinderBot
{
    public class GridModal : IModal
    {
        public string Title => "New-Row()";

        [InputLabel("Row-One")]
        [ModalTextInput("row_one", minLength: 1, maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string RowOne { get; set; }

        [RequiredInput(false)]
        [InputLabel("Row-Two")]
        [ModalTextInput("row_two", maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string RowTwo { get; set; }

        [RequiredInput(false)]
        [InputLabel("Row-Three")]
        [ModalTextInput("row_three", maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string RowThree { get; set; }

        [RequiredInput(false)]
        [InputLabel("Row-Four")]
        [ModalTextInput("row_four", maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string RowFour { get; set; }

        [RequiredInput(false)]
        [InputLabel("Row-Five")]
        [ModalTextInput("row_five", maxLength: 40, style: Discord.TextInputStyle.Short)]
        public string RowFive { get; set; }
    }
}
