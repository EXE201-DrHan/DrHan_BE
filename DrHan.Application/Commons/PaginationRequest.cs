using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.Commons
{
    public class PaginationRequest
    {
        public int PageNumber { get; }
        public int PageSize { get; }

        public PaginationRequest(int pageNumber = 1, int pageSize = 10)
        {
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;
        }

        public int Skip => (PageNumber - 1) * PageSize;
    }
}
