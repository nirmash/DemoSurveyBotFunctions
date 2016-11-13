#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.WindowsAzure.Storage.Blob;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TextReader inputBlob, TraceWriter log)
{
    string sJson = inputBlob.ReadToEnd();
    log.Info(sJson); 
    return req.CreateResponse(HttpStatusCode.OK, sJson);  
}  