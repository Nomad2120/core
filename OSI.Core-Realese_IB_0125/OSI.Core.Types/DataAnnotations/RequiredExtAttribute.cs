using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.DataAnnotations
{
    public class RequiredExtAttribute : System.ComponentModel.DataAnnotations.RequiredAttribute
    {        //
        // Summary:
        //     Gets or sets a value that indicates whether a default value is allowed.
        //
        // Returns:
        //     true if a default value is allowed; otherwise, false. The default value is false.
        public bool AllowDefault { get; set; }

        public override bool IsValid(object value)
        {
            var isValidMethod = typeof(RequiredExtAttribute).GetMethod(nameof(InternalIsValid), BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(value.GetType());
            var isValid = (bool)isValidMethod.Invoke(this, new[] { value });
            return isValid && base.IsValid(value);
        }

        private bool InternalIsValid<T>(T value)
        {
            return AllowDefault || !Equals(value, default(T));
        }
    }
}
