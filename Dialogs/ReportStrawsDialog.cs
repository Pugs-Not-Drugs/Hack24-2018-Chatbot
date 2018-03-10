using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
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
                    PostToApi(badbusiness, 1);
                    await context.PostAsync(
                        $"That's sad to hear about {badbusiness.Name}. We have recorded this so that other people can avoid this establishment");
                    break;
                case NoPlasticStraws:
                    var goodbusiness = _places.Find(b => b.Name == _establishment);
                    PostToApi(goodbusiness, 0);
                    await context.PostAsync(
                        $"{goodbusiness.Name} is great! We will tell people that they can come here to support an establishment that cares about our environment");
                    break;
                default:
                    await context.PostAsync("Sorry, I didn't understand");
                    break;
            }

            context.Done("");
        }

        private static void PostToApi(BussinessResults badbusiness, int straws)
        {
            HttpClient client = new HttpClient();
            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("Id", badbusiness.Id));
            values.Add(new KeyValuePair<string, string>("Name", badbusiness.Name));
            values.Add(new KeyValuePair<string, string>("Latitude", badbusiness.geometry.location.lat.ToString()));
            values.Add(new KeyValuePair<string, string>("Longitude", badbusiness.geometry.location.lng.ToString()));
            values.Add(new KeyValuePair<string, string>("Straws", straws.ToString()));

            using (FormUrlEncodedContent content = new FormUrlEncodedContent(values))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                client.PostAsync("https://econotts-api.azurewebsites.net/api/establishment/add", content);
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
        public Geometry geometry { get; set; }
        public string Vicinity { get; set; }
    }

    [Serializable]
    public class Geometry
    {
        public Location location { get; set; }
    }

    [Serializable]
    public class Location
    {
        public decimal lat { get; set; }
        public decimal lng { get; set; }
    }

    [Serializable]
    public class PostObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public int Straws { get; set; }
    }
}