using System.Configuration;

namespace Devbridge.BasicAuthentication.Configuration
{
    public class BasicAuthenticationConfigurationSection : ConfigurationSection
    {
        private const string CredentialsNode = "credentials";

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        /// <value>
        /// The credentials.
        /// </value>
        [ConfigurationProperty(CredentialsNode, IsRequired = false)]
        public CredentialElementCollection Credentials
        {
            get { return (CredentialElementCollection)this[CredentialsNode]; }
            set { this[CredentialsNode] = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether authenticaiton module should allow redirects without issuing auth challenge.
        /// </summary>
        /// <value>
        ///   <c>true</c> to allow redirects; otherwise, <c>false</c>.
        /// </value>
        [ConfigurationProperty("allowRedirects", DefaultValue = "false", IsRequired = false)]
        public bool AllowRedirects
        {
            get { return (bool)this["allowRedirects"]; }
            set { this["allowRedirects"] = value; }
        }
    }
}