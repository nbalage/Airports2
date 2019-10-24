using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Airports.Logic.Models
{
    public class Airline
    {
        public string CallSign { get; set; }
        [Column("iata")]
        public string IATACode { get; set; }
        [Column("icao")]
        public string ICAOCode { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
