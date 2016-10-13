// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See full license at the bottom of this file.
namespace SampleVSOBot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;

    public static class DialogExtensions
    {
        public static void NotifyLongRunningOperation<T>(this Task<T> operation, IDialogContext context, Func<T, string> handler)
        {
            operation.ContinueWith(
                async (t, ctx) =>
                {
                    var messageText = handler(t.Result);
                    await NotifyUser((IDialogContext)ctx, messageText);
                },
                context);
        }

        public static void NotifyLongRunningOperation<T>(this Task<T> operation, IDialogContext context, Func<T, IDialogContext, string> handler)
        {
            operation.ContinueWith(
                async (t, ctx) =>
                {
                    var messageText = handler(t.Result, (IDialogContext)ctx);
                    await NotifyUser((IDialogContext)ctx, messageText);
                },
                context);
        }

        public static string GetEntityOriginalText(this EntityRecommendation recommendation, string query)
        {
            if (recommendation.StartIndex.HasValue && recommendation.EndIndex.HasValue)
            {
                return query.Substring(recommendation.StartIndex.Value, recommendation.EndIndex.Value - recommendation.StartIndex.Value + 1);
            }

            return null;
        }

        public static async Task NotifyUser(this IDialogContext context, string messageText)
        {
            if (!string.IsNullOrEmpty(messageText))
            {
                await context.PostAsync(messageText);
            }
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
