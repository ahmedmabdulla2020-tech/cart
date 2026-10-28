using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureCourse.Function
{
    public class EventGridFunction
    {
        private readonly ILogger<EventGridFunction> log;

        public EventGridFunction(ILogger<EventGridFunction> log)
        {
            this.log = log;
        }

        [Function("ProcessOrderStorage")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [BlobInput("orders", Connection = "StorageConnectionString")] BlobContainerClient container)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var blobName=Guid.NewGuid().ToString()+".txt";

            log.LogInformation($"blobName: {blobName}");
            log.LogInformation($"Order received: {requestBody}");

            var blobClient=container.GetBlobClient(blobName);
            await blobClient.UploadAsync(BinaryData.FromString(requestBody));

            return new OkResult();
        }
    }
}
