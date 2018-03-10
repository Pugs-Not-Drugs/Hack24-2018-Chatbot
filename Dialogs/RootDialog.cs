using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;


namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        protected int Count = 1;

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync($"Welcome {context.Activity.From.Name}, I\'m ready to help you make Nottingham more eco-friendly. Talk to me about:" +
                                    "Impact of plastic straws on the environment, Reporting use of plastic straws in cafés/ bars/ restaurants etc.");
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            switch (message.Text)
            {
                case "hello":
                    PromptDialog.Choice(context, AfterMoreIntrestingAsync, new List<string> {"Learn about the impact of plastic straws on the environment", "Report an establishments straw policy"},
                        $"Hello there {context.Activity.From.Name}, would you like to:");
                    break;
                case "reset":
                    PromptDialog.Confirm(
                        context,
                        AfterResetAsync,
                        "Are you sure you want to reset the count?",
                        "Didn't get that!",
                        promptStyle: PromptStyle.Auto);
                    break;
                case "talk about something more intresting":
                    PromptDialog.Choice(context, AfterMoreIntrestingAsync, new List<string> {"a", "b", "c"},
                        "ok how about the alphabet?");
                    break;
                default:
                    await context.PostAsync($"{Count++}: You said {message.Text}");
                    context.Wait(MessageReceivedAsync);
                    break;
            }
        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.Count = 1;
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }

            context.Wait(MessageReceivedAsync);
        }

        public async Task AfterMoreIntrestingAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var choice = await argument;
            switch (choice)
            {
                case "Learn about the impact of plastic straws on the environment":
                    await context.PostAsync("Straws have a terrible impact on the environment https://www.isfoundation.com/infographic/final-straw-infographic");
                    break;
                case "Report an establishments straw policy":
                    await context.PostAsync("STRAWS!");
                    break;
                default:
                    await context.PostAsync("Sorry, I didn't understand");
                    PromptDialog.Choice(context, AfterMoreIntrestingAsync, new List<string> {"Learn about the impact of plastic straws on the environment", "Report an establishments straw policy"},
                        $"Would you like to:");
                    break;
            }

            context.Wait(MessageReceivedAsync);
        }
    }
}