using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RestSharp;
using RestSharp.Serializers;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class GetStrawsDialogs: IDialog<string>
    {
        private readonly Something _deserializeObject;
        private List<BussinessResults> _bussinessResultses;

        public GetStrawsDialogs(Something deserializeObject)
        {
            _deserializeObject = deserializeObject;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(DoStrawStuff);
        }
        
        private async Task DoStrawStuff(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var entities = _deserializeObject.entities.First(e => e.type == "Establishment");
            string theentity = null;
            if (entities != null)
            {
                theentity = _deserializeObject.entities.First(e => e.type == "Establishment").entity;
            }

            // get establishment back
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
            request.AddQueryParameter("keyword", theentity);

            var environmentVariable = Environment.GetEnvironmentVariable("BING_API_KEY");

            request.AddQueryParameter("key",
                string.IsNullOrEmpty(environmentVariable)
                    ? ConfigurationManager.AppSettings.Get("BING_API_KEY")
                    : environmentVariable);

            var restResponse = restClient.Execute<SomeData>(request);

            _bussinessResultses = restResponse.Data.results;
            //pipe into google api
            if (_bussinessResultses.Count == 0)
            {
                await context.PostAsync("Sorry, I couldn't find that establishment");
            }

            PromptDialog.Choice(context, SelectAsync,
                restResponse.Data.results.Select(b => $"{b.Name} ({b.Vicinity})").ToList(),
                "Which one of these did you mean?");
        }

        private async Task SelectAsync(IDialogContext context, IAwaitable<string> result)
        {
            var choice = await result;
            var id = _bussinessResultses.First(b => $"{b.Name} ({b.Vicinity})" == choice).Id;
            var client = new HttpClient();
            var httpResponseMessage =
                await client.GetAsync($"https://econotts-api.azurewebsites.net/api/establishment/{id}");

            if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                await context.PostAsync("Sorry, I couldn't find that establishment");
            }
            else
            {
                var info = SimpleJson.SimpleJson.DeserializeObject<ResultFromApi>(await httpResponseMessage.Content
                    .ReadAsStringAsync());
                
                await context.PostAsync($"We have {info.happyStraws} reports that they use no plastic straws and {info.sadStraws} that they do.");
            }

            
        }
    }
}