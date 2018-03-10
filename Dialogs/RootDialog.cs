using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;


namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private const string LearnAboutStraws = "Learn about the impact of plastic straws on the environment";
        private const string ReportAnEstablishmentsStrawPolicy = "Report an establishments straw policy";

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync(
                $"Welcome {context.Activity.From.Name}, I\'m ready to help you make Nottingham more eco-friendly. Talk to me about:" +
                "Impact of plastic straws on the environment, Reporting use of plastic straws in cafés/ bars/ restaurants etc.");
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            if (message.Text == "hello")
            {
                PromptDialog.Choice(context, AfterMoreIntrestingAsync,
                    new List<string> {LearnAboutStraws, ReportAnEstablishmentsStrawPolicy},
                    $"Hello there {context.Activity.From.Name}, would you like to:");
            }
            else if(message.Text.Contains("report") && message.Text.Contains("straw"))
            {
                context.Call(new ReportStrawsDialog(), ResumeAfterNewOrderDialog);
            }
            else if (message.Text.Contains("info") || message.Text.Contains("about") && message.Text.Contains("straw"))
            {
                await TeachAboutStraws(context);
            }
            else
            {
                context.Wait(MessageReceivedAsync);
            }
        }

        public async Task AfterMoreIntrestingAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var choice = await argument;
            switch (choice)
            {
                case LearnAboutStraws:
                    await TeachAboutStraws(context);
                    context.Wait(MessageReceivedAsync);
                    break;
                case ReportAnEstablishmentsStrawPolicy:
                    context.Call(new ReportStrawsDialog(), ResumeAfterNewOrderDialog);
                    break;
                default:
                    await context.PostAsync("Sorry, I didn't understand");
                    PromptDialog.Choice(context, AfterMoreIntrestingAsync,
                        new List<string> {LearnAboutStraws, ReportAnEstablishmentsStrawPolicy},
                        $"Would you like to:");
                    break;
            }
        }

        private static async Task TeachAboutStraws(IDialogContext context)
        {
            await context.PostAsync(
                "Straws have a terrible impact on the environment https://www.isfoundation.com/infographic/final-straw-infographic");
        }

        private async Task ResumeAfterNewOrderDialog(IDialogContext context, IAwaitable<string> result)
        {
            PromptDialog.Choice(context, AfterMoreIntrestingAsync,
                new List<string> {LearnAboutStraws, ReportAnEstablishmentsStrawPolicy},
                "Can I help with with anything else?");
        }
    }
}