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
    log.LogInformation("C# HTTP trigger function processed a request.");

    //Set container and directory from the Request query
    string container = req.Query["container"];
    string directory = req.Query["directory"];
    string partition;

    //Set container and directory from the Headers
    string containerHeader = req.Headers["container"];
    string directoryHeader = req.Headers["directory"];
    string partitionHeader = req.Headers["partition"];

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic body = JsonConvert.DeserializeObject(requestBody);
    
    //If query parameter values don't exist, then see if they are in the body
    container = container ?? body?.container ?? containerHeader ?? "raw";
    directory = directory ?? body?.directory ?? directoryHeader ?? "httpRequests";
    partition = partitionHeader ?? "httpRequest";

    var blobFileName = Guid.NewGuid();
    var blobName = $"{container}/{directory}/{partition}/{blobFileName}.json";
    var storageAttribute = "AzureWebJobsStorage";

    var attributes = new Attribute[]
    {
        new BlobAttribute($"{blobName}"),
        new StorageAccountAttribute(storageAttribute)
    };

    using (var writer = await binder.BindAsync<TextWriter>(attributes))
    {
        writer.Write(requestBody);
    }

    string result = $"{{'result': 'ok', 'status': 200, 'blobName': '{blobName}'}}";
    dynamic data = JsonConvert.DeserializeObject(result);

    return new OkObjectResult(data);
}