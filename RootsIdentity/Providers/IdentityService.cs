using FIGCommon.Interfaces;
using FIGCommon.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;


namespace RootsIdentity.Providers
{
    public class IdentityService : IIdentityService
    {
        public UserManager<AspNetUser> UserManager { get; private set; }
        public RoleManager<IdentityRole> RoleManager { get; private set; }
        public IMessageService MessageService { get; private set; }

        public IdentityService(
            IAuthenticationSchemeProvider authenticationSchemeProvider
            , UserManager<AspNetUser> userManager
            , RoleManager<IdentityRole> roleManager
            , IMessageService messageService
            )
        {
            UserManager = userManager;
            RoleManager = roleManager;
            MessageService = messageService;
        }
    }
}
