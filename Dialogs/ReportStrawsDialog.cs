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

            var restClient = new RestClient("https://maps.googleapis.com/maps/api/");
            var request = new RestRequest("place/nearbysearch/json",
                Method.GET)
            {
                RequestFormat = DataFormat.Json,
                JsonSerializer = new JsonSerializer()
            };
            request.AddQueryParameter("location", "52.954783,-1.158109");
            request.AddQueryParameter("radius", "5000");
            request.AddQueryParameter("type", "restaurant");
            request.AddQueryParameter("keyword", message.Text);

            var environmentVariable = Environment.GetEnvironmentVariable("BING_API_KEY");

            request.AddQueryParameter("key",
                string.IsNullOrEmpty(environmentVariable)
                    ? ConfigurationManager.AppSettings.Get("BING_API_KEY")
                    : environmentVariable);

            var restResponse = restClient.Execute<SomeData>(request);

            _places = restResponse.Data.results;
            PromptDialog.Choice(context, SelectAsync,
                restResponse.Data.results.Select(b => $"{b.Name} ({b.Vicinity})").ToList(),
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
                    var badbusiness = _places.Find(b => $"{b.Name} ({b.Vicinity})" == _establishment);
                    await context.PostAsync(
                        $"That's sad to hear about {badbusiness.Name}. We have recorded this so that other people can avoid this establishment");
                    PostToApi(badbusiness, 1);

                    break;
                case NoPlasticStraws:
                    var goodbusiness = _places.Find(b => b.Name == _establishment);
                    await context.PostAsync(
                        $"{goodbusiness.Name} is great! We will tell people that they can come here to support an establishment that cares about our environment");
                    PostToApi(goodbusiness, 0);
                    break;
                default:
                    await context.PostAsync("Sorry, I didn't understand");
                    break;
            }

            context.Done("");
        }

        private static void PostToApi(BussinessResults badbusiness, int straws)
        {
            var restClient = new RestClient("https://requestb.in");
            var request = new RestRequest("/1e9aae61",
                Method.POST)
            {
                RequestFormat = DataFormat.Json,
            };
            request.AddJsonBody(new
            {
                Id = badbusiness.Id,
                Name = badbusiness.Name,
                Latitude = badbusiness.location.lat,
                Longitude = badbusiness.location.lng,
                Straws = straws
            });
            try
            {
                restClient.Execute(request);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    [Serializable]
    public class SomeData
    {
        public List<BussinessResults> results { get; set; }
    }

    [Serializable]
    public class BussinessResults
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Location location { get; set; }
        public string Vicinity { get; set; }
    }

    [Serializable]
    public class Location
    {
        public decimal lat { get; set; }
        public decimal lng { get; set; }
    }
}