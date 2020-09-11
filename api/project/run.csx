#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    var http = new HttpClient();
    http.DefaultRequestHeaders.Add("x-api-key", Environment.GetEnvironmentVariable("APPINSIGHTS_APIKEY"));
    var json = await (await http.GetAsync($"https://api.applicationinsights.io/v1/apps/{Environment.GetEnvironmentVariable("APPINSIGHTS_APPID")}/query?query=" +
@"customEvents
| order by timestamp desc
| where name == 'redirect'
| project proj = tostring(customDimensions['project'])
| summarize dcount(proj)")).Content.ReadAsStringAsync();

    dynamic stats = JObject.Parse(json);
    return new ContentResult 
    { 
        Content = JsonConvert.SerializeObject(new 
        { 
            schemaVersion = 1,
            label = "",
            message = stats.tables[0].rows[0][0].ToString(),
        }), 
        ContentType = "application/json", 
        StatusCode = 200 
    };
}