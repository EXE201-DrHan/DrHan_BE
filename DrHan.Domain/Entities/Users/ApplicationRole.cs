using Microsoft.AspNetCore.Identity;

namespace DrHan.Domain.Entities.Users;

public class ApplicationRole : IdentityRole<int> 
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}