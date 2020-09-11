using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

public static IActionResult Run(HttpRequest req, ILogger log, string path)
    => new RedirectResult($"https://api.azdo.io/{req.Host.Host.Split('.').First()}/{path.Substring(6)}");