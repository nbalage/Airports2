using Airports.Logic.Models;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Airports.Logic.Services
{
    public class FileManager
    {
        readonly Logger logger;
        const string InputFolderPath = @"..\\..\\..\\..\\Data\";
        const string OutputFolderPath = @"\Output\";
        static string[] FileNames =
        {
            "airports.json",
            "cities.json",
            "countries.json",
            "locations.json"
        };

        public FileManager()
        {
            logger = LogManager.GetCurrentClassLogger();
        }

        public List<T> Deserialize<T>(string fileName) where T : class
        {
            return JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(InputFolderPath + OutputFolderPath + fileName));
        }

        public bool GetDataAvailability()
        {
            try
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
            catch (DirectoryNotFoundException ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        public ICollection<AirportTimeZoneInfo> DeserializeTimeZones()
        {
            return JsonConvert.DeserializeObject<List<AirportTimeZoneInfo>>(File.ReadAllText(InputFolderPath + @"timezoneinfo.json"));
        }

        public void WriteObjectToFile<T>(string fileName, IEnumerable<T> list) where T : class
        {
            var folderPath = InputFolderPath + OutputFolderPath + fileName.Substring(0, fileName.LastIndexOf('\\'));
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var sb = new StringBuilder();
            sb.Append(JsonConvert.SerializeObject(list, Formatting.Indented));

            using (var streamWriter = GetStreamForWrite(fileName))
            {
                streamWriter.Write(sb.ToString());
            }
        }

        public StreamReader GetStreamForRead(string fileName)
        {
            return new StreamReader(new FileStream(InputFolderPath + fileName, FileMode.Open));
        }

        public StreamWriter GetStreamForWrite(string fileName)
        {
            return new StreamWriter(new FileStream(fileName, FileMode.Create));
        }
    }
}
