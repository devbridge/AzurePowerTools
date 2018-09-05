using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Devbridge.BasicAuthentication
{
    /// <summary>
    /// This module performs basic authentication.
    /// For details on basic authentication see RFC 2617.
    /// Based on the work by Mike Volodarsky (www.iis.net/learn/develop/runtime-extensibility/developing-a-module-using-net)
    ///
    /// The basic operational flow is:
    ///
    /// On AuthenticateRequest:
    ///     extract the basic authentication credentials
    ///     verify the credentials
    ///     if succesfull, create and send authentication cookie
    ///
    /// On SendResponseHeaders:
    ///     if there is no authentication cookie in request, clear response, add unauthorized status code (401) and
    ///     add the basic authentication challenge to trigger basic authentication.
    /// </summary>
    public class BasicAuthenticationModule : IHttpModule
    {
        /// <summary>
        /// HTTP1.1 Authorization header
        /// </summary>
        public const string HttpAuthorizationHeader = "Authorization";

        /// <summary>
        /// HTTP1.1 Basic Challenge Scheme Name
        /// </summary>
        public const string HttpBasicSchemeName = "Basic"; //

        /// <summary>
        /// HTTP1.1 Credential username and password separator
        /// </summary>
        public const char HttpCredentialSeparator = ':';

        /// <summary>
        /// HTTP1.1 Not authorized response status code
        /// </summary>
        public const int HttpNotAuthorizedStatusCode = 401;

        /// <summary>
        /// HTTP1.1 Basic Challenge Scheme Name
        /// </summary>
        public const string HttpWwwAuthenticateHeader = "WWW-Authenticate";

        /// <summary>
        /// The name of cookie that is sent to client
        /// </summary>
        public const string AuthenticationCookieName = "BasicAuthentication";

        /// <summary>
        /// HTTP.1.1 Basic Challenge Realm
        /// </summary>
        public const string Realm = "demo";

        /// <summary>
        /// The App Setting that can enable/disable the entire functionality
        /// </summary>
        public const string EnabledAppSetting = "BasicAuthEnabled";

        /// <summary>
        /// The credentials that are allowed to access the site.
        /// </summary>
        private IDictionary<string, string> activeUsers;

        /// <summary>
        /// Exclude configuration - request URL is matched to dictionary key and request method is matched to the value of the same key-value pair.
        /// </summary>
        private IDictionary<Regex, Regex> excludes;

        /// <summary>
        /// Indicates whether redirects are allowed without authentication.
        /// </summary>
        private bool allowRedirects;

        /// <summary>
        /// Indicates whether local requests are allowed without authentication.
        /// </summary>
        private bool allowLocal;

        /// <summary>
        /// Regular expression that matches any given string.
        /// </summary>
        private readonly static Regex AllowAnyRegex = new Regex(".*", RegexOptions.Compiled);

        /// <summary>
        /// Boolean that allows an app setting to enable/disable basic auth
        /// </summary>
        private bool isEnabled = true;

        /// <summary>
        /// Dictionary that caches whether basic authentication challenge should be sent. Key is request URL + request method, value indicates whether
        /// challenge should be sent.
        /// </summary>
        private static IDictionary<string, bool> shouldChallengeCache = new Dictionary<string, bool>();

        public void AuthenticateUser(object source, EventArgs e)
        {
            var httpApp = (HttpApplication)source;
            var context = httpApp.Context;

            string authorizationHeader = context.Request.Headers[HttpAuthorizationHeader];

            // Extract the basic authentication credentials from the request
            string userName = null;
            string password = null;
            if (!ExtractBasicCredentials(authorizationHeader, ref userName, ref password))
            {
                httpApp.CompleteRequest();
                return;
            }

            // Validate the user credentials
            if (!ValidateCredentials(userName, password))
            {
                httpApp.CompleteRequest();
                return;
            }

            // Add Header with same name as Cookie
            if (context.Request.Headers.AllKeys.FirstOrDefault(x => x.Equals(AuthenticationCookieName)) == null)
            {
                context.Request.Headers.Add(AuthenticationCookieName, userName);
            }

            // check whether cookie is set and send it to client if needed
            var authCookie = context.Request.Cookies.Get(AuthenticationCookieName);
            if (authCookie == null)
            {
                authCookie = new HttpCookie(AuthenticationCookieName, "1") { Expires = DateTime.Now.AddHours(1) };
                context.Response.Cookies.Add(authCookie);
            }
        }

        public void IssueAuthenticationChallenge(object source, EventArgs e)
        {
            var context = ((HttpApplication)source).Context;

            if (allowLocal && context.Request.IsLocal)
            {
                return;
            }

            if (allowRedirects && IsRedirect(context.Response.StatusCode))
            {
                return;
            }

            if (ShouldChallenge(context))
            {
                // if authentication cookie is not set issue a basic challenge
                var authCookie = context.Request.Cookies.Get(AuthenticationCookieName);
                if (authCookie == null)
                {
                    //make sure that user is not authencated yet
                    if (!context.Response.Cookies.AllKeys.Contains(AuthenticationCookieName))
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = HttpNotAuthorizedStatusCode;
                        context.Response.AddHeader(HttpWwwAuthenticateHeader, "Basic realm =\"" + Realm + "\"");
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if authentication challenge should be sent to client based on configured exclude rules
        /// </summary>
        private bool ShouldChallenge(HttpContext context)
        {
            // allow app setting take precedence
            if (!isEnabled)
            {
                return false;
            }

            // first check cache
            var key = string.Concat(context.Request.Path, context.Request.HttpMethod);
            if (shouldChallengeCache.ContainsKey(key))
            {
                return shouldChallengeCache[key];
            }

            // if value is not found in cache check exclude rules
            foreach (var urlVerbRegex in excludes)
            {
                if ((urlVerbRegex.Key.IsMatch(context.Request.Path) || urlVerbRegex.Key.IsMatch(context.Request.Url.Host)) && urlVerbRegex.Value.IsMatch(context.Request.HttpMethod))
                {
                    shouldChallengeCache[key] = false;

                    return false;
                }
            }

            shouldChallengeCache[key] = true;
            return true;
        }

        private static bool IsRedirect(int httpStatusCode)
        {
            return httpStatusCode == (int)HttpStatusCode.MovedPermanently ||
                   httpStatusCode == (int)HttpStatusCode.Redirect ||
                   httpStatusCode == (int)HttpStatusCode.TemporaryRedirect;
        }

        protected virtual bool ValidateCredentials(string userName, string password)
        {
            var lowerCaseUserName = userName.ToLower();

            if (activeUsers.ContainsKey(lowerCaseUserName) && activeUsers[lowerCaseUserName] == password)
            {
                return true;
            }

            return false;
        }

        protected virtual bool ExtractBasicCredentials(string authorizationHeader, ref string username, ref string password)
        {
            if (string.IsNullOrEmpty(authorizationHeader))
            {
                return false;
            }

            string verifiedAuthorizationHeader = authorizationHeader.Trim();
            if (verifiedAuthorizationHeader.IndexOf(HttpBasicSchemeName, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                return false;
            }

            // get the credential payload
            verifiedAuthorizationHeader = verifiedAuthorizationHeader.Substring(HttpBasicSchemeName.Length, verifiedAuthorizationHeader.Length - HttpBasicSchemeName.Length).Trim();
            // decode the base 64 encoded credential payload
            byte[] credentialBase64DecodedArray = Convert.FromBase64String(verifiedAuthorizationHeader);
            string decodedAuthorizationHeader = Encoding.UTF8.GetString(credentialBase64DecodedArray, 0, credentialBase64DecodedArray.Length);

            // get the username, password, and realm
            int separatorPosition = decodedAuthorizationHeader.IndexOf(HttpCredentialSeparator);

            if (separatorPosition <= 0)
            {
                return false;
            }

            username = decodedAuthorizationHeader.Substring(0, separatorPosition).Trim();
            password = decodedAuthorizationHeader.Substring(separatorPosition + 1, (decodedAuthorizationHeader.Length - separatorPosition - 1)).Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            return true;
        }

        public void Init(HttpApplication context)
        {
            var config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~/web.config");
            var basicAuth = TraverseConfigSections<Configuration.BasicAuthenticationConfigurationSection>(config.RootSectionGroup);

            if (basicAuth == null)
            {
                System.Diagnostics.Debug.WriteLine("BasicAuthenticationModule not started - Configuration not found. Make sure that BasicAuthenticationConfigurationSection section is defined.");
                return;
            }

            allowRedirects = basicAuth.AllowRedirects;
            allowLocal = basicAuth.AllowLocal;

            var appSetting = ConfigurationManager.AppSettings[EnabledAppSetting];
            if (appSetting != null)
            {
                isEnabled = appSetting == "true";
            }

            InitCredentials(basicAuth);
            InitExcludes(basicAuth);

            // Subscribe to the authenticate event to perform the authentication.
            context.AuthenticateRequest += AuthenticateUser;

            // Subscribe to the EndRequest event to issue the authentication challenge if necessary.
            context.EndRequest += IssueAuthenticationChallenge;
        }

        private void InitCredentials(Configuration.BasicAuthenticationConfigurationSection basicAuth)
        {
            activeUsers = new Dictionary<string, string>();

            for (int i = 0; i < basicAuth.Credentials.Count; i++)
            {
                var credential = basicAuth.Credentials[i];
                activeUsers.Add(credential.UserName.ToLower(), credential.Password);
            }
        }

        private void InitExcludes(Configuration.BasicAuthenticationConfigurationSection basicAuth)
        {
            var excludesAsString = new Dictionary<string, string>();
            excludes = new Dictionary<Regex, Regex>();
            var allowAnyRegex = AllowAnyRegex.ToString();

            for (int i = 0; i < basicAuth.Excludes.Count; i++)
            {
                var excludeUrl = basicAuth.Excludes[i].Url;
                var excludeVerb = basicAuth.Excludes[i].Verb;

                if (string.IsNullOrEmpty(excludeUrl))
                {
                    excludeUrl = allowAnyRegex;
                }
                if (string.IsNullOrEmpty(excludeVerb))
                {
                    excludeVerb = allowAnyRegex;
                }

                excludesAsString[excludeUrl] = excludeVerb;
            }

            foreach (var url in excludesAsString.Keys)
            {
                var urlRegex = url == allowAnyRegex ? AllowAnyRegex : new Regex(url, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var verbRegex = excludesAsString[url] == allowAnyRegex ? AllowAnyRegex : new Regex(excludesAsString[url], RegexOptions.Compiled | RegexOptions.IgnoreCase);

                excludes[urlRegex] = verbRegex;
            }
        }

        private T TraverseConfigSections<T>(ConfigurationSectionGroup group) where T : ConfigurationSection
        {
            foreach (ConfigurationSection section in group.Sections)
            {
                if (Type.GetType(section.SectionInformation.Type, false) == typeof(T))
                    return (T)section;
            }

            foreach (ConfigurationSectionGroup g in group.SectionGroups)
            {
                var section = TraverseConfigSections<T>(g);
                if (section != null)
                {
                    return section;
                }
            }

            return null;
        }

        public void Dispose()
        {
            // Do nothing here
        }
    }
}