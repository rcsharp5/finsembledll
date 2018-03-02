using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ChartIQ.Finsemble
{
    public class LinkerChannel
    {
        public string name { private set; get; }
        public Color color { private set; get; }
        public Color border { private set; get; }

        public LinkerChannel(string name, string color, string border)
        {
            this.name = name;
            this.color = (Color)ColorConverter.ConvertFromString(color);
            this.border = (Color)ColorConverter.ConvertFromString(border);
        }
    }
}
