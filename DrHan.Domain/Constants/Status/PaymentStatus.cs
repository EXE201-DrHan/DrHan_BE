using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Domain.Constants.Status
{
    public enum PaymentStatus
    {
        [EnumMember(Value = "Pending")]
        Pending,
        [EnumMember(Value = "Success")]
        Success,
        [EnumMember(Value = "Failed")]
        Failed
    }
}
