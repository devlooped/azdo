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
    // build
    Assert.Equal("https://api.azdo.io/build/devlooped/azdo/123", Run<RedirectResult>("build", "/devlooped/azdo/123").Url);
    
    // b => build
    Assert.Equal("https://api.azdo.io/build/devlooped/azdo/123", Run<RedirectResult>("b", "/devlooped/azdo/123").Url);
    // r => release
    Assert.Equal("https://api.azdo.io/release/devlooped/azdo/123", Run<RedirectResult>("r", "/devlooped/azdo/123").Url);
    // w => wiki
    Assert.Equal("https://api.azdo.io/wiki/devlooped/azdo/123", Run<RedirectResult>("w", "/devlooped/azdo/123").Url);
    // i => work
    Assert.Equal("https://api.azdo.io/work/devlooped/azdo/123", Run<RedirectResult>("i", "/devlooped/azdo/123").Url);
    // t => tasks
    Assert.Equal("https://api.azdo.io/tasks/", Run<RedirectResult>("t", "/").Url);
}

static TActionResult Run<TActionResult>(string subdomain, string path) => (TActionResult)Run(subdomain, path);

static IActionResult Run(string subdomain, string path)
{
    var request = new DefaultHttpRequest(new DefaultHttpContext());
    request.Headers.Add("DISGUISED-HOST", $"{subdomain}.azdo.io");
    
    var services = new ServiceCollection().AddLogging(builder => builder.AddConsole());

    return Run(request, services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("Console"), path);
}

public static IActionResult Run(HttpRequest req, ILogger logger, string path)
{
    var host = req.Headers["DISGUISED-HOST"].ToString().Split('.').First();
    if (host == "b")
        host = "build";
    else if (host == "r")
        host = "release";
    else if (host == "w")
        host = "wiki";
    else if (host == "i")
        host = "work";
    else if (host == "t")
        host = "tasks";
    
    var target = path?.Length == 0 && req.QueryString.HasValue ?
        $"https://api.azdo.io/{host}" : 
        $"https://api.azdo.io/{host}/{path.TrimStart('/')}{req.QueryString}";

    logger.LogInformation($"{req.Path}{req.QueryString} -> {target}");

    return new RedirectResult(target);
}