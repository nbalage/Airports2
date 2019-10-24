using Airports2.Models;
using Airports2.Services;
using Microsoft.Extensions.Configuration;
using System;

namespace Airports2
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
        }
    }
}
