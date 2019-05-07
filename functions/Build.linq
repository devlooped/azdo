<Query Kind="Program">
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
  <DisableMyExtensions>true</DisableMyExtensions>
</Query>

void Main()
{
    // If no parameters provided, go to docs
    Assert.IsType<RedirectResult>(Run());
    Assert.Equal("https://github.com/kzu/azdo#builds", Run<RedirectResult>().Url);
    // If project/id missing, go to docs
    Assert.IsType<RedirectResult>(Run("foo"));
    Assert.Equal("https://github.com/kzu/azdo#builds", Run<RedirectResult>("foo").Url);

    // If org/project provided but no id, go to docs
    Assert.IsType<RedirectResult>(Run("foo", "oss"));
    Assert.Equal("https://github.com/kzu/azdo#builds", Run<RedirectResult>("foo", "oss").Url);

    Assert.IsType<RedirectResult>(Run("kzu", "oss", 1));
    Assert.IsType<RedirectResult>(Run("kzu", "1"));

    // Low id defaults to definition
    Assert.StartsWith("https://dev.azure.com/kzu/oss/_build/index?definitionId=", Run<RedirectResult>("kzu", "oss", 1).Url);
    // High id explicitly opted in to definition
    Assert.StartsWith("https://dev.azure.com/kzu/oss/_build/index?definitionId=", Run<RedirectResult>("kzu", "oss", 500, "d").Url);

    // High id defaulted to build
    Assert.StartsWith("https://dev.azure.com/kzu/oss/_build/index?buildId=", Run<RedirectResult>("kzu", "oss", 500).Url);
    // Low id explicity opted in to definition
    Assert.StartsWith("https://dev.azure.com/kzu/oss/_build/index?buildId=", Run<RedirectResult>("kzu", "oss", 1, "b").Url);

    // DevDiv defaults
    Assert.StartsWith("https://dev.azure.com/DevDiv/DevDiv/_build/index?definitionId=", Run<RedirectResult>("DevDiv", "10000").Url);
    Assert.StartsWith("https://dev.azure.com/DevDiv/DevDiv/_build/index?buildId=", Run<RedirectResult>("DevDiv", "1100000").Url);
    Assert.StartsWith("https://dev.azure.com/DevDiv/DevDiv/_build/index?definitionId=", Run<RedirectResult>("10000").Url);
    Assert.StartsWith("https://dev.azure.com/DevDiv/DevDiv/_build/index?buildId=", Run<RedirectResult>("1100000").Url);
}

static TActionResult Run<TActionResult>(string org = null, string project = null, double? id = null, string query = null)
    => (TActionResult)Run(org, project, id, query);

static IActionResult Run(string org = null, string project = null, double? id = null, string query = null)
{   
    var request = new DefaultHttpRequest(new DefaultHttpContext())
    {
        Query = query == null ?
            new QueryCollection() :
            new QueryCollection(new Dictionary<string, StringValues>
            {
                { query, "" }
            })
    };
    
    var services = new ServiceCollection().AddLogging(builder => builder.AddConsole());
    return Run(request, services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Console"), org, project, id);
}

public static IActionResult Run(HttpRequest req, ILogger log, string org = null, string project = null, double? id = null)
{
    var parsed = id.HasValue ? id.Value : 0d;
    
    // If the id is omitted, we try to parse it from the org (for DevDiv's super-short 
    // format) or from the project, and default the project to == org further down (or both to DevDiv)
    if (id == null && !(double.TryParse(org, out parsed) || double.TryParse(project, out parsed)))
    {
        return new RedirectResult("https://github.com/kzu/azdo#builds");
    }

    // If we didn't get an id parameter, probe for defaults for the org/project
    if (id == null)
    {
        // If didn't a project either, we must have parsed the id from the org above, 
        // meaning it's a DevDiv super-short URL
        if (project == null)
        {
            org = project = "DevDiv";
        }
        else
        {
            // Otherwise, we must have parsed it from the project so default project to org as a shorthand too.
            project = org;
        }
    }
        
    // TODO: add telemetry here so I can update defaults appropriately 

    // A ?d query string parameter forces the id to be interpreted as an RD
    var def = req.QueryString.HasValue && req.QueryString.Value.StartsWith("?d");
    // We know DevDiv to be huge and have over a thousand RD with over 300k releases
    if (!def)
    {
        if (project.Equals("devdiv", StringComparison.OrdinalIgnoreCase) && parsed < 1000000)
        {
            def = true;
        } 
        else if (parsed < 200 && !(req.QueryString.HasValue && req.QueryString.Value.StartsWith("?b")))
        {
            // We consider everything else to be small-ish projects, with "few" BDs so most urls don't include 
            // the querystring arg. If we find many ?d in actual use, we might want to increment this together 
            // with a corresponding update to the linkinator. We also consider ?b to force a non-def redirect.
            def = true;
        }
    }

    if (def)
        return new RedirectResult($"https://dev.azure.com/{org}/{project}/_build/index?definitionId={parsed}");
    else
        return new RedirectResult($"https://dev.azure.com/{org}/{project}/_build/index?buildId={parsed}");
}