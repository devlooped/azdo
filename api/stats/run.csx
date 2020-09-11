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
| project name, operation = operation_Name, org = tostring(customDimensions['org']), proj = tostring(customDimensions['project'])
| extend unique = strcat(org, proj)
| summarize total = count(), org = dcount(org),  proj = dcount(unique)")).Content.ReadAsStringAsync();

    dynamic stats = JObject.Parse(json);
    return new ContentResult 
    { 
        Content = JsonConvert.SerializeObject(new 
        { 
            total = stats.tables[0].rows[0][0].ToString(),
            organizations = stats.tables[0].rows[0][1].ToString(),
            projects = stats.tables[0].rows[0][2].ToString(),
        }), 
        ContentType = "application/json", 
        StatusCode = 200 
    };
}