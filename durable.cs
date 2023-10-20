using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdamDurableCombineFA
{
    public static class durable
    {
        [Function(nameof(durable))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var inputData = context.GetInput<List<string>>();

            if (!(inputData is IEnumerable<string> stringArray))
            {
                await Console.Out.WriteLineAsync("Input data is not an array of strings.");
                return null;
            }

            ILogger logger = context.CreateReplaySafeLogger(nameof(durable));
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();

            foreach (string inputString in stringArray)
            {
                // Invoke an activity with the current string as input
                string result = await context.CallActivityAsync<string>("SayHello", inputString);

                // Add the result to the list of results
                outputs.Add(result);
            }


            string result = await context.CallActivityAsync<string>("SayHello", inputString);

            // Replace name and input with values relevant for your Durable Functions Activity
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }

        [Function("durable_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("durable_HttpStart");

            // req holds json list of strings
            // Parse out the contents of req into a List
            var reqBody = await req.ReadAsStringAsync();
            List<string> input = System.Text.Json.JsonSerializer.Deserialize<List<string>>(reqBody);

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(durable), input);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
