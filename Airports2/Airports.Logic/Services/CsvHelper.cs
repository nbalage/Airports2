using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Airports.Logic.Services
{
    public static class CsvHelper
    {
        public static IEnumerable<T> Parse<T>(string source) where T : class
        {
            List<T> retVal = new List<T>();

            using (var fr = new FileStream(source, FileMode.Open))
            using (var sr = new StreamReader(fr))
            {
                string line;
                bool firstRow = true;
                string[] columnNames = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (firstRow)
                    {
                        firstRow = false;
                        columnNames = GetColumns(line);
                        continue;
                    }
                    var columns = GetColumns(line);
                    var instance = CreateObject<T>(columnNames, columns);
                    if (instance != null && Validator.TryValidateObject(instance, new ValidationContext(instance), new List<ValidationResult>(), true))
                    {
                        retVal.Add(instance);
                    }
                }
            }

            return retVal;
        }

        private static string[] GetColumns(string line)
        {
            return line.AirportSplit(',');
        }

        private static T CreateObject<T>(string[] columnNames, string[] columns) where T : class
        {
            var instance = Activator.CreateInstance(typeof(T));
            var type = instance.GetType();

            try
            {
                for (int i = 0; i < columnNames.Length; i++)
                {
                    var prop = GetColumnName(type, columnNames[i]);
                    if (prop != null)
                    {
                        if (prop.PropertyType == typeof(TimeSpan))
                        {
                            TimeSpan ts;
                            TimeSpan.TryParse(columns[i].Trim('"'), out ts);
                            prop.SetValue(instance, ts);
                        }
                        else
                        {
                            prop.SetValue(instance, Convert.ChangeType(columns[i].Trim('"'), prop.PropertyType));
                        }
                    }
                }
            }
            catch (InvalidCastException ex)
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Error(ex);
                return null;
            }

            return instance as T;
        }

        private static PropertyInfo GetColumnName(Type type, string columnName)
        {
            PropertyInfo property = null;

            foreach (var prop in type.GetProperties())
            {
                var attributes = prop.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(ColumnAttribute) &&
                a.ConstructorArguments.Count(ar => ar.Value.ToString() == columnName) > 0);

                if (attributes != null || prop.Name.ToLower() == columnName.ToLower())
                {
                    property = prop;
                }
            }

            return property;
        }
    }
}
