<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.EnterpriseServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.RegularExpressions.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Design.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.ApplicationServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.Protocols.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.Services.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.Build.Utilities.v4.0.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Caching.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.Build.Framework.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.Build.Tasks.v4.0.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
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
    Assert.Equal(
        "https://dev.azure.com/foo/oss/_wiki/wikis/oss.wiki?pagePath=%2FMy%20%26%20Folder%2FMy%252DPage",
        Run<RedirectResult>("http://wiki.azdo.io/My_&_Folder/My-Page?u", "foo", "oss").Url);
        
    Assert.IsType<RedirectResult>(Run("http://wiki.azdo.io/MyPage", "foo", "oss"));
    Assert.Equal(
        "https://dev.azure.com/foo/oss/_wiki/wikis/oss.wiki?pagePath=%2FMyPage",
        Run<RedirectResult>("http://wiki.azdo.io/MyPage", "foo", "oss").Url);

    // If no parameters provided, go to docs
    Assert.IsType<RedirectResult>(Run("http://wiki.azdo.io/"));
    Assert.Equal("https://github.com/kzu/azdo#wiki", Run<RedirectResult>("http://wiki.azdo.io/").Url);
    // If project missing, go to docs
    Assert.IsType<RedirectResult>(Run("http://wiki.azdo.io/", "foo"));
    Assert.Equal("https://github.com/kzu/azdo#wiki", Run<RedirectResult>("http://wiki.azdo.io/", "foo").Url);
}

static TActionResult Run<TActionResult>(string url, string org = null, string project = null)
    => (TActionResult)Run(url, org, project);

static IActionResult Run(string url, string org = null, string project = null)
{
    var uri = new Uri(url);
    var path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).TrimStart('/');
    var query = uri.GetComponents(UriComponents.Query, UriFormat.Unescaped).TrimStart('?');
    return Run(
        new DefaultHttpRequest(new DefaultHttpContext())
        {
            Host = new HostString(uri.Host),
            Path = path.Length == 0 ? PathString.Empty : new PathString("/" + path),
            QueryString = query.Length == 0 ? QueryString.Empty : new QueryString("?" + query)
        },
        new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Console"),
        org, project);
}

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
    
    // Finally, URL-encode the path before making up the final URL
    var escaped = Uri.EscapeDataString(path).Replace("-", "%252D");
    var location = $"https://dev.azure.com/{org}/{project}/_wiki/wikis/{project}.wiki?pagePath={escaped}";

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