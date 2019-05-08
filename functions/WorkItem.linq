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
    Assert.Equal("https://github.com/kzu/azdo#work-items", Run<RedirectResult>().Url);
    // If project/id missing, go to docs
    Assert.IsType<RedirectResult>(Run("foo"));
    Assert.Equal("https://github.com/kzu/azdo#work-items", Run<RedirectResult>("foo").Url);

    // If org/project provided but no id, go to docs
    Assert.IsType<RedirectResult>(Run("foo", "oss"));
    Assert.Equal("https://github.com/kzu/azdo#work-items", Run<RedirectResult>("foo", "oss").Url);

    Assert.IsType<RedirectResult>(Run("kzu", "oss", 1));
    Assert.IsType<RedirectResult>(Run("kzu", "1"));

    // DevDiv defaults
    Assert.StartsWith("https://dev.azure.com/DevDiv/DevDiv/_workitems/edit/", Run<RedirectResult>("DevDiv", "10000").Url);
    Assert.StartsWith("https://dev.azure.com/DevDiv/DevDiv/_workitems/edit/", Run<RedirectResult>("10000").Url);
}

static TActionResult Run<TActionResult>(string org = null, string project = null, double? id = null)
    => (TActionResult)Run(org, project, id);

static IActionResult Run(string org = null, string project = null, double? id = null)
    => Run(
        new DefaultHttpRequest(new DefaultHttpContext()), 
        new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Console"), 
        org, project, id);

public static IActionResult Run(HttpRequest req, ILogger log, string org = null, string project = null, double? id = null)
{
    var parsed = id.HasValue ? id.Value : 0d;
    
    // If the id is omitted, we try to parse it from the org (for DevDiv's super-short 
    // format) or from the project, and default the project to == org further down (or both to DevDiv)
    if (id == null && !(double.TryParse(org, out parsed) || double.TryParse(project, out parsed)))
    {
        new TelemetryClient(TelemetryConfiguration.Active).TrackEvent(
            "docs", new Dictionary<string, string>
            {
                { "url", req.Host + req.Path + req.QueryString },
                { "redirect", "https://github.com/kzu/azdo#work-items" },
                { "org", org },
                { "project", project },
                { "id", id?.ToString() },
            });

        return new RedirectResult("https://github.com/kzu/azdo#work-items");
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

    var location = $"https://dev.azure.com/{org}/{project}/_workitems/edit/{parsed}";
    new TelemetryClient(TelemetryConfiguration.Active).TrackEvent(
        "redirect", new Dictionary<string, string>
        {
            { "url", req.Host + req.Path + req.QueryString },
            { "redirect", location },
            { "org", org },
            { "project", project },
            { "id", parsed.ToString() },
        });

    return new RedirectResult(location);
}