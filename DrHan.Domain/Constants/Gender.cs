using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DrHan.Domain.Constants
{
    [JsonConverter(typeof(JsonStringEnumConverter))] // Required for System.Text.Json
    public enum Gender
    {
        [EnumMember(Value = "Male")]
        Male,

        [EnumMember(Value = "Female")]
        Female,

        [EnumMember(Value = "Other")]
        Other,
    }
}
