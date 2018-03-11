using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using RestSharp;
using RestSharp.Serializers;
using SimpleEchoBot;


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

        private static List<BussinessResults> _bussinessResultses;

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync(
                $"Welcome {context.Activity.From.Name}, I\'m ready to help you make Nottingham more eco-friendly. Talk to me about: " +
                "Impact of plastic straws on the environment, Reporting use of plastic straws in cafés/ bars/ restaurants etc." +
                "\n Please visit our site at https://econotts.iamawizard.co.uk/ to learn more about helping the Nottingham community");
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            var messageText = await CogHelpers.RunThroughSpellCheck(message.Text);

            if (messageText.Contains("hello") || messageText.Contains("Hello") || messageText.Contains("hi") || messageText.Contains("Hi") || messageText.Contains("ok") || messageText.Contains("OK"))
            {
                PromptDialog.Choice(context, AfterMoreIntrestingAsync,
                    new List<string> {LearnAboutStraws, ReportAnEstablishmentsStrawPolicy},
                    $"Would you like to:");
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
                var environmentVariable = Environment.GetEnvironmentVariable("SPELL_CHECK");
                var spellcheckKey = string.IsNullOrEmpty(environmentVariable)
                    ? ConfigurationManager.AppSettings.Get("SPELL_CHECK")
                    : environmentVariable;

                var luienvironmentVariable = Environment.GetEnvironmentVariable("LUI_KEY");
                var luiKey = string.IsNullOrEmpty(luienvironmentVariable)
                    ? ConfigurationManager.AppSettings.Get("LUI_KEY")
                    : luienvironmentVariable;

                var client = new HttpClient();
                var uri =
                    $"https://westeurope.api.cognitive.microsoft.com/luis/v2.0/apps/ebcd8903-9d89-4423-8874-0f43d03af753?subscription-key={luiKey}&spellCheck=true&bing-spell-check-subscription-key={spellcheckKey}&verbose=true&timezoneOffset=0&q={messageText}";

                var httpResponseMessage = await client.GetAsync(uri);
                var contentString = await httpResponseMessage.Content.ReadAsStringAsync();
                var deserializeObject = SimpleJson.SimpleJson.DeserializeObject<Something>(contentString);

                if (deserializeObject.topScoringIntent.score > 0.9)
                {
                    switch (deserializeObject.topScoringIntent.intent)
                    {
                        case "Straw Policy":
                            context.Call(new GetStrawsDialogs(deserializeObject), ResumeAfterNewOrderDialog);
                            break;
//                        case "Recycling":
//                            await DoRecyclingStuff(context, deserializeObject);
//                            break;
                        default:
                            await context.PostAsync(
                                "Sorry, I can't help with that. Try asking me about straws or recycling centres in the area!");
                            break;
                    }
                }

                context.Wait(MessageReceivedAsync);
            }
        }

        private async Task DoRecyclingStuff(IDialogContext context, Something deserializeObject)
        {
            var recyclingInfo = SimpleJson.SimpleJson.DeserializeObject<RecyclingInfo>(JsonHelper.RecyclingJson);
            var thing = deserializeObject.entities.First(e => e.type == "thing");

            if (thing != null)
            {
                switch (thing.entity)
                {
                    case "cans":
                        var recyclingCentres = recyclingInfo.recyclingCentres.Where(c => c.canTins == "Yes");
                        var names = recyclingCentres.Select(c => c.name).ToList();
                        await context.PostAsync($"The following centres recycle cans: " +
                                                string.Join(string.Empty, names.ToArray()));
                        break;
                }
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

    public class ResultFromApi
    {
        public string id { get; set; }
        public string name { get; set; }
        public string happyStraws { get; set; }
        public string sadStraws { get; set; }
    }

    public class RecyclingInfo
    {
        public List<RecyclingCentre> recyclingCentres;
    }

    public class RecyclingCentre
    {
        public string name { get; set; }
        public string address { get; set; }
        public string banktype { get; set; }
        public string clearGlass { get; set; }
        public string greenGlass { get; set; }
        public string brownGlass { get; set; }
        public string mixed { get; set; }
        public string canTins { get; set; }
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