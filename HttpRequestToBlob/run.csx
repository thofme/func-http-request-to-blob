#r "Newtonsoft.Json"
#r "Microsoft.Azure.WebJobs.Extensions.Storage"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Storage;

public static async Task<IActionResult> Run(
    HttpRequest req,
    ILogger log,
    TextWriter outputBlob,
    Binder binder
)
{
    try {
        log.LogInformation("C# HTTP trigger function processed a request.");

        //Set container and directory from the Request query
        string container = req.Query["container"];
        string directory = req.Query["directory"];
        string partition = req.Query["partition"];

        //Set container and directory from the Headers
        string containerHeader = req.Headers["container"];
        string directoryHeader = req.Headers["directory"];
        string partitionHeader = req.Headers["partition"];

        //Read the request body - this should contain what we want to write to Blob
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic body = JsonConvert.DeserializeObject(requestBody);

        //If query parameter values don't exist, then see if they are in the body, then in the header, and finally default.
        container = container ?? body?.container ?? containerHeader ?? "raw";
        directory = directory ?? body?.directory ?? directoryHeader ?? "httpRequests";
        partition = partition ?? body?.partition ?? partitionHeader ?? "httpRequest";

        //Define the Blob location
        var blobFileName = Guid.NewGuid();
        var blobName = $"{container}/{directory}/{partition}/{blobFileName}.json";
        var storageAttribute = "AzureWebJobsStorage"; //Be sure that the Function App has this defined.

        //Create the attributes of the Blob
        var attributes = new Attribute[]
        {
            new BlobAttribute($"{blobName}"),
            new StorageAccountAttribute(storageAttribute)
        };

        //Write to the Blob, using attributes as defined above
        using (var writer = await binder.BindAsync<TextWriter>(attributes))
        {
            writer.Write(requestBody);
        }

        //Create OK results to send back to the requester (Data Factory)
        string result = $"{{'result': 'ok', 'status': 200, 'blobName': '{blobName}'}}";
        dynamic data = JsonConvert.DeserializeObject(result);

        //Send results back to the requester (Data Factory)
        return new OkObjectResult(data);
    }
    catch (Exception ex)
    {
        log.LogInformation($"Caught exception: {ex.Message}");

        string result = $"{{'result': 'bad', 'status': 400, 'Error': '{ex.Message.Replace("'", "\"")}'}}";
        dynamic data = JsonConvert.DeserializeObject(result);
        return new BadRequestObjectResult(data);
    }
}