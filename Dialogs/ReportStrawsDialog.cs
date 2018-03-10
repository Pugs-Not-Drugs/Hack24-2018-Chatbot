using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serializers;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class ReportStrawsDialog : IDialog<string>
    {
        private string _establishment;
        private List<BussinessResults> _places;
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
            
            var restClient = new RestClient("http://spatial.virtualearth.net/REST/v1/");
            var request = new RestRequest("/data/c2ae584bbccc4916a0acf75d1e6947b4/NavteqEU/NavteqPOIs",
                Method.GET)
            {
                RequestFormat = DataFormat.Json,
                JsonSerializer = new JsonSerializer()
            };
            request.AddQueryParameter("spatialFilter", "nearby('Nottingham',100)");
            request.AddQueryParameter("entityTypeName", "Business");
            request.AddQueryParameter("$format", "json");
            request.AddQueryParameter("$top", "5");
            
            var environmentVariable = Environment.GetEnvironmentVariable("BING_API_KEY");

            request.AddQueryParameter("key",
                string.IsNullOrEmpty(environmentVariable)
                    ? ConfigurationManager.AppSettings.Get("BING_API_KEY")
                    : environmentVariable);

            var restResponse = restClient.Execute<TheResponse>(request);

            _places = restResponse.Data.d.results;
            PromptDialog.Choice(context, SelectAsync,
                restResponse.Data.d.results.Select(b => b.Name).ToList(),
                "Which one of these did you mean?");
        }

        public async Task SelectAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var message = await argument;
            _establishment = message;

            PromptDialog.Choice(context, AfterReportAsync, new List<string> {PlasticStraws, NoPlasticStraws},
                $"Great! Does \"{_establishment}\" have:");
        }

        public async Task AfterReportAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var choice = await argument;
            switch (choice)
            {
                case PlasticStraws:
                    var badbusiness = _places.Find(b => b.Name == _establishment);
                    await context.PostAsync(
                        $"That's sad to hear {badbusiness.EntityID}. We have recorded this so that other people can avoid this establishment");
                    break;
                case NoPlasticStraws:
                    var goodbusiness = _places.Find(b => b.Name == _establishment);
                    await context.PostAsync(
                        $"Great {goodbusiness.EntityID}! We will tell people that they can come here to support an establishment that cares about our environment");
                    break;
                default:
                    await context.PostAsync("Sorry, I didn't understand");
                    break;
            }

            context.Done("");
        }
    }

    [Serializable]
    public class TheResponse
    {
        public SomeData d { get; set; }
    }

    [Serializable]
    public class SomeData
    {
        public List<BussinessResults> results { get; set; }
    }

    [Serializable]
    public class BussinessResults
    {
        public string EntityID { get; set; }
        public string Name { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string PostalCode { get; set; }
    }
}