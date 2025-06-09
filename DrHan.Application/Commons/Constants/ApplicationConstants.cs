using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.Commons.Constants
{
    public static class ApplicationConstants
    {
    }
    public static class Location
    {
        public static readonly string AbsoluteProjectPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

    }
}
