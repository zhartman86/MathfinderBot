using Discord.Interactions;
using System.ComponentModel;

namespace MathfinderBot
{
    public class GridModal : IModal
    {
        public string Title => "New-Grid()";

        [InputLabel("Give it a name")]
        [ModalTextInput("grid_name", minLength: 2, maxLength: 20)]
        public string Name { get; set; }

        [InputLabel("List of rows. One per line, up to 5")]
        [ModalTextInput("row_one", minLength: 1, maxLength: 120, style: Discord.TextInputStyle.Paragraph, placeholder: "ROW1\rROW2\rROW3\rROW4\rROW5")]
        public string Rows { get; set; }
    }
}
