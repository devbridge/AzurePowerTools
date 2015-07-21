using System;
using System.Configuration;

namespace Devbridge.BasicAuthentication.Configuration
{
    public class ExcludeElement : ConfigurationElement
    {
        private const string UrlAttribute = "url";
        private const string VerbAttribute = "verb";

        /// <summary>
        /// Gets or sets the url to exclude.
        /// </summary>
        /// <value>
        /// The url.
        /// </value>
        [ConfigurationProperty(UrlAttribute, IsRequired = false, IsKey = false)]
        public string Url
        {
            get { return Convert.ToString(this[UrlAttribute]); }
            set { this[UrlAttribute] = value; }
        }

        /// <summary>
        /// Gets or sets the verb to exclude.
        /// </summary>
        /// <value>
        /// The verb.
        /// </value>
        [ConfigurationProperty(VerbAttribute, IsRequired = false, IsKey = false)]
        public string Verb
        {
            get { return Convert.ToString(this[VerbAttribute]); }
            set { this[VerbAttribute] = value; }
        }

        public override string ToString()
        {
            return string.Concat(Url, '_', Verb);
        }
    }
}
