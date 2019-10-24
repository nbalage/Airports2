using System;
using Airports.Logic.Models;
using Airports.Logic.Services;

namespace Airports
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            AirportContext context;
            DataLoader loader = new DataLoader();
            if (!loader.AreDataAvailable)
            {
                context = loader.LoadData();
            }
            else
            {
                context = loader.ReadImportedFiles();
            }

            //DataProcessor processor = new DataProcessor(context);
            //Console.WriteLine(processor.CountCountries());
            //Console.WriteLine(processor.MaxAirportNumberInOneCity());
        }
    }
}
