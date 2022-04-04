using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

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
        $"https://api.azdo.io/{host}/{path}{req.QueryString}";

    logger.LogInformation($"{req.Path}{req.QueryString} -> {target}");

    return new RedirectResult(target);
}