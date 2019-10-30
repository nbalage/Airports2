using Airports.Logic.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Airports.Logic.Services.Interfaces
{
    public interface IFileManager
    {
        ICollection<T> Deserialize<T>(string fileName) where T : class;
        bool GetDataAvailability();
        ICollection<AirportTimeZoneInfo> DeserializeTimeZones();
        void WriteObjectToFile<T>(string fileName, IEnumerable<T> list) where T : class;
        StreamReader GetStreamForRead(string fileName);
        StreamWriter GetStreamForWrite(string fileName);
    }
}
