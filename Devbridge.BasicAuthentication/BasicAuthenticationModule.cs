using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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

        private IDictionary<string, string> activeUsers;
        private IDictionary<string, List<string>> excludesVerb;
        private IDictionary<string, List<string>> excludesUrl;

        private bool allowRedirects;

        public void AuthenticateUser(Object source, EventArgs e)
        {
            var context = ((HttpApplication)source).Context;

            string authorizationHeader = context.Request.Headers[HttpAuthorizationHeader];

            // Extract the basic authentication credentials from the request
            string userName = null;
            string password = null;
            if (!this.ExtractBasicCredentials(authorizationHeader, ref userName, ref password))
            {
                return;
            }

            // Validate the user credentials
            if (!this.ValidateCredentials(userName, password))
            {
                return;
            }

            // check whether cookie is set and send it to client if needed
            var authCookie = context.Request.Cookies.Get(AuthenticationCookieName);
            if (authCookie == null)
            {
                authCookie = new HttpCookie(AuthenticationCookieName, "1") { Expires = DateTime.Now };
                context.Response.Cookies.Add(authCookie);
            }
        }

        public void IssueAuthenticationChallenge(Object source, EventArgs e)
        {
            var context = ((HttpApplication)source).Context;

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

        private bool ShouldChallenge(HttpContext context)
        {
            if (excludesVerb.ContainsKey(context.Request.HttpMethod.ToUpper())) 
            {
                var urls = excludesVerb[context.Request.HttpMethod.ToUpper()];
                if (urls.Contains(""))
                {
                    //if the empty URL is in the list, the config means that we should bypass challenge for this verb
                    //for every URL
                    return false;
                }
                else
                {
                    if (urls.Contains(context.Request.Path.ToUpper())) {
                        return false;
                    }
                }
            }

            if (excludesUrl.ContainsKey(context.Request.Path.ToUpper()))
            {
                var verbs = excludesUrl[context.Request.Path.ToUpper()];
                if (verbs.Contains(""))
                {
                    //if the empty verb is in the list, the config means that we should bypass challenge for this url
                    //for every verb
                    return false;
                }
                else
                {
                    if (verbs.Contains(context.Request.HttpMethod.ToUpper()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsRedirect(int httpStatusCode)
        {
            return new[]
            {
                HttpStatusCode.MovedPermanently,
                HttpStatusCode.Redirect,
                HttpStatusCode.TemporaryRedirect
            }.Any(c => (int)c == httpStatusCode);
        }

        protected virtual bool ValidateCredentials(string userName, string password)
        {
            if (activeUsers.ContainsKey(userName) && activeUsers[userName] == password)
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
            var config = System.Configuration.ConfigurationManager.GetSection("basicAuth");
            var basicAuth = (Configuration.BasicAuthenticationConfigurationSection)config;

            this.allowRedirects = basicAuth.AllowRedirects;
            InitCredentials(basicAuth);
            InitExcludes(basicAuth);

            // Subscribe to the authenticate event to perform the authentication.
            context.AuthenticateRequest += AuthenticateUser;

            // Subscribe to the EndRequest event to issue the authentication challenge if necessary.
            context.EndRequest += IssueAuthenticationChallenge;
        }

        private void InitCredentials(Configuration.BasicAuthenticationConfigurationSection basicAuth)
        {
            this.activeUsers = new Dictionary<string, string>();

            for (int i = 0; i < basicAuth.Credentials.Count; i++)
            {
                var credential = basicAuth.Credentials[i];
                this.activeUsers.Add(credential.UserName, credential.Password);
            }
        }

        private void InitExcludes(Configuration.BasicAuthenticationConfigurationSection basicAuth)
        {
            this.excludesUrl = new Dictionary<string, List<string>>();
            this.excludesVerb = new Dictionary<string, List<string>>();

            for (int i = 0; i < basicAuth.ExcludeUrls.Count; i++)
            {
                var excludeUrl = basicAuth.ExcludeUrls[i];

                if (!String.IsNullOrEmpty(excludeUrl.Url)) {
                    if (excludesUrl.ContainsKey(excludeUrl.Url.ToUpper()))
                    {
                        excludesUrl[excludeUrl.Url.ToUpper()].Add(excludeUrl.Verb);
                    }
                    else
                    {
                        excludesUrl.Add(excludeUrl.Url.ToUpper(), new List<string> { excludeUrl.Verb });
                    }
                }                    
            }

            for (int i = 0; i < basicAuth.ExcludeVerbs.Count; i++)
            {
                var excludeVerb = basicAuth.ExcludeVerbs[i];

                if (!String.IsNullOrEmpty(excludeVerb.Verb))
                {
                    if (excludesVerb.ContainsKey(excludeVerb.Verb.ToUpper()))
                    {
                        excludesVerb[excludeVerb.Verb.ToUpper()].Add(excludeVerb.Url);
                    }
                    else
                    {
                        excludesVerb.Add(excludeVerb.Verb.ToUpper(), new List<string> { excludeVerb.Url });
                    }
                }
            }
        }

        public void Dispose()
        {
            // Do nothing here
        }
    }
}