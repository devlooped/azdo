using System.Net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

public static IActionResult Run(HttpRequest req, ILogger log, string path)
{
    var parts = path.Split('/');
    var org = parts.FirstOrDefault();
    var project = parts.Skip(1).FirstOrDefault();
    double? id = parts.Skip(2).Select(x => double.TryParse(x, out var value) ? value : default(double?)).FirstOrDefault();

    var parsed = id.HasValue ? id.Value : 0d;
    
    // If the id is omitted, we try to parse it from the org (for DevDiv's super-short 
    // format) or from the project, and default the project to == org further down (or both to DevDiv)
    if (id == null && !(double.TryParse(org, out parsed) || double.TryParse(project, out parsed)))
    {
        new TelemetryClient(TelemetryConfiguration.Active).TrackEvent(
            "docs", new Dictionary<string, string>
            {
                { "url", req.Host + req.Path + req.QueryString },
                { "redirect", "https://github.com/kzu/azdo#builds"},
                { "org", org },
                { "project", project },
                { "id", id?.ToString() },
            });
        
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
        if (org.Equals("devdiv", StringComparison.OrdinalIgnoreCase) && project.Equals("devdiv", StringComparison.OrdinalIgnoreCase) && parsed < 1000000)
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

    var location = def 
        ? $"https://dev.azure.com/{org}/{project}/_build?definitionId={parsed}" 
        : $"https://dev.azure.com/{org}/{project}/_build/results?buildId={parsed}";

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