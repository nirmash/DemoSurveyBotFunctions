#r "Newtonsoft.Json"

using System;
using System.Net;
using System.Configuration;
using Newtonsoft.Json; 
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, IAsyncCollector<string> outputQueueItem, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");
    string incoming = await req.Content.ReadAsStringAsync();
    log.Info(incoming);

    string[] pairs = incoming.Split('&');
    string sComments="";
    string sRet = "{";
    foreach(string sVal in pairs)
    {
        string[] pair = sVal.Split('=');
        pair[1]= WebUtility.UrlDecode(pair[1]);
        sRet += "'" + pair[0] + "':'" + pair[1] + "',";
        if(pair[0]=="Comments")
            sComments=pair[1];
    }
    //Comments
    log.Info(sComments);
    if(sComments!="")
        sRet += "'Sentiment' : '" + GetSentiment(sComments) + "'}";
    else 
        sRet += "'Sentiment' : 'Indifferent'}";
    JObject jo3 = JObject.Parse(sRet);
    log.Info(jo3.ToString());
    await outputQueueItem.AddAsync(jo3.ToString()); 
    return req.CreateResponse(HttpStatusCode.OK, "");
}

private class BatchInput
{
    public List<DocumentInput> documents { get; set; }
}
private class DocumentInput
{
    public double id { get; set; }
    public string text { get; set; }
}
private class BatchResult
{
    public List<DocumentResult> documents { get; set; }
}
private class DocumentResult
{
    public double score { get; set; }
    public string id { get; set; }
}

private static string GetSentiment(string comment)
{
            string apiKey = ConfigurationManager.AppSettings["TextApiKey"];
            string queryUri = ConfigurationManager.AppSettings["TextApiUri"];

            // Create a request using a URL that can receive a post. 
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(queryUri);
            // Set the Method property of the request to POST.
            request.Method = "POST";
            request.Accept = "application/json";
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

            var sentimentInput = new BatchInput
            {
                documents = new List<DocumentInput> {
                    new DocumentInput {
                            id = 1,
                            text = comment
                        }
                    }
            };
            string postData = JsonConvert.SerializeObject(sentimentInput);

            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            // Set the ContentType property of the WebRequest.
            //request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            // Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.

            var sentimentJsonResponse = JsonConvert.DeserializeObject<BatchResult>(responseFromServer);
            var sentimentScore = sentimentJsonResponse?.documents?.FirstOrDefault()?.score ?? 0;

            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();
            string retMessage="";
            if (sentimentScore > 0.7)
            {
                retMessage = $"Positive";
            }
            else if (sentimentScore < 0.3)
            {
                retMessage = $"Negative";
            }
            else
            {
                retMessage = $"Indifferent";
            }

            return retMessage;
}