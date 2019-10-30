﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Bonsai.Data.Models
{
    /// <summary>
    /// User account.
    /// </summary>
    public class AppUser: IdentityUser
    {
        /// <summary>
        /// Flag indicating that the user's profile has been validated by the administrator.
        /// </summary>
        public bool IsValidated { get; set; }

        /// <summary>
        /// Flag indicating that the user has a login-password pair instead of an external OAuth provider.
        /// </summary>
        public bool UsesLocalAuth { get; set; }

        /// <summary>
        /// First name.
        /// </summary>
        [StringLength(256)]
        public string FirstName { get; set; }

        /// <summary>
        /// Last name.
        /// </summary>
        [StringLength(256)]
        public string LastName { get; set; }

        /// <summary>
        /// Middle name.
        /// </summary>
        [StringLength(256)]
        public string MiddleName { get; set; }

        /// <summary>
        /// Birthday.
        /// </summary>
        [StringLength(10)]
        public string Birthday { get; set; }

        /// <summary>
        /// User's own page.
        /// </summary>
        public Page Page { get; set; }

        /// <summary>
        /// ID of the page.
        /// </summary>
        public Guid? PageId { get; set; }

        /// <summary>
        /// Changes created by this user.
        /// </summary>
        public virtual ICollection<Changeset> Changes { get; set; }
    }
}
