using System;
using System.Collections.Generic;
using System.Text;

namespace Airports.Logic.Services
{
    public static class StringExtensions
    {
        public static string[] AirportSplit(this string input, char separator)
        {
            List<string> tokens = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool closedString = true;
            int index = 0;

            foreach (char c in input)
            {
                if (c == '"')
                {
                    closedString = !closedString;
                }

                if (c == separator && closedString)
                {
                    tokens.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }

                if (index == input.Length - 1)
                {
                    tokens.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                index++;
            }

            return tokens.ToArray();
        }
    }
}
