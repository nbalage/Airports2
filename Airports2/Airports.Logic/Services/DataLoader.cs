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
        const string InputFolderPath = @"..\\..\\..\\..\\Data\";
        const string OutputFolderPath = @"\Output\";
        readonly Logger logger;
        AirportContext context;

        static IDictionary<UniqueCity, City> cities; // it is necessary, because there are more cities in different countries, which have the same name
        static string[] FileNames =
        {
            "airports.json",
            "cities.json",
            "countries.json",
            "locations.json"
        };

        public bool AreDataAvailable()
        {
            if (!Directory.Exists(InputFolderPath + OutputFolderPath))
            {
                return false;
            }

            bool filesExsist = true;

            foreach (var path in FileNames)
            {
                if (!File.Exists(InputFolderPath + OutputFolderPath + path))
                {
                    filesExsist = false;
                }
}

            return filesExsist;
        }

        public DataLoader()
        {
            logger = LogManager.GetCurrentClassLogger();
            cities = new Dictionary<UniqueCity, City>();
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

        public AirportContext LoadData()
        {
            var pattern = "^[0-9]{1,4},(\".*\",){3}(\"[A-Za-z]+\",){2}([-0-9]{1,4}(\\.[0-9]{0,})?,){2}";

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            using (var reader = new StreamReader(new FileStream(InputFolderPath + @"airports.dat", FileMode.Open)))
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

            LoadConstantData();
            SerializeObjects();

            return context;
        }

        public AirportContext ReadImportedFiles()
        {
            context.Airports = JsonConvert.DeserializeObject<List<Airport>>(File.ReadAllText(InputFolderPath + OutputFolderPath + "airports.json"));
            context.Cities = JsonConvert.DeserializeObject<List<City>>(File.ReadAllText(InputFolderPath + OutputFolderPath + "cities.json"));
            context.Countries = JsonConvert.DeserializeObject<List<Country>>(File.ReadAllText(InputFolderPath + OutputFolderPath + "countries.json"));
            context.Locations = JsonConvert.DeserializeObject<List<Location>>(File.ReadAllText(InputFolderPath + OutputFolderPath + "locations.json"));
            LoadConstantData();
            return context;
        }

        private void LoadConstantData()
        {
            LoadCSVs();
            context.TimeZones = DeserializeTimeZones();
            LoadTimeZoneNames();
            FindISOCodes();
        }

        private void LoadCSVs()
        {
            context.Airlines = (ICollection<Airline>)CsvHelper.Parse<Airline>(InputFolderPath + "airlines.dat");
            context.Segments = (ICollection<Segment>)CsvHelper.Parse<Segment>(InputFolderPath + "segments.dat");
            context.Flights = (ICollection<Flight>)CsvHelper.Parse<Flight>(InputFolderPath + "flights.dat");
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

        public ICollection<AirportTimeZoneInfo> DeserializeTimeZones()
        {
            return JsonConvert.DeserializeObject<List<AirportTimeZoneInfo>>(File.ReadAllText(InputFolderPath + @"timezoneinfo.json"));
        }

        public void LoadTimeZoneNames()
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

        public void FindISOCodes()
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
            WriteObjectToFile(InputFolderPath + OutputFolderPath + @"airports.json", context.Airports);
            WriteObjectToFile(InputFolderPath + OutputFolderPath + @"cities.json", cities.Values);
            WriteObjectToFile(InputFolderPath + OutputFolderPath + @"countries.json", context.Countries);
            WriteObjectToFile(InputFolderPath + OutputFolderPath + @"locations.json", context.Locations);
        }

        private void WriteObjectToFile<T>(string path, IEnumerable<T> list) where T : class
        {
            var folderPath = path.Substring(0, path.LastIndexOf('\\'));
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var sb = new StringBuilder();
            sb.Append(JsonConvert.SerializeObject(list, Formatting.Indented));

            using (var stream = new FileStream(path, FileMode.Create))
            using (var streamWriter = new StreamWriter(stream))
            {
                streamWriter.Write(sb.ToString());
            }
        }
    }

    class UniqueCity
    {
        public string CityName { get; set; }

        public string CountryName { get; set; }
    }
}
