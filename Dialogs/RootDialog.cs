using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;


namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private static readonly Random RandomGen = new Random();

        private const string LearnAboutStraws = "Tell me about plastic straws";
        private const string ReportAnEstablishmentsStrawPolicy = "Report an establishments straw policy";

        private static readonly List<string> StrawFacts = new List<string>
        {
            "Plastic straws are among the top 10 plastic debris found during coastal cleanups",
            "An estimated 100,000 marine animals are killed every year due to plastic debris",
            "80 - 90% of the debris that ends up in the ocean is plastic",
            "More straws means increased plastic production requiring more oil and gas is needed for energy for manufacturing and distribution. This means more carbon emissions and pollution",
            "Reusable straws are an earth friendly option! If you want to use straws, why not carry your own reusable one? They come in all colours and even with cute characters for your kids",
            "Alternative materials to plastic for straws include bamboo, metal, glass, straw or paper",
            "Plastic straws may be small but are a big concern - Their size makes it hard to pick them out for recycling. They also easily find their way into bodies of water. Moreover, marine animals mistake straws for food.",
            "The amount of marine litter found on UK beaches has more than doubled in the last 15 years."
        };

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

            var messageText = await RunThroughSpellCheck(message.Text);

            if (messageText == "hello")
            {
                PromptDialog.Choice(context, AfterMoreIntrestingAsync,
                    new List<string> {LearnAboutStraws, ReportAnEstablishmentsStrawPolicy},
                    $"Hello there {context.Activity.From.Name}, would you like to:");
            }
            else if (messageText.Contains("report") && messageText.Contains("straw"))
            {
                context.Call(new ReportStrawsDialog(), ResumeAfterNewOrderDialog);
            }
            else if (messageText.Contains("info") || messageText.Contains("about") && message.Text.Contains("straw"))
            {
                await TeachAboutStraws(context);
            }
            else
            {
                context.Wait(MessageReceivedAsync);
            }
        }

        public static async Task<string> RunThroughSpellCheck(string messageText)
        {
            var environmentVariable = Environment.GetEnvironmentVariable("SPELL_CHECK");
            var spellcheckKey = string.IsNullOrEmpty(environmentVariable)
                ? ConfigurationManager.AppSettings.Get("SPELL_CHECK")
                : environmentVariable;

            string host = "https://api.cognitive.microsoft.com";
            string path = "/bing/v7.0/spellcheck?";

            
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", spellcheckKey);

            // The following headers are optional, but it is recommended they be treated as required.
            // These headers help the service return more accurate results.
            //client.DefaultRequestHeaders.Add("X-Search-Location", ClientLocation);
            //client.DefaultRequestHeaders.Add("X-MSEdge-ClientID", ClientId);
            //client.DefaultRequestHeaders.Add("X-MSEdge-ClientIP", ClientIp);

            HttpResponseMessage response = new HttpResponseMessage();
            string uri = host + path;

            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("mkt", "en-GB"));
            values.Add(new KeyValuePair<string, string>("mode", "proof"));
            values.Add(new KeyValuePair<string, string>("text", messageText));

            using (FormUrlEncodedContent content = new FormUrlEncodedContent(values))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                response = await client.PostAsync(uri, content);
            }

            var contentString = await response.Content.ReadAsStringAsync();
            var deserializeObject = SimpleJson.SimpleJson.DeserializeObject<SpellCheck>(contentString);
            deserializeObject.flaggedTokens.ForEach(t => messageText = messageText.Replace(t.token, t.suggestions.First().suggestion));
            return messageText;
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
            var next = RandomGen.Next(0, StrawFacts.Count);
            var strawFact = StrawFacts[next];
            await context.PostAsync(strawFact);
        }

        private async Task ResumeAfterNewOrderDialog(IDialogContext context, IAwaitable<string> result)
        {
            PromptDialog.Choice(context, AfterMoreIntrestingAsync,
                new List<string> {LearnAboutStraws, ReportAnEstablishmentsStrawPolicy},
                "Can I help with with anything else?");
        }
    }

    public class SpellCheck
    {
        public List<FlaggedTokens> flaggedTokens { get; set; }
    }

    public class FlaggedTokens
    {
        public string token { get; set; }
        public List<Suggestion> suggestions { get; set; }
    }

    public class Suggestion
    {
        public string suggestion { get; set; }
    }
}