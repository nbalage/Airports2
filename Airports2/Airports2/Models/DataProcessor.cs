using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Airports2.Models
{
    class DataProcessor
    {
        readonly AirportContext context;

        public DataProcessor(AirportContext context)
        {
            this.context = context;
        }

        public string CountCountries()
        {
            var orderedAirports = context.Airports.OrderBy(a => a.Country.Name);
            var groupedAirports = orderedAirports.GroupBy(o => o.Country.Name);

            StringBuilder sb = new StringBuilder();
            foreach (var item in groupedAirports)
            {
                sb.AppendLine($"{item.Key}: {item.Count()}");
            }

            return sb.ToString();
        }

        public string MaxAirportNumberInOneCity()
        {
            var groupedAirports = context.Airports.GroupBy(o => o.City.Name);
            var maxAirportInCities = groupedAirports.OrderByDescending(g => g.Count()).First();
            return $"The city, wich has the most airports is {maxAirportInCities.Key}, with {maxAirportInCities.Count()}";
        }
    }
}
