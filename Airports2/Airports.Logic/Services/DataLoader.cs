using Airports.Logic.Models;
using Airports.Logic.Services.Interfaces;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Airports.Logic.Services
{
    public class DataLoader : IDataLoader
    {
        readonly Logger logger;
        AirportContext context;
        IFileManager fileManager;

        static IDictionary<UniqueCity, City> cities; // it is necessary, because there are more cities in different countries, which have the same name

        public DataLoader()
        {
            logger = LogManager.GetCurrentClassLogger();
            cities = new Dictionary<UniqueCity, City>();
            fileManager = new FileManager();
            ClearContext();
        }

        public bool AreDataAvailable
        {
            get
            {
                return fileManager.GetDataAvailability();
            }
        }

        public AirportContext LoadData()
        {
            ClearContext();
            var pattern = "^[0-9]{1,4},(\".*\",){3}(\"[A-Za-z]+\",){2}([-0-9]{1,4}(\\.[0-9]{0,})?,){2}";

            try
            {
                using (var reader = fileManager.GetStreamForRead(@"airports.dat"))
                {
                    string line;
                    int count = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!Regex.Match(line, pattern).Success)
                        {
                            logger.Info($"The next row (\"{line}\") is not match with the pattern.");
                            count++;
                            continue;
                        }

                        CreateAirportModel(line);
                    }

                    logger.Info($"There was {count} elements, wich not matched with the pattern.");
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                logger.Error(ex);
            }

            LoadConstantData();
            SerializeObjects();

            return context;
        }

        public AirportContext ReadImportedFiles()
        {
            if (AreDataAvailable)
            {
                ClearContext();

                context.Airports = fileManager.Deserialize<Airport>("airports.json");
                context.Cities = fileManager.Deserialize<City>("cities.json");
                context.Countries = fileManager.Deserialize<Country>("countries.json");
                context.Locations = fileManager.Deserialize<Location>("locations.json");
                LoadConstantData();
                return context;
            }
            return new AirportContext();
        }

        private void ClearContext()
        {
            context = new AirportContext
            {
                Airports = new List<Airport>(),
                Airlines = new List<Airline>(),
                Cities = new List<City>(),
                Countries = new List<Country>(),
                Flights = new List<Flight>(),
                Locations = new List<Location>(),
                Segments = new List<Segment>(),
                TimeZones = new List<AirportTimeZoneInfo>()
            };
        }

        private void LoadConstantData()
        {
            LoadCSVs();
            context.TimeZones = fileManager.DeserializeTimeZones();
            LoadTimeZoneNames();
            FindISOCodes();
        }

        private void LoadCSVs()
        {
            context.Airlines = (ICollection<Airline>)CsvHelper.Parse<Airline>("airlines.dat");
            context.Segments = (ICollection<Segment>)CsvHelper.Parse<Segment>("segments.dat");
            context.Flights = (ICollection<Flight>)CsvHelper.Parse<Flight>("flights.dat");
        }

        private void CreateAirportModel(string line)
        {
            var data = line.AirportSplit(',');

            var country = CreateCountry(data);
            var city = CreateCity(data, country);
            var location = CreateLocation(data);

            var airport = new Airport
            {
                Id = int.Parse(data[0]),
                Name = data[1].Trim('"'),
                FullName = GenerateFullName(data[1].Trim('"')),
                CityId = city.Id,
                City = city,
                CountryId = country.Id,
                Country = country,
                Location = location,
                IATACode = data[4].Trim('"'),
                ICAOCode = data[5].Trim('"')
            };
            context.Airports.Add(airport);
        }

        private string GenerateFullName(string name)
        {
            int airtportWordLength = 7;
            if (name.Length < airtportWordLength)
            {
                return name + " Airport";
            }
            else if (name.Substring(name.Length - airtportWordLength).ToLower() == "airport")
            {
                return name;
            }
            else
            {
                return name + " Airport";
            }
        }

        private Country CreateCountry(string[] data)
        {
            var country = context.Countries.SingleOrDefault(c => c.Name == data[3].Trim('"'));
            if (country == null)
            {
                var newCountry = new Country
                {
                    Id = context.Countries.Count > 0 ? context.Countries.Max(c => c.Id) + 1 : 1,
                    Name = data[3].Trim('"')
                };
                context.Countries.Add(newCountry);
                country = newCountry;
            }

            return country;
        }

        private Location CreateLocation(string[] data)
        {
            var location = context.Locations.SingleOrDefault(l => l.Longitude.ToString() == data[6]
                                              && l.Latitude.ToString() == data[7]
                                              && l.Altitude.ToString() == data[8]);
            if (location == null)
            {
                var newLocation = new Location
                {
                    Longitude = decimal.Parse(data[6], CultureInfo.InvariantCulture),
                    Latitude = decimal.Parse(data[7], CultureInfo.InvariantCulture),
                    Altitude = decimal.Parse(data[8], CultureInfo.InvariantCulture)
                };

                context.Locations.Add(newLocation);
                location = newLocation;
            }

            return location;
        }

        private City CreateCity(string[] data, Country country)
        {
            var city = cities.SingleOrDefault(c => c.Key == new UniqueCity { CityName = data[2].Trim('"'), CountryName = country.Name }).Value;
            if (city == null)
            {
                var newCity = new City
                {
                    Id = cities.Count > 0 ? cities.Values.Max(c => c.Id) + 1 : 1,
                    Name = data[2].Trim('"'),
                    CountryId = country.Id,
                    Country = country
                };

                cities.Add(new UniqueCity { CityName = newCity.Name, CountryName = country.Name }, newCity);
                context.Cities.Add(newCity);
                city = newCity;
            }

            return city;
        }

        private void LoadTimeZoneNames()
        {
            foreach (var zone in context.TimeZones)
            {
                var airport = context.Airports.SingleOrDefault(a => a.Id == zone.AirportId);
                if (airport != null)
                {
                    var city = context.Cities.Single(c => c.Id == airport.CityId);
                    airport.TimeZoneName = zone.TimeZoneInfoId;
                    city.TimeZoneName = zone.TimeZoneInfoId;
                }
            }
        }

        private void FindISOCodes()
        {
            foreach (var airport in context.Airports)
            {
                var culture = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                                         .FirstOrDefault(c => c.EnglishName.Contains(airport.Country.Name));

                if (culture != null)
                {
                    try
                    {
                        var country = context.Countries.Single(a => a.Id == airport.CountryId);
                        var regInfo = new RegionInfo(culture.Name);
                        country.TwoLetterISOCode = regInfo.TwoLetterISORegionName;
                        country.ThreeLetterISOCode = regInfo.ThreeLetterISORegionName;
                    }
                    catch (Exception)
                    {
                        logger.Info($"Culture ({culture.EnglishName}) is not correct.");
                    }
                }
            }
        }

        private void SerializeObjects()
        {
            fileManager.WriteObjectToFile(@"airports.json", context.Airports);
            fileManager.WriteObjectToFile(@"cities.json", cities.Values);
            fileManager.WriteObjectToFile(@"countries.json", context.Countries);
            fileManager.WriteObjectToFile(@"locations.json", context.Locations);
        }
    }

    class UniqueCity
    {
        public string CityName { get; set; }

        public string CountryName { get; set; }
    }
}
