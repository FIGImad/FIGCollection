using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RootsIdentity.Models;
using RootsIdentity.Providers;
using System.Net;
using RootsIdentity.DataAccess;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using FIGCommon.Utilities;
using FIGCommon.Models;
using FIGCommon.Interfaces;

namespace RootsIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : AppControllerBase
    {
        public AccountController(
            IWebHostEnvironment hostEnvironment
            , IHttpContextAccessor httpContextAccessor
            , IIdentityService identityService
            , ILogger<AccountController> logger
            , IServiceProvider serviceProvider
            , IConfiguration config
            )
            : base(hostEnvironment, httpContextAccessor, identityService, logger, serviceProvider, config)
        {
        }

        #region Properties
        private string RedirectHtml
        {
            get
            {
                string key = "Redirect:Html";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectResetPasswordUrlFormat
        {
            get
            {
                string key = "Redirect:ResetPasswordUrlFormat";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectResetPasswordFailureUrlFormat
        {
            get
            {
                string key = "Redirect:ResetPasswordFailureUrlFormat";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectConfirmEmailUrlFormat
        {
            get
            {
                string key = "Redirect:ConfirmEmailUrlFormat";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectConfirmEmailAndNewPasswordUrlFormat
        {
            get
            {
                string key = "Redirect:ConfirmEmailAndNewPasswordUrlFormat";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectRefuteEmailUrlFormat
        {
            get
            {
                string key = "Redirect:RefuteEmailUrlFormat";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectConfirmRegistrationUrl
        {
            get
            {
                string key = "Redirect:ConfirmRegistrationUrl";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectEmailTag
        {
            get
            {
                string key = "Redirect:EmailTag";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectUserIdTag
        {
            get
            {
                string key = "Redirect:UserIdTag";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectUsernameTag
        {
            get
            {
                string key = "Redirect:UsernameTag";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectTokenIdTag
        {
            get
            {
                string key = "Redirect:TokenIdTag";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string RedirectUrlTag
        {
            get
            {
                string key = "Redirect:UrlTag";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private string this[string redirectUrl]
        {
            get
            {
                string key = redirectUrl;
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        private int ValidationTokenExpiryHours
        {
            get
            {
                string key = "ValidationTokenExpiryHours";
                string defVal = "0";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return Int32.Parse(val == null ? defVal : val);
            }
        }
        private string TempUserPassword
        {
            get
            {
                string key = "TempUser:Password";
                string defVal = "";
                string? val = _config == null ? defVal : _config[key] == null ? defVal : _config[key];
                return val == null ? defVal : val;
            }
        }

        #endregion Properties

        // GET api/account/confirmemail/{valpwd}/{userid}/{tokenid}
        [AllowAnonymous]
        [Route("confirmemail/{valpwd:int}/{userid:guid}/{tokenid:long}", Name = "ConfirmEmail")]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(int valpwd, string userid, long tokenid)
        {
            if (null == userid || 0 == tokenid)
            {
                return BadRequest();
            }

            string redirect = string.Format(RedirectRefuteEmailUrlFormat);

            var user = await _identityService.UserManager.FindByIdAsync(userid);
            long newTokenId = 0;

            if (user != null)
            {
                ValidationTokenRS? validationToken = AccountsRepo.SelectValidationToken(tokenid);
                if (null != validationToken && validationToken.UserId == userid)
                {
                    TimeSpan span = DateTime.UtcNow - validationToken.Timestamp;
                    if (span.TotalHours > ValidationTokenExpiryHours)
                    {
                        AccountsRepo.DeleteValidationToken(tokenid);
                        validationToken = null;
                        _logger.LogInformation($"ConfirmEmail(): Validation token has expired. User={user.Email}, token ID={validationToken?.ValidationTokenId}");
                    }

                    // this will also fail if email has been confirmed already
                    await _identityService.UserManager.ConfirmEmailAsync(user, validationToken?.Token??"");
                    //AccountsRepo.DeleteValidationToken(tokenid);
                    if (await _identityService.UserManager.IsEmailConfirmedAsync(user))
                    {
                        //if (1 == valpwd)
                        //{
                        //    var passwordValidationToken = new ValidationToken()
                        //    {
                        //        ValidationTokenId = ValidationToken.GenerateValidationTokenId(),
                        //        Token = await _identityService.UserManager.GeneratePasswordResetTokenAsync(user),
                        //        UserId = userid
                        //    };
                        //    AccountsRepo.InsertValidationToken(passwordValidationToken);
                        //    newTokenId = passwordValidationToken.ValidationTokenId;
                        //    redirect = string.Format(RedirectConfirmEmailUrlFormat);
                        //}
                        if (2 == valpwd || 1 == valpwd)
                        {
                            var passwordValidationToken = new ValidationTokenRS()
                            {
                                ValidationTokenId = ValidationTokenRS.GenerateValidationTokenId(),
                                Token = await _identityService.UserManager.GeneratePasswordResetTokenAsync(user),
                                UserId = userid
                            };
                            AccountsRepo.InsertValidationToken(passwordValidationToken);
                            newTokenId = passwordValidationToken.ValidationTokenId;
                            redirect = string.Format(RedirectConfirmEmailAndNewPasswordUrlFormat);
                        }
                        else
                        {
                            redirect = string.Format(RedirectConfirmEmailUrlFormat);
                        }
                    }
                    else
                    {
                        // token validation failed & email is not confirmed - delete account
                        await _identityService.UserManager.DeleteAsync(user);
                        _logger.LogInformation($"ConfirmEmail(): Invalid/expired token. Email has not been confirmed. User {user.Email} deleted.");
                        redirect = string.Format(RedirectConfirmEmailUrlFormat);
                    }
                }
                else
                {
                    // must be already confirmed or link already expired
                    // checked if already confirmed e-mail
                    if (await _identityService.UserManager.IsEmailConfirmedAsync(user))
                    {
                        // check if password is still random one
                        if (2 == valpwd)
                        {
                            if (await _identityService.UserManager.CheckPasswordAsync(user, TempUserPassword))
                            {
                                var passwordValidationToken = new ValidationTokenRS()
                                {
                                    ValidationTokenId = ValidationTokenRS.GenerateValidationTokenId(),
                                    Token = await _identityService.UserManager.GeneratePasswordResetTokenAsync(user),
                                    UserId = userid
                                };
                                AccountsRepo.InsertValidationToken(passwordValidationToken);
                                newTokenId = passwordValidationToken.ValidationTokenId;
                                redirect = string.Format(RedirectConfirmEmailAndNewPasswordUrlFormat);
                            }
                            else
                            {
                                _logger.LogError($"ConfirmEmail(): Failed to find a valid token for the user with ID = {userid} and user is not new");
                            }
                        }
                        else
                        {
                            _logger.LogError($"ConfirmEmail(): Failed to find a valid token for the user with ID = {userid} and user is not new");
                        }
                    }
                    else
                    {
                        _logger.LogError($"ConfirmEmail(): Failed to find a valid token for the user with ID = {userid} and email is not confirmed");
                    }
                }
            }
            else
            {
                _logger.LogError($"ConfirmEmail(): Failed to find user with ID = {userid}");
            }

            var redirectUri = new Uri(new Uri(BaseUrl), redirect);
            _logger.LogInformation($"ConfirmEmail(): RedirectUri = {redirectUri}");

            var content = System.IO.File.ReadAllText(Path.Combine(ContentPhysicalFolder, RedirectHtml), new System.Text.UTF8Encoding());
            content = content.Replace(RedirectUserIdTag, userid);
            content = content.Replace(RedirectUsernameTag, null != user ? user.UserName : "");
            content = content.Replace(RedirectUrlTag, redirectUri.ToString());
            content = content.Replace(RedirectTokenIdTag, newTokenId.ToString());
            content = content.Replace(RedirectEmailTag, null != user ? user.Email : "");

            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = content
            };
        }

        [Route("login")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest, [FromServices] SignInManager<AspNetUser> signInManager)
        {
            try
            {
                var result = await signInManager.PasswordSignInAsync(loginRequest.Username, loginRequest.Password, false, false);
                if (result.Succeeded)
                {
                    var user = await _identityService.UserManager.Users.FirstOrDefaultAsync(u => u.UserName == loginRequest.Username || u.Email == loginRequest.Username);
                    if (user != null)
                    {
                        var roles = await _identityService.UserManager.GetRolesAsync(user);
                        var isEmailConfirmed = await _identityService.UserManager.IsEmailConfirmedAsync(user);
                        if (isEmailConfirmed || (roles.Count > 0 && roles.Contains(Role.SVC)))
                        {
                            return Ok(JwtToken.GenerateJwtToken(loginRequest.Username, user, roles, null, _config));
                        }
                        else
                        {
                            return OnBadRequest(ErrorCodes.AuthError_EmailConfirm);
                        }
                    }
                    else
                    {
                        return OnBadRequest(ErrorCodes.AuthError_InvalidCred);
                    }
                }
                else
                {
                    return OnBadRequest(ErrorCodes.AuthError_InvalidCred);
                }
            }
            catch (Exception e)
            {
                return OnException(e, ErrorCodes.AuthError_Login);
            }
        }

        // POST api/account/forgotpassword
        [AllowAnonymous]
        [HttpPost]
        [Route("forgotpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _identityService.UserManager.FindByNameAsync(request.UserName??"");

            if (user == null || !await _identityService.UserManager.IsEmailConfirmedAsync(user))
            {
                // don't reveal that the user does not exist or is not confirmed
                return Ok();
            }

            var validationToken = new ValidationTokenRS()
            {
                ValidationTokenId = ValidationTokenRS.GenerateValidationTokenId(),
                Token = await _identityService.UserManager.GeneratePasswordResetTokenAsync(user),
                UserId = user.Id
            };

            try
            {
                AccountsRepo.InsertValidationToken(validationToken);

                var uri = new Uri(new Uri(BaseUrl), $"api/account/confirmresetpassword/{user.Id}/{validationToken.ValidationTokenId}");

                _logger.LogDebug($"ForgotPassword(): uri={uri}, user ID={user.Id}");

                var message = new ResetPasswordMessage(
                    uri.ToString(),
                    MessagesHtmlTemplatesFolder,
                    _config
                    );
                message.Build();

                await _identityService.MessageService.Send(user.Email??"", message.Subject, message.Body);

                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/account/confirmresetpassword
        [AllowAnonymous]
        [Route("confirmresetpassword/{userid:guid}/{tokenid:long}", Name = "ConfirmResetPassword")]
        [HttpGet]
        public async Task<IActionResult> ConfirmResetPassword(string userid, long tokenid)
        {
            if (null == userid || 0 == tokenid)
            {
                return BadRequest();
            }

            string redirect = string.Format(RedirectResetPasswordFailureUrlFormat);

            AspNetUser? user = await _identityService.UserManager.FindByIdAsync(userid);
            if (user != null)
            {
                ValidationTokenRS? validationToken = AccountsRepo.SelectValidationToken(tokenid);
                if (null != validationToken)
                {
                    redirect = string.Format(RedirectConfirmEmailAndNewPasswordUrlFormat);
                    //redirect = string.Format(RedirectResetPasswordUrlFormat);
                }
            }

            var uri = new Uri(new Uri(BaseUrl), redirect);

            var content = System.IO.File.ReadAllText(Path.Combine(ContentPhysicalFolder, RedirectHtml), new System.Text.UTF8Encoding());
            content = content.Replace(RedirectUserIdTag, userid);
            content = content.Replace(RedirectUsernameTag, null != user ? user.UserName : "");
            content = content.Replace(RedirectTokenIdTag, tokenid.ToString());
            content = content.Replace(RedirectUrlTag, uri.ToString());

            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = content
            };
        }

        // POST api/Account/resetpassword/
        [AllowAnonymous]
        [Route("resetpassword")]
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, [FromServices] SignInManager<AspNetUser> signInManager)
        {
            var user = await _identityService.UserManager.FindByIdAsync(request.UserId);
            if (null == user || !user.EmailConfirmed)
            {
                return BadRequest();
            }

            ValidationTokenRS? validationToken = AccountsRepo.SelectValidationToken(request.TokenId);
            if (null == validationToken || validationToken.UserId != user.Id)
            {
                return BadRequest();
            }

            var result = await _identityService.UserManager.ResetPasswordAsync(user, validationToken.Token, request.NewPassword);

            if (result.Succeeded)
            {
                AccountsRepo.DeleteValidationTokens(request.UserId);
                var resSignIn = signInManager.PasswordSignInAsync(user.UserName??"", request.NewPassword, false, false);
                if (resSignIn.Result.Succeeded)
                {
                    var roles = await _identityService.UserManager.GetRolesAsync(user);
                    return Ok(JwtToken.GenerateJwtToken(user.UserName ?? "", user, roles, null, _config));
                }
                return BadRequest("Invalid username and/or password");
            }

            return BadRequest();
        }


        // POST api/Account/changepassword/force
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("changepassword/force")]
        [HttpPost]
        public async Task<IActionResult> ForceChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var user = await _identityService.UserManager.FindByNameAsync(request.UserName);
                if (null == user)
                {
                    return BadRequest();
                }

                var token = await _identityService.UserManager.GeneratePasswordResetTokenAsync(user);
                var result = await _identityService.UserManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (result.Succeeded)
                {
                    return Ok(result);
                }
                return BadRequest(result.Errors);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/Account/changepassword/
        [Route("changepassword")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var aspNetUser = await _identityService.UserManager.GetUserAsync(HttpContext.User);
                var user = await _identityService.UserManager.FindByNameAsync(request.UserName);
                if (null == user || !user.EmailConfirmed)
                {
                    return BadRequest();
                }

                if (!await _identityService.UserManager.CheckPasswordAsync(user, request.Password))
                {
                    return BadRequest();
                }
                // can only change password for self
                //if (aspNetUser.Id != user.Id)
                //{
                //    return BadRequest();
                //}

                var token = await _identityService.UserManager.GeneratePasswordResetTokenAsync(user);
                var result = await _identityService.UserManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (result.Succeeded)
                {
                    return Ok(result);
                }
                return BadRequest(result.Errors);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/Account/createpassword
        [Route("createpassword")]
        [HttpPost]
        public async Task<IActionResult> CreatePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var user = await _identityService.UserManager.FindByNameAsync(request.UserName);
                if (null == user || !user.EmailConfirmed)
                {
                    return BadRequest();
                }

                var checkPassword = await _identityService.UserManager.CheckPasswordAsync(user, TempUserPassword);
                if (!checkPassword)
                {
                    return BadRequest();
                }
                // can only change password for self
                //if (aspNetUser.Id != user.Id)
                //{
                //    return BadRequest();
                //}

                var token = await _identityService.UserManager.GeneratePasswordResetTokenAsync(user);
                var result = await _identityService.UserManager.ResetPasswordAsync(user, token, request.NewPassword);
                if (result.Succeeded)
                {
                    return Ok(result);
                }
                return BadRequest(result.Errors);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // POST api/Account/logout/
        [Route("logout")]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Ok();
        }

        // GET api/account/users
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("users")]
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var response = AccountsRepo.GetUsers();
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


        // POST api/account/user
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("user")]
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] UserRS user)
        {
            try
            {
                var existingUser = await _identityService.UserManager.FindByNameAsync(user.UserName);
                if (null == existingUser)
                {
                    var seeder = new Seeder(_identityService.UserManager, _identityService.RoleManager, _config);
                    var identityUser = user.ToIdentityUser();
                    var identityRoles = user.ToIdentityRoles();
                    //var password = Seeder.GetRandomPassword(12);
                    var password = TempUserPassword;

                    AspNetUser newUser = await seeder.CreateUser(identityUser, password, identityRoles);
                    await SendConfirmationEmailAsync(identityUser, false, true);

                    return Ok(user);
                }
                else
                {
                    return BadRequest("User already registered");
                }
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // PUT api/account/user
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("user")]
        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UserRS user)
        {
            try
            {
                IdentityResult result;
                var existingUser = await _identityService.UserManager.FindByNameAsync(user.UserName);
                if (existingUser == null)
                {
                    throw new Exception($"Failed to update email address for '{user.UserName}' - User not found");
                }
                var existingRoles = await _identityService.UserManager.GetRolesAsync(existingUser);

                if (!String.IsNullOrEmpty(user.Email) && user.Email.ToUpper() != existingUser.NormalizedEmail)
                {
                    result = await _identityService.UserManager.SetEmailAsync(existingUser, user.Email);
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to update email address for'{user.UserName}'");
                    }
                    await SendConfirmationEmailAsync(existingUser, false, false);
                }
                if (!String.IsNullOrEmpty(user.PhoneNumber) && user.PhoneNumber.ToUpper() != existingUser.PhoneNumber)
                {
                    result = await _identityService.UserManager.SetPhoneNumberAsync(existingUser, user.PhoneNumber);
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to update phone number for'{user.UserName}'");
                    }
                }
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                result = await _identityService.UserManager.UpdateAsync(existingUser);
                if (!result.Succeeded)
                {
                    // user cascade deleted when tenant is deleted
                    throw new Exception($"Failed to update user '{user.UserName}'");
                }

                // compare roles
                if (user.Roles != null)
                {
                    List<string> newRoles = new List<string>();
                    for (int ndx = 0; ndx < user.Roles.Count; ndx++)
                    {
                        bool found = false;
                        for (int existNdx = 0; existNdx < existingRoles.Count; existNdx++)
                        {
                            if (user.Roles.ElementAt<string>(ndx).ToUpper() == existingRoles.ElementAt<string>(existNdx).ToUpper())
                            {
                                // match, clear role from existing and userRoles
                                existingRoles.RemoveAt(existNdx);
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            newRoles.Add(user.Roles.ElementAt<string>(ndx).ToUpper());
                        }
                    }

                    // roles to delete
                    foreach (string role in existingRoles)
                    {
                        await _identityService.UserManager.RemoveFromRoleAsync(existingUser, role);
                    }

                    // roles to add
                    foreach (string role in newRoles)
                    {
                        await _identityService.UserManager.AddToRoleAsync(existingUser, role);
                    }
                }

                return Ok(user);
            }
            catch (Exception e)
            {
                return OnException(e, ErrorCodes.AuthError_Registration);
            }

        }

        // DELETE api/account/user/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("user/{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var existingUser = await _identityService.UserManager.FindByIdAsync(id);
                if (existingUser == null)
                {
                    throw new Exception("AuthError_InvalidUser");
                }
                //ValidationToken validationToken = Repository.SelectValidationTokenByUserName(request.Email);
                await _identityService.UserManager.DeleteAsync(existingUser);
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }



        // GET api/account/username/{username}
        [Authorize]
        [Route("username/{username}", Name = "GetUserByName")]
        [HttpGet]
        public async Task<IActionResult> GetUserByName(string username)
        {
            try
            {
                var user = await _identityService.UserManager.FindByNameAsync(username);
                if (null != user)
                {
                    var response = AccountsRepo.SelectUser(user.Id);
                    return Ok(response);
                }
                else
                {
                    throw new Exception("AuthError_InvalidUser");
                }
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // PUT api/account/resendconf/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("resendconf/{id}")]
        [HttpPost]
        public async Task<IActionResult> ResendConfirmation(string id)
        {
            try
            {
                var existingUser = await _identityService.UserManager.FindByIdAsync(id);
                if (null != existingUser)
                {
                    await SendConfirmationEmailAsync(existingUser, !existingUser.EmailConfirmed, false);
                }
                else
                {
                    throw new Exception("AuthError_InvalidUser");
                }
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }
        // PUT api/account/emailconf/{id}
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("emailconf/{id}")]
        [HttpPost]
        public async Task<IActionResult> ForceEmailConfirmation(string id)
        {
            try
            {
                var existingUser = await _identityService.UserManager.FindByIdAsync(id);
                if (null != existingUser)
                {
                    await ForceEmailConfirmation(existingUser);
                }
                else
                {
                    throw new Exception("AuthError_InvalidUser");
                }
                return Ok();
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }

        // GET api/account/userroles
        [Authorize(Roles = Role.Admin + "," + Role.SuperAdmin)]
        [Route("userroles")]
        [HttpGet]
        public async Task<IActionResult> GetUsersRoles()
        {
            try
            {
                var response = AccountsRepo.GetUsersRoles();
                return Ok(response);
            }
            catch (Exception e)
            {
                return OnException(e);
            }
        }


    }
}