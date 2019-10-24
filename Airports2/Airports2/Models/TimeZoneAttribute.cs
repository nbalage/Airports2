using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Airports2
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TimeZoneAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            TimeSpan time;
            if (!TimeSpan.TryParse(value.ToString(), out time))
            {
                return new ValidationResult("Not a valid timespan format!");
            }

            return ValidationResult.Success;
        }
    }
}
