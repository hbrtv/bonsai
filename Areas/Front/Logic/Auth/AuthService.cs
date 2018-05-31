﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bonsai.Areas.Front.ViewModels.Auth;
using Bonsai.Code.Tools;
using Bonsai.Data;
using Bonsai.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bonsai.Areas.Front.Logic.Auth
{
    /// <summary>
    /// Wrapper service for authorization.
    /// </summary>
    public class AuthService
    {
        public AuthService(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, AppDbContext db)
        {
            _signMgr = signInManager;
            _userMgr = userManager;
            _db = db;
        }

        private readonly SignInManager<AppUser> _signMgr;
        private readonly UserManager<AppUser> _userMgr;
        private readonly AppDbContext _db;

        /// <summary>
        /// Attempts to authenticate the user.
        /// </summary>
        public async Task<LoginResultVM> LoginAsync(AuthenticateResult authResult)
        {
            var extLogin = GetUserLoginData(authResult?.Principal);
            var result = await LoginInternalAsync(extLogin).ConfigureAwait(false);

            return new LoginResultVM
            {
                Status = result,
                ExternalLogin = extLogin
            };
        }

        /// <summary>
        /// Logs the user out.
        /// </summary>
        public async Task LogoutAsync()
        {
            await _signMgr.SignOutAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the user.
        /// </summary>
        public async Task<RegisterUserResultVM> RegisterAsync(RegisterUserVM vm, ExternalLoginData extLogin)
        {
            var errors = await ValidateRegisterRequestAsync(vm).ConfigureAwait(false);
            if (errors.Any())
                return new RegisterUserResultVM {ErrorMessages = errors};

            var id = Guid.NewGuid().ToString();
            var user = new AppUser
            {
                Id = id,
                Email = vm.Email,
                FirstName = vm.FirstName,
                MiddleName = vm.MiddleName,
                LastName = vm.LastName,
                Birthday = vm.Birthday
            };

            var createResult = await _userMgr.CreateAsync(user).ConfigureAwait(false);
            if (!createResult.Succeeded)
            {
                var msgs = createResult.Errors.Select(x => new KeyValuePair<string, string>("", x.Description)).ToList();
                return new RegisterUserResultVM {ErrorMessages = msgs};
            }

            _db.UserLogins.Add(new IdentityUserLogin<string>
            {
                LoginProvider = extLogin.LoginProvider,
                ProviderKey = extLogin.ProviderKey,
                UserId = id
            });

            await _db.SaveChangesAsync().ConfigureAwait(false);

            return new RegisterUserResultVM();
        }

        #region Constants

        private const string UserIdClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

        #endregion

        #region Private helpers

        /// <summary>
        /// Returns the information about a user.
        /// </summary>
        private ExternalLoginData GetUserLoginData(ClaimsPrincipal principal)
        {
            var identity = principal?.Identity as ClaimsIdentity;
            var claim = identity?.FindFirst(UserIdClaimType);

            if (claim == null)
                return null;

            return new ExternalLoginData(claim.Issuer, claim.Value);
        }

        /// <summary>
        /// Finds the corresponding user.
        /// </summary>
        private async Task<AppUser> FindUserAsync(ExternalLoginData data)
        {
            var login = await _db.UserLogins
                                 .FirstOrDefaultAsync(x => x.LoginProvider == data.LoginProvider
                                                           && x.ProviderKey == data.ProviderKey)
                                 .ConfigureAwait(false);

            if (login == null)
                return null;

            return await _db.Users
                            .FirstOrDefaultAsync(x => x.Id == login.UserId)
                            .ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the authorization and returns the status.
        /// </summary>
        private async Task<LoginStatus> LoginInternalAsync(ExternalLoginData extLogin)
        {
            if (extLogin == null)
                return LoginStatus.Failed;

            var user = await FindUserAsync(extLogin).ConfigureAwait(false);
            if (user == null)
                return LoginStatus.NewUser;

            var isLockedOut = await _userMgr.IsLockedOutAsync(user).ConfigureAwait(false);
            if (isLockedOut)
                return LoginStatus.LockedOut;

            if (user.IsValidated == false)
                return LoginStatus.Unvalidated;

            await _signMgr.SignInAsync(user, true).ConfigureAwait(false);

            return LoginStatus.Succeeded;
        }

        /// <summary>
        /// Performs additional checks on the registration request.
        /// </summary>
        private async Task<IReadOnlyList<KeyValuePair<string, string>>> ValidateRegisterRequestAsync(RegisterUserVM vm)
        {
            var result = new Dictionary<string, string>();

            if (FuzzyDate.TryParse(vm.Birthday) == null)
                result[nameof(vm.Birthday)] = "Дата рождения указана неверно.";

            var emailExists = await _db.Users.AnyAsync(x => x.Email == vm.Email).ConfigureAwait(false);
            if (emailExists)
                result[nameof(vm.Email)] = "Адрес электронной почты уже зарегистрирован.";

            return result.ToList();
        }

        #endregion
    }
}
