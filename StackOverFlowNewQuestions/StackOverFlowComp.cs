using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StackOverFlowNewQuestions
{
    public static class StackOverFlowComp
    {
        [FunctionName("StackOverFlowComp")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var result = await MakeStackExchangeRequest();
            var newQuestions = await ProcessResult(result, log);
            await MakeSlackRequest($"StackOverFlow New questions on Date {DateTime.Today.ToShortDateString()} \nAzure Questions : {newQuestions[0].ToString()} and AWS Questions : { newQuestions[1].ToString() }");
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        private static async Task<List<string>> ProcessResult(List<string> stackExchReq, ILogger log)
        {

            try
            {
                var jsonObAzure = JsonConvert.DeserializeObject<dynamic>(stackExchReq[0]);
                var jsonObAWS = JsonConvert.DeserializeObject<dynamic>(stackExchReq[1]);

                // var items = jsonObAWS.Value<JArray>("items");

                int AzureCount = jsonObAzure.items.Count;
                var hasMoreAzure = jsonObAzure.has_more=="True"? " +" : "";

                int AWSCount = jsonObAWS.items.Count;
                var hasMoreAWS = jsonObAWS.has_more == "True" ? " +" : "";


                List<string> result = new List<string>();

                result.Add($"{Convert.ToString(AzureCount)}{hasMoreAzure}");
                result.Add($"{Convert.ToString(AWSCount)}{hasMoreAWS}");
                return result;
            }
            catch (Exception ex)
            {

                throw ex;
            }
           
           
        }

        public static async Task<string> MakeSlackRequest(string msg)
        {
            using (var client = new HttpClient())
            {
                var requestData = new StringContent("{'text':'"+msg+"'}",Encoding.UTF8,"application/json");
                var response = await client.PostAsync($"https://hooks.slack.com/services/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",requestData);
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }

        }
        public static async Task<List<string>> MakeStackExchangeRequest()
        {

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            using (var client = new HttpClient(handler))
            {
                var objEpoc = (int)(DateTime.UtcNow.AddDays(-1) - new DateTime(1970, 1, 1)).TotalSeconds;
                var searchObject = "Azure";
                var responseForAzure = await client.GetAsync($"https://api.stackexchange.com/2.2/search?fromdate={objEpoc}&order=desc&sort=activity&intitle={searchObject}&site=stackoverflow");
                var resultForAzure = await responseForAzure.Content.ReadAsStringAsync();
                searchObject = "AWS";
                var responseForAWS = await client.GetAsync($"https://api.stackexchange.com/2.2/search?fromdate={objEpoc}&order=desc&sort=activity&intitle={searchObject}&site=stackoverflow");
                var resultForAWS = await responseForAWS.Content.ReadAsStringAsync();
                var returnResult = new List<string>();
                returnResult.Add(resultForAzure);
                returnResult.Add(resultForAWS);
                return returnResult;
            }
        }
    }
}
