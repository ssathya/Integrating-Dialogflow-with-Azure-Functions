# Integrating Dialogflow with Azure Functions.

Sadly there are not many tutorials about Dialogflow with webhooks published and could not come across any tutorial that uses Azure Functions as the Fulfillment webhook agent. This might not be a full-length tutorial but at least I'll document the process I followed to integrate a C# Azure Function with my Google Action.

I assume you have the basic understanding of Google Action, Dialog flow, and webhook so I'm not going to go into its details.

I'll be using the following services/products in my development cycle.



*   Visual Studio
*   [Mockable Io](https://www.mockable.io/)
*   [Ngrok](https://ngrok.com/)
*   [Postman](https://www.getpostman.com/)
*   And of course Google Actions, Dialogflow, and Azure.

We have a free version of all the above products and at least for this demo, we can build our application using the free versions.

Let's assume that you have created a Dialogflow project and want to create your first intent that ';will need to get the data from your Azure function.

Let us create an Entity **presidentName **and add, say, the first 4 presidents; George Washington, John Adams, Thomas Jefferson, and James Madison. Make sure you also add aliases for each one of them. Next, create an intent **presidentOfficeStart **and let's add a few training phrases.


*   When did Adams become the president?
*   Which year did George Washington become the president?
*   Which year did the 4th President take office?

_Adams alias for John Adams, 4th President for James Madison in presidentName entity. _

Make sure Adams, George Washington, 4th President are marked as @presidentName parameter.  Before we enable our webhook let us create a response (which we'll soon delete) "You asked about $presidentName; correct?". Save and run your intent and make sure you get a response like "You asked about George Washington; correct?" for your question "Which year did the 1st President take office?".

_By now you would understand I assume too much from the reader. I do not want this document to go too long so I presume that your I.Q is greater than 200._

Now that we've established the intent lets delete the default response and expand **Fulfillment **in our presidentOfficeStart intent and "Enable webhook call for this intent" and save our intent. 


## Test our fulfillment is working

Before we proceed further let's ensure our intent will get a response from webhook requests. Let's head over to Mockable and create a Rest Mock. 

Under path create our path as shown below:



<p id="gdcalert1" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/Integrating-Dialogflow0.png). Store image on your image server and adjust path/filename if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert2">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/Integrating-Dialogflow0.png "image_tooltip")


Make sure you set your path as President and also select Post as your Verb. _It took me some time to digest that requests come as POST. We are getting data and why do we have to use POST verb. Google has some writeup why it is done so but I'm not convinced._

Now update your Response body as follows:



<p id="gdcalert2" ><span style="color: red; font-weight: bold">>>>>>  gd2md-html alert: inline image link here (to images/Integrating-Dialogflow1.png). Store image on your image server and adjust path/filename if necessary. </span><br>(<a href="#">Back to top</a>)(<a href="#gdcalert3">Next alert</a>)<br><span style="color: red; font-weight: bold">>>>>> </span></p>


![alt_text](images/Integrating-Dialogflow1.png "image_tooltip")


Save and Start your mock. Also, copy your path; in my case its [https://demo6808454.mockable.io/President](https://demo6808454.mockable.io/President). 

Now head back to Dialogflow and select "Fulfillment" from the left panel. Enable Webhook and update your URL to your mock URL ([https://demo{xxxxxxx}.mockable.io/President](https://demo6808454.mockable.io/President)) and save. Now let us try to run our intend again.  Hopefully, you get your response from the mock server. Before we leave dialog flow make click on  "Diagnostic info" and click on "Copy fulfillment request as curl" and save the command

In a text editor; in my case, the Curl command looked as follows.


```shell
curl -X POST -H 'Content-Type: application/json' -d '{"responseId":"6f42f56e-2f8b-46d4-ad60-650890dbf6d9","queryResult":{"queryText":"in which year did the 1st president take office","parameters":{"presidentName":"George Washington"},"allRequiredParamsPresent":true,"intent":{"name":"projects/pleasedeleteme/agent/intents/520301ab-e485-4f11-87e0-421d1c0c793e","displayName":"presidentOfficeStart"},"intentDetectionConfidence":1,"languageCode":"en"},"originalDetectIntentRequest":{"payload":{}},"session":"projects/pleasedeleteme/agent/sessions/3e233bef-6254-8ee6-e2a5-f20419991258"}' 'https://demo6808454.mockable.io/President'
```


In the above example remove 


```shell
curl -X POST -H 'Content-Type: application/json' -d ' 
```


and 


```
' 'https://demo6808454.mockable.io/President'
```


I.e. you are extracting only the JSON and this is what you need.


```json
{
    "responseId": "6f42f56e-2f8b-46d4-ad60-650890dbf6d9",
    "queryResult": {
        "queryText": "in which year did the 1st president take office",
        "parameters": {
            "presidentName": "George Washington"
        },
        "allRequiredParamsPresent": true,
        "intent": {
            "name": "projects/pleasedeleteme/agent/intents/520301ab-e485-4f11-87e0-421d1c0c793e",
            "displayName": "presidentOfficeStart"
        },
        "intentDetectionConfidence": 1,
        "languageCode": "en"
    },
    "originalDetectIntentRequest": {
        "payload": {}
    },
    "session": "projects/pleasedeleteme/agent/sessions/3e233bef-6254-8ee6-e2a5-f20419991258"
}
```


We'll be using this JSON with Postman when we are building our Azure function.

Time to switch tools:

Launch Visual Studio (I'm using 2017 for this illustration) and create a new project: 

Create an Azure Function project and when presented to select a trigger select Http trigger and for <span style="text-decoration:underline;">this test project </span>we can select Access rights as Anonymous (please delete the project from Azure once we complete the tutorial).

To handle webhook request and response that comes from Dialogflow let's include "Google.Cloud.Dialogflow.V2" NuGet package. Parsing request and creating the response is much easier (actually saves a lot of manual fixes otherwise) with this package. At the time of writing this document, this package is in beta version and hence we need to select "Include Prerelease" when we use the Nuget package manager. 

Visual Studio Azure function wizard would have generated a class Function1. Replace it as follows:

```cs
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

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
```



In the above code, we are using Google's JsonParser to read and parse the request to WebhookRequest object. We are then extracting the requested President's name from the request parameter element to the variable name. We are now creating a WebhookRequest object and populating the FulfillmentText with value "Hello {whichever president you selected}" and return the value as ContentResult. 

If you are wondering why I'm not returning OkObjectResult but using ContentResult details are mentioned [here](https://tutel.me/c/programming/questions/50599456/dialogflow+fails+to+parse+the+json+response+from+my+webhook+seems+to+change+character+encoding). Run your function locally on your machine and "Azure Functions Core Tools" should launch your application. Function Core Tools gives you a local URL (e.g. [http://localhost:7071/api/President](http://localhost:7071/api/President)) that listens to your API calls. We can test our application by using Postman.



*   URL: the one you get from Core tools.
*   Method: Post
*   Body: the JSON that we extracted from Curl command as explained above (raw, JSON).

If everything goes well you should get a response as follows:


```json
{
    "fulfillmentText": "Hello George Washington"
}
```


Excellent; its time for your test application to run with Dialogflow. Time to use our next tool: Ngrok. Start Ngrok as follows:


```shell
ngrok http 7071 -host-header="localhost:7071"
```


_Of course, if Core tools use an alternate port you need to adjust your ports accordingly. _

Once ngrok launches it will give you forwarding URLs: both HTTP and HTTPS. We are interested only in the HTTPS URL (E.g. https://ba0cb49f.ngrok.io -> [http://localhost:7071](http://localhost:7071) ).

Now head back to Dialogflow and under "Fulfillment" change the URL to [https://{Your random number}.ngrok.io/api/President](https://ba0cb49f.ngrok.io/api/President)  and save your changes. Let us try our changes. In Dialogflow try "In which year did Washington become the president?" and hopefully you get your response as "Hello George Washington". 

All well; we can deploy our Azure function at Azure, change the URL in DialogFlow to point to our Azure endpoint or make the application work in making the application better. 

We'll make the application better in the next part.

