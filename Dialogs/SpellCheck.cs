using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public class SpellCheck
    {
        public List<FlaggedTokens> flaggedTokens { get; set; }

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
            var deserializeObject = SimpleJson.SimpleJson.DeserializeObject<SpellCheck>(contentString);
            deserializeObject.flaggedTokens.ForEach(t => messageText = messageText.Replace(t.token, t.suggestions.First().suggestion));
            return messageText;
        }
    }
}