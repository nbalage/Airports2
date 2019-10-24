using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Airports2.Models
{
    class Flight
    {
        public int Id { get; set; }

        [TimeZone]
        public TimeSpan ArrivalTime { get; set; }

        [TimeZone]
        public TimeSpan DepartureTime { get; set; }

        public string Number { get; set; }
        public int SegmentId { get; set; }
        public Segment Segment { get; set; }
    }
}
