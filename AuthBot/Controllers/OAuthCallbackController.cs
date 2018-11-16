// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace AuthBot.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using Autofac;
    using Helpers;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Microsoft.Bot.Connector;
    using Microsoft.Rest;
    using Models;

    public class OAuthCallbackController : ApiController
    {
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private static readonly uint MaxWriteAttempts = 5;

        [HttpGet]
        [Route("api/OAuthCallback")]
        public async Task<HttpResponseMessage> OAuthCallback()
        {
            try
            {
               
                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    resp.Content = new StringContent($"<html><body>You have been signed out. You can now close this window.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    return resp;
               
            }
            catch (Exception ex)
            {
                // Callback is called with no pending message as a result the login flow cannot be resumed.
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }

        }
        [HttpGet]
        [Route("api/OAuthCallback")]
        public async Task<HttpResponseMessage> OAuthCallback(
            [FromUri] string code, 
            [FromUri] string state, 
            CancellationToken cancellationToken)
        {
            try
            {

                var queryParams = state;
                object tokenCache = null;
                if (string.Equals(AuthSettings.Mode, "v1", StringComparison.OrdinalIgnoreCase))
                {
                    tokenCache = new Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache();
                }
                else if (string.Equals(AuthSettings.Mode, "v2", StringComparison.OrdinalIgnoreCase))
                {
                    tokenCache = new Microsoft.Identity.Client.TokenCache();
                }
                else if (string.Equals(AuthSettings.Mode, "b2c", StringComparison.OrdinalIgnoreCase))
                {
                }

                var resumptionCookie = UrlToken.Decode<ResumptionCookie>(queryParams);
                // Create the message that is send to conversation to resume the login flow
                var message = resumptionCookie.GetMessage();
               
                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                {
                    var client = scope.Resolve<IConnectorClient>();                
                    AuthResult authResult = null;
                    if (string.Equals(AuthSettings.Mode, "v1", StringComparison.OrdinalIgnoreCase))
                    {
                        // Exchange the Auth code with Access token
                        var token = await AzureActiveDirectoryHelper.GetTokenByAuthCodeAsync(code, (Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache)tokenCache);
                        authResult = token;
                    }
                    else if (string.Equals(AuthSettings.Mode, "v2", StringComparison.OrdinalIgnoreCase))
                    {
                        // Exchange the Auth code with Access token
                        var token = await AzureActiveDirectoryHelper.GetTokenByAuthCodeAsync(code, (Microsoft.Identity.Client.TokenCache)tokenCache,Models.AuthSettings.Scopes);
                        authResult = token;
                    }
                    else if (string.Equals(AuthSettings.Mode, "b2c", StringComparison.OrdinalIgnoreCase))
                    {
                    }
                    
                    IStateClient sc = scope.Resolve<IStateClient>();

                    //IMPORTANT: DO NOT REMOVE THE MAGIC NUMBER CHECK THAT WE DO HERE. THIS IS AN ABSOLUTE SECURITY REQUIREMENT
                    //REMOVING THIS WILL REMOVE YOUR BOT AND YOUR USERS TO SECURITY VULNERABILITIES. 
                    //MAKE SURE YOU UNDERSTAND THE ATTACK VECTORS AND WHY THIS IS IN PLACE.
                    int magicNumber = GenerateRandomNumber();
                    bool writeSuccessful = false;
                    uint writeAttempts = 0;
                    while (!writeSuccessful && writeAttempts++ < MaxWriteAttempts)
                    {
                        try
                        {
                            BotData userData = sc.BotState.GetUserData(message.ChannelId, message.From.Id);
                            userData.SetProperty(ContextConstants.AuthResultKey, authResult);
                            userData.SetProperty(ContextConstants.MagicNumberKey, magicNumber);
                            userData.SetProperty(ContextConstants.MagicNumberValidated, "false");
                            sc.BotState.SetUserData(message.ChannelId, message.From.Id, userData);
                            writeSuccessful = true;
                        }
                        catch (HttpOperationException)
                        {
                            writeSuccessful = false;
                        }
                    }
                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    if (!writeSuccessful)
                    {
                        message.Text = String.Empty; // fail the login process if we can't write UserData
                        await Conversation.ResumeAsync(resumptionCookie, message);
                        resp.Content = new StringContent("<html><body>Could not log you in at this time, please try again later</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    }
                    else
                    {
                        await Conversation.ResumeAsync(resumptionCookie, message);
                        if (message.ChannelId == "skypeforbusiness")
                            resp.Content = new StringContent($"<html><body>Almost done! Please copy this number and paste it back to your chat so your authentication can complete:<br/> {magicNumber} </body></html>", System.Text.Encoding.UTF8, @"text/html");
                        else if (message.ChannelId == "email")
                            resp.Content = new StringContent($"<html><body>Almost done! Please reply to the email you've received asking you to authenticate with this number so your authentication can complete:<br/> {magicNumber} </body></html>", System.Text.Encoding.UTF8, @"text/html");
                        else
                            resp.Content = new StringContent($"<html><body>Almost done! Please copy this number and paste it back to your chat so your authentication can complete:<br/> <h1>{magicNumber}</h1>.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    }
                    return resp;
                }
            }
            catch (Exception ex)
            {
                // Callback is called with no pending message as a result the login flow cannot be resumed.
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        private int GenerateRandomNumber()
        {
            int number = 0;
            byte[] randomNumber = new byte[1];
            do
            {
                rngCsp.GetBytes(randomNumber);
                var digit = randomNumber[0] % 10;
                number = number * 10 + digit;
            } while (number.ToString().Length < 6);
            return number;

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
