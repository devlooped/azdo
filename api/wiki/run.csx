using System.Net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

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

    // Detect new URL format for new Wiki ([project].wiki/[id]/[page])
    var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
    if (double.TryParse(parts[0], out var pageId))
    {
        var location = $"https://dev.azure.com/{org}/{project}/_wiki/wikis/{project}.wiki{path}";

        new TelemetryClient(TelemetryConfiguration.Active).TrackEvent(
            "redirect", new Dictionary<string, string>
            {
            { "url", req.Host + req.Path },
            { "redirect", location },
            { "org", org },
            { "project", project },
            });

        return new RedirectResult(location);
    }
    else
    {
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
}