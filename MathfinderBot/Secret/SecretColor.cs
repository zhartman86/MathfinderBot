using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathfinderBot
{
    public static class SecretColor
    {
        public static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            { "Flush",      new Color(196, 33, 66)      },
            { "Well",       new Color(24, 76, 196)      },
            { "PaleBlue",   new Color(95, 154, 217)     },
        };

    }
}
