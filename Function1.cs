using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus;
using System.Text;

namespace HTTPTrigger
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,  "post", Route = null)] HttpRequest req,
            [CosmosDB("ToDoItems", "Items",
            Connection= "CosmosDBConnection")]CosmosClient client,
             [ServiceBus("myTopic", Connection = "ServiceBusConnection")] MessageSender messagesQueue,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Create or retrieve the TelemetryClient instance
            TelemetryConfiguration telemetryConfig = TelemetryConfiguration.Active;
            TelemetryClient telemetryClient = new TelemetryClient(telemetryConfig);

            EventTelemetry eventTelemetry = new EventTelemetry("CustomEventName");
            eventTelemetry.Properties["CorrelationId"] = "correlationId"; // Add the correlation ID as a custom property

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            byte[] bytes = Encoding.ASCII.GetBytes(responseMessage);
            Message m1 = new Message(bytes);
            await messagesQueue.SendAsync(m1);

            return new OkObjectResult(responseMessage);
        }
    }
}
