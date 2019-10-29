using Airports.Logic.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Airports.Logic.Services.Interfaces
{
    public interface IDataLoader
    {
        bool AreDataAvailable();
        AirportContext LoadData();
        AirportContext ReadImportedFiles();
    }
}
