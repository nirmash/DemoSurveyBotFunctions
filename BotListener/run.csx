#r "Newtonsoft.Json"

using System;
using System.Net;
using System.Configuration;
using Newtonsoft.Json; 
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public static void Run(string myQueueItem, out object outputDocument,  out string outputQueueItem, TraceWriter log)
{
    log.Info(myQueueItem);
    string qMeta = JToken.Parse(GetSurveyMetaJSON()).ToString(); 
    log.Info(qMeta);
    /*
    myQueueItem = CalculateRating(myQueueItem,qMeta);
    log.Info(myQueueItem); 
    JObject o = JObject.Parse(myQueueItem);
    outputDocument = o;
    */
    outputQueueItem = myQueueItem;
}
private static string CalculateRating(string sAnswers, string sQuestions)
{
    JObject questions = JObject.Parse(sQuestions);
    JObject answers = JObject.Parse(sAnswers);
    //get the answers  
    IDictionary<string, int> correctAnswers = new Dictionary<string, int>();
    JToken jItem = questions.SelectToken("properties").First;
    string pth = jItem.Path.Substring(11);

    while(jItem!=null && pth!= "Comments")
    {
        correctAnswers.Add(pth, int.Parse(jItem.First.SelectToken("CorrectResponse").ToString()));
        jItem = jItem.Next;
        pth = jItem.Path.Substring(11);
    }

    //load answers into a dictionary
    int rating = 0;
    IDictionary<string, string> dAnswers = JsonConvert.DeserializeObject<IDictionary<string, string>>(sAnswers);
    int answered=-1;
    foreach (KeyValuePair<string,int> pair in correctAnswers)
    {
        try { 
            answered = (int)float.Parse(dAnswers[pair.Key].ToString());
        }
        catch{
            answered = -1;
        }
        int correct = pair.Value;
        if (answered == correct)
            rating++;
    }
    dAnswers.Add("Rating", rating.ToString());
    string rateStr= "{'Rating' : '" + rating.ToString() + "'}";
    JObject jo1 = JObject.Parse(rateStr);
    answers.Merge(jo1, new JsonMergeSettings
    {
        MergeArrayHandling = MergeArrayHandling.Union
    });
    return answers.ToString();
}
private static string GetSurveyMetaJSON()
{
    string metaUri = ConfigurationManager.AppSettings["MetaFunctionURL"];

    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(metaUri);
    // Set the Method property of the request to POST.
    request.Method = "GET";
    // Get the response.
    WebResponse response = request.GetResponse(); 
    // Display the status.
    Stream dataStream = response.GetResponseStream();
    // Open the stream using a StreamReader for easy access.
    StreamReader reader = new StreamReader(dataStream);
    // Read the content.
    return reader.ReadToEnd();
}
