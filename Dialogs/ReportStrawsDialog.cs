using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class ReportStrawsDialog : IDialog<string>
    {
        private string _establishment;
        private const string PlasticStraws = "Plastic straws :(";
        private const string NoPlasticStraws = "No plastic straws :D";

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("Ok, What establishment would you like to tell us about?");
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            _establishment = message.Text;

            PromptDialog.Choice(context, AfterReportAsync, new List<string> {PlasticStraws, NoPlasticStraws},
                $"Great! Does \"{_establishment}\" have:");
        }

        public async Task AfterReportAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var choice = await argument;
            switch (choice)
            {
                case PlasticStraws:
                    await context.PostAsync(
                        "That's sad to hear. We have recorded this so that other people can avoid this establishment");
                    break;
                case NoPlasticStraws:
                    await context.PostAsync(
                        "Great! We will tell people that they can come here to support an establishment that cares about our environment");
                    break;
                default:
                    await context.PostAsync("Sorry, I didn't understand");
                    break;
            }

            context.Done("");
        }
    }
}