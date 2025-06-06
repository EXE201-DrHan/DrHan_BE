using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Domain.Constants.Status
{
    public enum UserStatus
    {
        [EnumMember(Value = "Enabled")]
        Enabled,

        [EnumMember(Value = "Disabled")]
        Disabled
    }
}
