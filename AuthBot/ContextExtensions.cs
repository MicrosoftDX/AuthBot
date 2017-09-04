// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace AuthBot
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Models;
    using System.Configuration;
    public static class ContextExtensions
    {
        public static async Task<string> GetAccessToken(this IBotContext context, string resourceId)
        {
            AuthResult authResult;
            if (context.UserData.TryGetValue(ContextConstants.AuthResultKey, out authResult))
            {
                try
                {
                    InMemoryTokenCacheADAL tokenCache = new InMemoryTokenCacheADAL(authResult.TokenCache);
                    var result = await AzureActiveDirectoryHelper.GetToken(authResult.UserUniqueId, tokenCache, resourceId);
                    authResult.AccessToken = result.AccessToken;
                    authResult.ExpiresOnUtcTicks = result.ExpiresOnUtcTicks;
                    authResult.TokenCache = tokenCache.Serialize();
                    context.StoreAuthResult(authResult);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Failed to renew token: " + ex.Message);
                    await context.PostAsync("Your credentials expired and could not be renewed automatically!");
                    await context.Logout();
                    return null;
                }
                return authResult.AccessToken;
            }
            return null;
        }
        public static async Task<string> GetAccessToken(this IBotContext context, string[] scopes)
        {
            AuthResult authResult;
            string validated = null;
            if (context.UserData.TryGetValue(ContextConstants.AuthResultKey, out authResult) &&
                context.UserData.TryGetValue(ContextConstants.MagicNumberValidated, out validated) &&
                validated == "true")
            { 

                    try
                    {
                    if (string.Equals(AuthSettings.Mode, "v2", StringComparison.OrdinalIgnoreCase))
                    {
                        InMemoryTokenCacheMSAL tokenCache = new InMemoryTokenCacheMSAL(authResult.TokenCache);
                        var result = await AzureActiveDirectoryHelper.GetToken(authResult.UserUniqueId, tokenCache, scopes);
                        authResult.AccessToken = result.AccessToken;
                        authResult.ExpiresOnUtcTicks = result.ExpiresOnUtcTicks;
                        authResult.TokenCache = tokenCache.Serialize();
                        context.StoreAuthResult(authResult);
                    }
                    else if (string.Equals(AuthSettings.Mode, "b2c", StringComparison.OrdinalIgnoreCase))
                    {
                        InMemoryTokenCacheMSAL tokenCache = new InMemoryTokenCacheMSAL(authResult.TokenCache);
                        var result = await AzureActiveDirectoryHelper.GetB2CToken(authResult.UserUniqueId, tokenCache, scopes);
                        authResult.AccessToken = result.AccessToken;
                        authResult.ExpiresOnUtcTicks = result.ExpiresOnUtcTicks;
                        authResult.TokenCache = tokenCache.Serialize();
                        context.StoreAuthResult(authResult);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Failed to renew token: " + ex.Message);
                    await context.PostAsync("Your credentials expired and could not be renewed automatically!");
                    await context.Logout();
                    return null;
                }
                return authResult.AccessToken;
            }

            return null;
        }

        public static void StoreAuthResult(this IBotContext context, AuthResult authResult)
        {
            context.UserData.SetValue(ContextConstants.AuthResultKey, authResult);
        }

        public static async Task Logout(this IBotContext context)
        {
            context.UserData.RemoveValue(ContextConstants.AuthResultKey);
            context.UserData.RemoveValue(ContextConstants.MagicNumberKey);
            context.UserData.RemoveValue(ContextConstants.MagicNumberValidated);
            string signoutURl = "https://login.microsoftonline.com/common/oauth2/logout?post_logout_redirect_uri=" + System.Net.WebUtility.UrlEncode(AuthSettings.RedirectUrl);
            await context.PostAsync($"In order to finish the sign out, please click at this [link]({signoutURl}).");
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
