using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartIQ.Finsemble
{
    internal class InternalHelper
    {
        public static string TitleCase(string str)
        {
            var split = str.Split(' ');
            for (var i = 0; i < split.Length; i++)
            {
                //split[i] = split[i].ToLower();
                if (!String.IsNullOrEmpty(split[i]))
                {
                    split[i] = char.ToUpper(split[i][0]) + split[i].Substring(1);
                }
            }
            return String.Join("", split);
        }
    }
}
