using System.Net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

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
                { "redirect", "https://github.com/kzu/azdo#work-items"},
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