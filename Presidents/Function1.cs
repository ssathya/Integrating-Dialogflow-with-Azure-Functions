using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;

namespace Presidents
{
    public static class Function1
    {
		private static readonly JsonParser jsonParser =
		new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

		[FunctionName("President")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Starting President service.");

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
			WebhookRequest request;
			request = jsonParser.Parse<WebhookRequest>(requestBody);
			var firstParamName = request.QueryResult.Parameters.Fields["presidentName"].ToString().Replace("\"", "");


			var response = new WebhookResponse
			{
				FulfillmentText = $"Hello {firstParamName}"
			};
			log.LogInformation("Ending Presidnet service");
			return new ContentResult
			{
				Content = response.ToString(),
				ContentType = "application/json",
				StatusCode = 200
			};

        }
    }
}
