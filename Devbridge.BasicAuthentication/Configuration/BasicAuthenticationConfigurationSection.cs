using System.Configuration;

namespace Devbridge.BasicAuthentication.Configuration
{
    public class BasicAuthenticationConfigurationSection : ConfigurationSection
    {
        private const string CredentialsNode = "credentials";
        private const string ExcludeVerbsNode = "excludeVerbs";
        private const string ExcludeUrlsNode = "excludeUrls";

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
        /// Gets or sets the URL exclusions.
        /// </summary>
        /// <value>
        /// The URL exclusions.
        /// </value>
        [ConfigurationProperty(ExcludeUrlsNode, IsRequired = false)]
        public ExcludeUrlElementCollection ExcludeUrls
        {
            get { return (ExcludeUrlElementCollection)this[ExcludeUrlsNode]; }
            set { this[ExcludeUrlsNode] = value; }
        }

        /// <summary>
        /// Gets or sets the Verb exclusions.
        /// </summary>
        /// <value>
        /// The Verb exclusions.
        /// </value>
        [ConfigurationProperty(ExcludeVerbsNode, IsRequired = false)]
        public ExcludeVerbElementCollection ExcludeVerbs
        {
            get { return (ExcludeVerbElementCollection)this[ExcludeVerbsNode]; }
            set { this[ExcludeVerbsNode] = value; }
        }  
    }
}