using System;
using System.Collections.Generic;
using System.Text;

namespace Airports.Logic.Models
{
    public class City
    {
        public int CountryId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string TimeZoneName { get; set; }
        public Country Country { get; set; }
    }
}
