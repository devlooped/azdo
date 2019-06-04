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
    Assert.Equal("https://github.com/kzu/azdo#wiki", Run<RedirectResult>().Url);
    // If project missing, go to docs
    Assert.IsType<RedirectResult>(Run("foo"));
    Assert.Equal("https://github.com/kzu/azdo#wiki", Run<RedirectResult>("foo").Url);

    Assert.IsType<RedirectResult>(Run("foo", "oss"));
}

static TActionResult Run<TActionResult>(string org = null, string project = null)
    => (TActionResult)Run(org, project);

static IActionResult Run(string org = null, string project = null)
    => Run(
        new DefaultHttpRequest(new DefaultHttpContext()), 
        new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Console"), 
        org, project);

public static IActionResult Run(HttpRequest req, ILogger log, string org = null, string project = null)
{
    if (req.Host.Value == "wiki.devdiv.io")
        org = project = "DevDiv";

    if (string.IsNullOrEmpty(org) || string.IsNullOrEmpty(project))
    {
        new TelemetryClient(TelemetryConfiguration.Active).TrackEvent(
            "docs", new Dictionary<string, string>
            {
                { "url", req.Host + req.Path + req.QueryString },
                { "redirect", "https://github.com/kzu/azdo#wiki" },
                { "org", org },
                { "project", project },
            });

        return new RedirectResult("https://github.com/kzu/azdo#wiki");
    }

    var path = req.Path.Value.Replace($"/{org}/{project}", "/");
    if (!req.QueryString.HasValue)
    {
        // Default mode is to replace dashes with spaces
        path = path.Replace('-', ' ');
    } 
    else if (req.QueryString.Value == "?u")
    {
        path = path.Replace('_', ' ');
    }
    var location = $"https://dev.azure.com/{org}/{project}/_wiki/wikis/{project}.wiki?pagePath={path}";

    new TelemetryClient(TelemetryConfiguration.Active).TrackEvent(
        "redirect", new Dictionary<string, string>
        {
            { "url", req.Host + req.Path + req.QueryString },
            { "redirect", location },
            { "org", org },
            { "project", project },
        });

    return new RedirectResult(location);
}