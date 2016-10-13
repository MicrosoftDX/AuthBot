// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace SampleVSOBot.Dialogs
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AuthBot;
    using AuthBot.Dialogs;
    using AuthBot.Models;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using System.Configuration;
    using Helpers;

    [Serializable]
    public class ActionDialog : IDialog<string>
    {

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }


        public async Task GetTokenInfo(IDialogContext context)
        {
            
            AuthResult authResult;
            if (context.UserData.TryGetValue(ContextConstants.AuthResultKey, out authResult))
            {
                DateTime expiredDate = new DateTime(authResult.ExpiresOnUtcTicks);
                String expireDateStr = expiredDate.ToString("d/M/yyyy HH:mm:ss");

                var expiresin = TimeSpan.FromTicks(authResult.ExpiresOnUtcTicks - DateTime.UtcNow.Ticks).TotalMinutes;
                if (expiresin < 0)
                {
                    await context.PostAsync($"Your access token already expired on {expireDateStr}");
                }
                else
                {
                    await context.PostAsync($"Your access token expires in {expiresin} minutes ({expireDateStr})");
                    await context.PostAsync($"Your access token is \"{authResult.AccessToken}\".");
                }

            }
            else
            { await context.PostAsync($"Please logon first"); }
            
            context.Wait(MessageReceivedAsync);
        }
        /// <summary>
        /// For Testing Purposes / capability to force token expiration
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task ForceTokenExpiration(IDialogContext context)
        {
            AuthResult authResult;
            if (context.UserData.TryGetValue(ContextConstants.AuthResultKey, out authResult))
            {
                authResult.ExpiresOnUtcTicks = 0;
                context.StoreAuthResult(authResult);
                await context.PostAsync($"Forced token to expire");
            }
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            if (message.Text.ToLower() == "logon" || message.Text.ToLower() == "login")
            {
                //endpoint v1
                if (string.IsNullOrEmpty(await context.GetAccessToken(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"])))
                {
                    await context.Forward(new AzureAuthDialog(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"]), this.ResumeAfterAuth, message, CancellationToken.None);
                }
                else
                {
                    await context.PostAsync("already logged in.");
                    context.Wait(MessageReceivedAsync);
                }
            }
            else if (message.Text.ToLower() == "echo")
            {
                await context.PostAsync("echo");

                context.Wait(this.MessageReceivedAsync);
            }
            else if (message.Text.ToLower() == "token")
            {
                await GetTokenInfo(context);               
            }
            else if (message.Text.ToLower() == "expire")
            {
                await ForceTokenExpiration(context);
            }
            else if (message.Text.ToLower() == "logout")
            {
                await context.Logout();
                context.Wait(this.MessageReceivedAsync);
            }
            else if (message.Text.ToLower() == "projects")
            {
                await GetVSOProjectList(context);
            }
            else
            {
                await context.PostAsync("say what ? Please say something like logon, logout, token, echo or projects");
                context.Wait(MessageReceivedAsync);
            }
        }


        public async Task GetVSOProjectList(IDialogContext context)
        {
            var accessToken = await context.GetAccessToken(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"]);

            try
            {   //todo: check if token has expired
                if (string.IsNullOrEmpty(accessToken))
                {
                    await context.PostAsync("Please logon first");
                    context.Wait(MessageReceivedAsync);
                }
                else
                {
                    //String witems = await VSORestHelper.GetWorkItems(accessToken);
                    //String witems = await VSORestHelper.QueryWorkItems_Query(accessToken);
                    //String witems = await VSORestHelper.QueryWorkItems_Wiql(accessToken);
                    String witems = await VSORestHelper.ListProjects(accessToken);
                    await context.PostAsync(witems);
                }
            }
            catch (Exception exc)
            { await context.PostAsync($"Error geting work items. Error:\n {exc.Message}"); }

            context.Wait(MessageReceivedAsync);
        }
        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;

            await context.PostAsync(message);
            context.Wait(MessageReceivedAsync);
        }
    }
}


//*********************************************************
//
//AuthBot, https://github.com/matvelloso/AuthBot
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
