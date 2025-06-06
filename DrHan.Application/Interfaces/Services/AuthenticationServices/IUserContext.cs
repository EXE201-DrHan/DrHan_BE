using DrHan.Application.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Application.Interfaces.Services.AuthenticationServices
{
    public interface IUserContext
    {
        CurrentUser? GetCurrentUser();
        public int? GetCurrentUserId();
    }
}
