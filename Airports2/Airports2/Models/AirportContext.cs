using System;
using System.Collections.Generic;
using System.Text;

namespace Airports2.Models
{
    class AirportContext
    {
        public ICollection<Airport> Airports { get; set; }
        public ICollection<City> Cities { get; set; }
        public ICollection<Country> Countries { get; set; }
        public ICollection<Location> Locations { get; set; }
        public ICollection<Airline> Airlines { get; set; }
        public ICollection<Segment> Segments { get; set; }
        public ICollection<Flight> Flights { get; set; }
    }
}
