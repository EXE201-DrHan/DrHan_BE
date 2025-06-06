using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Domain.Exceptions
{
    public class DuplicateUserException : Exception
    {
        public DuplicateUserException(string message) : base(message)
        {
        }
    }
}
