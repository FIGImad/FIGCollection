using FIGCommon.Models;
using Microsoft.AspNetCore.Identity;

namespace FIGCommon.Interfaces
{
    public interface IIdentityService
    {
        UserManager<AspNetUser> UserManager { get; }
        RoleManager<IdentityRole> RoleManager { get; }
        IMessageService MessageService { get; }
    }
}
