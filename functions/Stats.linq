<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <NuGetReference>Microsoft.ApplicationInsights</NuGetReference>
  <NuGetReference>Microsoft.AspNetCore.Mvc</NuGetReference>
  <NuGetReference>Microsoft.Extensions.Logging.Console</NuGetReference>
  <NuGetReference>Microsoft.Extensions.Logging.Debug</NuGetReference>
  <NuGetReference>xunit.assert</NuGetReference>
  <Namespace>Microsoft.ApplicationInsights</Namespace>
  <Namespace>Microsoft.AspNetCore.Http</Namespace>
  <Namespace>Microsoft.AspNetCore.Http.Internal</Namespace>
  <Namespace>Microsoft.AspNetCore.Mvc</Namespace>
  <Namespace>Microsoft.Extensions.DependencyInjection</Namespace>
  <Namespace>Microsoft.Extensions.Logging</Namespace>
  <Namespace>Microsoft.Extensions.Logging.Console</Namespace>
  <Namespace>Microsoft.Extensions.Logging.Debug</Namespace>
  <Namespace>Microsoft.Extensions.Primitives</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Xunit</Namespace>
  <Namespace>Microsoft.ApplicationInsights.Extensibility</Namespace>
  <Namespace>Microsoft.ApplicationInsights.Channel</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>System.Configuration</Namespace>
  <DisableMyExtensions>true</DisableMyExtensions>
</Query>

void Main()
{
    Run<ContentResult>().Content.Dump();
}

static TActionResult Run<TActionResult>()
    => (TActionResult)Run();

static IActionResult Run()
    => Run(
        new DefaultHttpRequest(new DefaultHttpContext()),
        new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Console")).Result;

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
            total = stats.tables[0].rows[0][0],
            organizations = stats.tables[0].rows[0][1],
            projects = stats.tables[0].rows[0][2],
        }), 
        ContentType = "application/json", 
        StatusCode = 200 
    };
}