using Airports2.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Airports2.Services
{
    class DataLoader
    {
        const string InputFolderPath = @"..\\..\\..\\Data\";
        const string OutputFolderPath = @"\Output\";
        readonly Logger logger;

        static IDictionary<string, Country> countries;
        static IDictionary<UniqueCity, City> cities;
        static IList<Location> locations;
        static IDictionary<int, Airport> airports;
        static IDictionary<string, AirportTimeZoneInfo> timeZones;
        static string[] FileNames =
        {
            "airports.json",
            "cities.json",
            "countries.json",
            "locations.json"
        };

        public bool AreDataAvailable
        {
            get
            {
                return CheckDataAvailability();
            }
        }

        public DataLoader()
        {
            logger = LogManager.GetCurrentClassLogger();
            countries = new Dictionary<string, Country>();
            cities = new Dictionary<UniqueCity, City>();
            locations = new List<Location>();
            airports = new Dictionary<int, Airport>();
            timeZones = new Dictionary<string, AirportTimeZoneInfo>();
        }

        private bool CheckDataAvailability()
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

        public AirportContext LoadData()
        {
            var pattern = "^[0-9]{1,4},(\".*\",){3}(\"[A-Za-z]+\",){2}([-0-9]{1,4}(\\.[0-9]{0,})?,){2}";

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
            AirportContext context = new AirportContext();
            context.Airports = airports.Values;
            context.Cities = cities.Values;
            context.Countries = countries.Values;
            context.Locations = locations;

            SerializeObjects();

            return context;
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
            airports.Add(airport.Id, airport);
        }

        public string GenerateFullName(string name)
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

        public Country CreateCountry(string[] data)
        {
            var country = countries.SingleOrDefault(c => c.Key == data[3].Trim('"')).Value;
            if (country == null)
            {
                var newCountry = new Country
                {
                    Id = countries.Count > 0 ? countries.Values.Max(c => c.Id) + 1 : 1,
                    Name = data[3].Trim('"')
                };
                countries.Add(newCountry.Name, newCountry);
                country = newCountry;
            }

            return country;
        }

        public Location CreateLocation(string[] data)
        {
            var location = locations.SingleOrDefault(l => l.Longitude.ToString() == data[6]
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

                locations.Add(newLocation);
                location = newLocation;
            }

            return location;
        }

        public City CreateCity(string[] data, Country country)
        {
            var city = cities.SingleOrDefault(c => c.Key == new UniqueCity { CityName = data[2].Trim('"'), CountryName = country.Name }).Value; // c.Key == data[2].Trim('"') + "_" + country.Name
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
                city = newCity;
            }

            return city;
        }

        public void DeserializeTimeZones()
        {
            timeZones = JsonConvert.DeserializeObject<List<AirportTimeZoneInfo>>(File.ReadAllText(InputFolderPath + @"timezoneinfo.json"))
                        .ToDictionary(t => t.AirportId.ToString(), t => t);
        }

        public void LoadTimeZoneNames()
        {
            foreach (var zone in timeZones.Values)
            {
                var airport = airports.SingleOrDefault(a => a.Key == zone.AirportId).Value;
                if (airport != null)
                {
                    var city = cities.Single(c => c.Value.Id == airport.CityId).Value;
                    airport.TimeZoneName = zone.TimeZoneInfoId;
                    city.TimeZoneName = zone.TimeZoneInfoId;
                }
            }
        }

        public void FindISOCodes()
        {
            foreach (var airport in airports.Values)
            {
                var culture = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                                                            .FirstOrDefault(c => c.EnglishName.Contains(airport.Country.Name));

                if (culture != null)
                {
                    try
                    {
                        var regInfo = new RegionInfo(culture.Name);
                        airport.Country.TwoLetterISOCode = regInfo.TwoLetterISORegionName;
                        airport.Country.ThreeLetterISOCode = regInfo.ThreeLetterISORegionName;
                    }
                    catch (Exception)
                    {
                        logger.Info($"Culture ({culture.EnglishName}) is not correct.");
                    }
                }
            }
        }

        public void SerializeObjects()
        {
            WriteObjectToFile(InputFolderPath + OutputFolderPath + @"airports.json", airports.Values);
            WriteObjectToFile(InputFolderPath + OutputFolderPath + @"cities.json", cities.Values);
            WriteObjectToFile(InputFolderPath + OutputFolderPath + @"countries.json", countries.Values);
            WriteObjectToFile(InputFolderPath + OutputFolderPath + @"locations.json", locations);
        }

        public AirportContext ReadImportedFiles()
        {
            AirportContext context = new AirportContext();
            context.Airports = JsonConvert.DeserializeObject<List<Airport>>(File.ReadAllText(InputFolderPath + OutputFolderPath + "airports.json")); // .ToDictionary(a => a.Id, a => a)
            context.Cities = JsonConvert.DeserializeObject<List<City>>(File.ReadAllText(InputFolderPath + OutputFolderPath + "cities.json")); // .ToDictionary(a => new UniqueCity { CityName = a.Name, CountryName = a.Country.Name }, a => a)
            context.Countries = JsonConvert.DeserializeObject<List<Country>>(File.ReadAllText(InputFolderPath + OutputFolderPath + "countries.json")); // .ToDictionary(a => a.Name, a => a)
            context.Locations = JsonConvert.DeserializeObject<List<Location>>(File.ReadAllText(InputFolderPath + OutputFolderPath + "locations.json"));
            return context;
        }

        public StreamReader OpenStreamReader(string path)
        {
            var stream = new FileStream(path, FileMode.Open);
            return new StreamReader(stream);
        }

        public void WriteObjectToFile<T>(string path, IEnumerable<T> list) where T : class
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
