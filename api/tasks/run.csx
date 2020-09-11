using System.Net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

public static IActionResult Run(HttpRequest req, ILogger log, string org = null, string project = null)
{
    if (req.Host.Value == "tasks.devdiv.io")
        org = project = "DevDiv";

    if (string.IsNullOrEmpty(org) && string.IsNullOrEmpty(project))
    {
        new TelemetryClient(TelemetryConfiguration.Active).TrackEvent(
            "docs", new Dictionary<string, string>
            {
                { "url", req.Host + req.Path + req.QueryString },
                { "redirect", "https://github.com/kzu/azdo#task-groups" },
                { "org", org },
                { "project", project },
            });

        return new RedirectResult("https://github.com/kzu/azdo#task-groups");
    }

    if (string.IsNullOrEmpty(project))
        project = org;

    new TelemetryClient(TelemetryConfiguration.Active).TrackEvent(
        "redirect", new Dictionary<string, string>
        {
            { "url", req.Host + req.Path + req.QueryString },
            { "redirect", $"https://dev.azure.com/{org}/{project}/_taskgroups" },
            { "org", org },
            { "project", project },
        });

    return new RedirectResult($"https://dev.azure.com/{org}/{project}/_taskgroups");
}