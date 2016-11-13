#r "Newtonsoft.Json"

using System;
using System.Net;
using System.Configuration;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static void Run(string myQueueItem, TraceWriter log)
{
    log.Info($"Entry string: {myQueueItem}");
    //load the data from the queus
    IDictionary<string, string> dAnswers = JsonConvert.DeserializeObject<IDictionary<string, string>>(myQueueItem);
    //make the header record
    string sResultsSummaries = "[{";
    sResultsSummaries += "'Email' : '" + dAnswers["Email"].ToString() + "', ";
    sResultsSummaries += "'Comments' : '" + dAnswers["Comments"].ToString() + "', ";
    sResultsSummaries += "'Sentiment' : '" + dAnswers["Sentiment"].ToString() + "', ";
    sResultsSummaries += "'Rating' : " + dAnswers["Rating"].ToString() + "";
    sResultsSummaries += "}]";
    log.Info($"sResultsSummaries: {sResultsSummaries}");
    //make the child records 
    string sResultsRaw = "[";
    foreach (KeyValuePair<string, string> pair in dAnswers)
    {

        string sKeyName = pair.Key;
        if (sKeyName != "Comments" && sKeyName != "Email" && sKeyName != "Sentiment" && sKeyName != "Rating")
        {
            sResultsRaw += "{'Email' : '" + dAnswers["Email"].ToString() + "', ";
            sResultsRaw += "'Question' : '" + pair.Key + "', ";
            sResultsRaw += "'Answer' : " + ((int)float.Parse(pair.Value)).ToString() + "},";
        }
    }
    sResultsRaw = sResultsRaw.Substring(0, sResultsRaw.Length - 1);
    sResultsRaw += "]";
    log.Info($"sResultsRaw: {sResultsRaw}");

    //send to PBI (both)
    var httpWebRequest = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["PBIEndpoint"]);
    httpWebRequest.ContentType = "application/json";
    httpWebRequest.Method = "POST"; 

    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
    {
        streamWriter.Write(sResultsSummaries);
    }
    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
    {
        var result = streamReader.ReadToEnd();
        log.Info(result);
    }
}