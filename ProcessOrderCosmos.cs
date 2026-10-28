using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureCourse.Function
{
    public class CosmosOrderFunction
    {
        private readonly ILogger<CosmosOrderFunction> log;

        public CosmosOrderFunction(ILogger<CosmosOrderFunction> log)
        {
            this.log = log;
        }

        [Function("ProcessOrderCosmos")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            log.LogInformation($"Order JSON: {requestBody}");

            //To save the order in Cosmos DB, change the return type to CosmosOrderResponse
            //and return: new CosmosOrderResponse { Order = JsonConvert.DeserializeObject<Order>(requestBody), Result = new OkResult() };

            return new OkResult();
        }
    }

    public class CosmosOrderResponse
    {
        [CosmosDBOutput(databaseName: "readit-orders", containerName: "orders", Connection = "CosmosDBConnection")]
        public Order Order { get; set; }

        [HttpResult]
        public IActionResult Result { get; set; }
    }
}
