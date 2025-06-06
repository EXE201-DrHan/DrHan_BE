using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.Commons
{
    public class SortingRequest
    {
        public string SortBy { get; }
        public bool IsDescending { get; }

        public SortingRequest(string sortBy = null, bool isDescending = false)
        {
            SortBy = sortBy ?? string.Empty;
            IsDescending = isDescending;
        }
    }
}
