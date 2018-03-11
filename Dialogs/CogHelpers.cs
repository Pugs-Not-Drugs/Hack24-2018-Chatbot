using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public class CogHelpers
    {
        public static async Task<string> RunThroughSpellCheck(string messageText)
        {
            var environmentVariable = Environment.GetEnvironmentVariable("SPELL_CHECK");
            var spellcheckKey = String.IsNullOrEmpty(environmentVariable)
                ? ConfigurationManager.AppSettings.Get("SPELL_CHECK")
                : environmentVariable;

            const string host = "https://api.cognitive.microsoft.com";
            const string path = "/bing/v7.0/spellcheck?";


            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", spellcheckKey);

            HttpResponseMessage response;
            const string uri = host + path;

            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("mkt", "en-GB"),
                new KeyValuePair<string, string>("mode", "proof"),
                new KeyValuePair<string, string>("text", messageText)
            };

            using (var content = new FormUrlEncodedContent(values))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                response = await client.PostAsync(uri, content);
            }

            var contentString = await response.Content.ReadAsStringAsync();
            var deserializeObject = SimpleJson.SimpleJson.DeserializeObject<SCHelper>(contentString);
            deserializeObject.flaggedTokens.ForEach(t =>
                messageText = messageText.Replace(t.token, t.suggestions.First().suggestion));
            return messageText;
        }

//        public static async Task<Tuple<bool, string>> LUISDialogs(string messageText)
//        {
//            var environmentVariable = Environment.GetEnvironmentVariable("SPELL_CHECK");
//            var spellcheckKey = string.IsNullOrEmpty(environmentVariable)
//                ? ConfigurationManager.AppSettings.Get("SPELL_CHECK")
//                : environmentVariable;
//
//            var luienvironmentVariable = Environment.GetEnvironmentVariable("LUI_KEY");
//            var luiKey = string.IsNullOrEmpty(luienvironmentVariable)
//                ? ConfigurationManager.AppSettings.Get("LUI_KEY")
//                : luienvironmentVariable;
//
//            var client = new HttpClient();
//            var uri =
//                $"https://westeurope.api.cognitive.microsoft.com/luis/v2.0/apps/ebcd8903-9d89-4423-8874-0f43d03af753?subscription-key={luiKey}&spellCheck=true&bing-spell-check-subscription-key={spellcheckKey}&verbose=true&timezoneOffset=0&q={messageText}";
//
//            var httpResponseMessage = await client.GetAsync(uri);
//            var contentString = await httpResponseMessage.Content.ReadAsStringAsync();
//            var deserializeObject = SimpleJson.SimpleJson.DeserializeObject<Something>(contentString);
//
//
//
//            if (deserializeObject.topScoringIntent.score == 1)
//            {
//                switch (deserializeObject.topScoringIntent.intent)
//                {
//                    case "Straw Policy":
//                    
//                        var entities = deserializeObject.entities.First(e => e.type == "Establishment");
//                        string theentity = null;
//                        if (entities != null)
//                        {
//                            theentity = deserializeObject.entities.First(e => e.type == "Establishment").entity;
//                        }
//
//                        return new Tuple<bool, string>(true, theentity);
//                        break;
//                    
//                }    
//            }
//            
//
//            return new Tuple<bool, string>(deserializeObject.topScoringIntent.intent == "Straw Policy" &&
//                                           deserializeObject.topScoringIntent.score == 1, theentity);
//        }
   }

    public class SCHelper
    {
        public List<FlaggedTokens> flaggedTokens { get; set; }
    }

    public class Something
    {
        public TopIntent topScoringIntent { get; set; }
        public List<Entities> entities { get; set; }
    }

    public class Entities
    {
        public string entity { get; set; }
        public string type { get; set; }
    }

    public class TopIntent
    {
        public string intent { get; set; }
        public double score { get; set; }
    }
}