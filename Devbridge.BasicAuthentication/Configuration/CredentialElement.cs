using System;
using System.Configuration;

namespace Devbridge.BasicAuthentication.Configuration
{
    public class CredentialElement : ConfigurationElement
    {
        private const string UserNameAttribute = "username";
        private const string PasswordAttribute = "password";

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The user name.
        /// </value>
        [ConfigurationProperty(UserNameAttribute, IsRequired = true)]
        public string UserName
        {
            get { return Convert.ToString(this[UserNameAttribute]); }
            set { this[UserNameAttribute] = value; }
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        [ConfigurationProperty(PasswordAttribute, IsRequired = true)]
        public string Password
        {
            get { return Convert.ToString(this[PasswordAttribute]); }
            set { this[PasswordAttribute] = value; }
        }
    }
}