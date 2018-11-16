// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace AuthBot.Helpers
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using Models;
    using Microsoft.Identity.Client;

    public static class AzureActiveDirectoryHelper
    {
        public static async Task<string> GetAuthUrlAsync(ResumptionCookie resumptionCookie, string resourceId)
        {
            var extraParameters = BuildExtraParameters(resumptionCookie);

            Uri redirectUri = new Uri(AuthSettings.RedirectUrl);
                Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(AuthSettings.EndpointUrl + "/" + AuthSettings.Tenant);
                var uri = await context.GetAuthorizationRequestUrlAsync(
                    resourceId,
                    AuthSettings.ClientId,
                    redirectUri,
                    Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier.AnyUser,
                    $"state={extraParameters}");
                return uri.ToString();       
        }

        public static async Task<string> GetAuthUrlAsync(ResumptionCookie resumptionCookie, string[] scopes)
        {
            var extraParameters = BuildExtraParameters(resumptionCookie);
            Uri redirectUri = new Uri(AuthSettings.RedirectUrl);
            if (string.Equals(AuthSettings.Mode, "v2", StringComparison.OrdinalIgnoreCase))
            {
                InMemoryTokenCacheMSAL tokenCache = new InMemoryTokenCacheMSAL();
                Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication("https://login.microsoftonline.com/" + AuthSettings.Tenant + "/oauth2/v2.0",
                    AuthSettings.ClientId, redirectUri.ToString(),
                    new Microsoft.Identity.Client.ClientCredential(AuthSettings.ClientSecret),
                    tokenCache);


                //var uri = "https://login.microsoftonline.com/" + AuthSettings.Tenant + "/oauth2/v2.0/authorize?response_type=code" +
                //    "&client_id=" + AuthSettings.ClientId +
                //    "&client_secret=" + AuthSettings.ClientSecret +
                //    "&redirect_uri=" + HttpUtility.UrlEncode(AuthSettings.RedirectUrl) +
                //    "&scope=" + HttpUtility.UrlEncode("openid profile " + string.Join(" ", scopes)) +
                //    "&state=" + encodedCookie;


                var uri = await client.GetAuthorizationRequestUrlAsync(
                   scopes,
                    null,
                    $"state={extraParameters}");
                return uri.ToString();
            }
            else if (string.Equals(AuthSettings.Mode, "b2c", StringComparison.OrdinalIgnoreCase))
            {
                InMemoryTokenCacheMSAL tokenCache = new InMemoryTokenCacheMSAL();
                Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication("https://login.microsoftonline.com/" + AuthSettings.Tenant + "/oauth2/v2.0",
                    AuthSettings.ClientId, redirectUri.ToString(),
                    new Microsoft.Identity.Client.ClientCredential(AuthSettings.ClientSecret),
                    tokenCache);


                var uri = "https://login.microsoftonline.com/" + AuthSettings.Tenant + "/oauth2/v2.0/authorize?response_type=code&&response_mode=query" +
                    "&client_id=" + AuthSettings.ClientId +
                    "&p=" + AuthSettings.Policy +
                    "&redirect_uri=" + HttpUtility.UrlEncode(AuthSettings.RedirectUrl) +
                    "&scope=" + HttpUtility.UrlEncode("openid profile offline_access") +
                    "&state=" + extraParameters;


            
                return uri.ToString();
            }
            return null;
        }

        public static async Task<AuthResult> GetTokenByAuthCodeAsync(string authorizationCode, Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache tokenCache)
        {
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(AuthSettings.EndpointUrl + "/" + AuthSettings.Tenant, tokenCache);
            Uri redirectUri = new Uri(AuthSettings.RedirectUrl);
            var result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(AuthSettings.ClientId, AuthSettings.ClientSecret));
            AuthResult authResult = AuthResult.FromADALAuthenticationResult(result, tokenCache);
            return authResult;
        }
        public static async Task<AuthResult> GetTokenByAuthCodeAsync(string authorizationCode, Microsoft.Identity.Client.TokenCache tokenCache, string[] scopes)
        {
            Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication(AuthSettings.ClientId, AuthSettings.RedirectUrl, new Microsoft.Identity.Client.ClientCredential(AuthSettings.ClientSecret), tokenCache);            
            Uri redirectUri = new Uri(AuthSettings.RedirectUrl);
            var result = await client.AcquireTokenByAuthorizationCodeAsync(scopes, authorizationCode);
            AuthResult authResult = AuthResult.FromMSALAuthenticationResult(result, tokenCache);
            return authResult;
        }

        public static async Task<AuthResult> GetB2cTokenByAuthCodeAsync(string authorizationCode, Microsoft.Identity.Client.TokenCache tokenCache, string[] scopes)
        {
            Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication("https://login.microsoftonline.com/" + AuthSettings.Tenant + "/oauth2/v2.0", AuthSettings.ClientId, AuthSettings.RedirectUrl, new Microsoft.Identity.Client.ClientCredential(AuthSettings.ClientSecret), tokenCache);
            Uri redirectUri = new Uri(AuthSettings.RedirectUrl);
            var result = await client.AcquireTokenByAuthorizationCodeAsync(scopes, authorizationCode,AuthSettings.Policy);
            AuthResult authResult = AuthResult.FromMSALAuthenticationResult(result, tokenCache);
            return authResult;
        }

        public static async Task<AuthResult> GetToken(string userUniqueId, Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache tokenCache, string resourceId)
        {
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(AuthSettings.EndpointUrl + "/" + AuthSettings.Tenant, tokenCache);
            var result = await context.AcquireTokenSilentAsync(resourceId, new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(AuthSettings.ClientId, AuthSettings.ClientSecret), new Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier(userUniqueId, Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifierType.UniqueId));
            AuthResult authResult = AuthResult.FromADALAuthenticationResult(result, tokenCache);
            return authResult;
        }

        public static async Task<AuthResult> GetToken(string userUniqueId, Microsoft.Identity.Client.TokenCache tokenCache, string[] scopes)
        {
            Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication(AuthSettings.ClientId, AuthSettings.RedirectUrl, new Microsoft.Identity.Client.ClientCredential(AuthSettings.ClientSecret), tokenCache);
            var result = await client.AcquireTokenSilentAsync(scopes, userUniqueId);
            AuthResult authResult = AuthResult.FromMSALAuthenticationResult(result, tokenCache);
            return authResult;
        }
        public static async Task<AuthResult> GetB2CToken(string userUniqueId, Microsoft.Identity.Client.TokenCache tokenCache, string[] scopes)
        {
            Microsoft.Identity.Client.ConfidentialClientApplication client = new Microsoft.Identity.Client.ConfidentialClientApplication("https://login.microsoftonline.com/" + AuthSettings.Tenant + "/oauth2/v2.0",AuthSettings.ClientId, AuthSettings.RedirectUrl, new Microsoft.Identity.Client.ClientCredential(AuthSettings.ClientSecret), tokenCache);
            var result = await client.AcquireTokenSilentAsync(scopes, userUniqueId, "https://login.microsoftonline.com/" + AuthSettings.Tenant + "/oauth2/v2.0",AuthSettings.Policy,false);
            AuthResult authResult = AuthResult.FromMSALAuthenticationResult(result, tokenCache);
            return authResult;
        }

        public static string TokenEncoder(string token)
        {
            return HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(token));
        }

        public static string TokenDecoder(string token)
        {
            return Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(token));
        }

        private static string BuildExtraParameters(ResumptionCookie resumptionCookie)
        {
            var encodedCookie = UrlToken.Encode(resumptionCookie);

            //var queryString = HttpUtility.ParseQueryString(string.Empty);
            //queryString["userId"] = resumptionCookie.Address.UserId;
            //queryString["botId"] = resumptionCookie.Address.BotId;
            //queryString["conversationId"] = resumptionCookie.Address.ConversationId;
            //queryString["serviceUrl"] = resumptionCookie.Address.ServiceUrl;
            //queryString["channelId"] = resumptionCookie.Address.ChannelId;
            //queryString["locale"] = resumptionCookie.Locale ?? "en";

            //return TokenEncoder(queryString.ToString());
            return encodedCookie;
        }
    }
}

//*********************************************************
//
//AuthBot, https://github.com/microsoftdx/AuthBot
//
//Copyright (c) Microsoft Corporation
//All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// ""Software""), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:




// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.




// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//*********************************************************
