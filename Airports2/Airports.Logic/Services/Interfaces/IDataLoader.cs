using Airports.Logic.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Airports.Logic.Services.Interfaces
{
    public interface IDataLoader
    {
        public bool AreDataAvailable { get; }
        AirportContext LoadData();
        AirportContext ReadImportedFiles();
    }
}
