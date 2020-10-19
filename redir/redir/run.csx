using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

public static IActionResult Run(HttpRequest req, string path)
    => path?.Length == 0 && req.QueryString.HasValue ?
        new RedirectResult($"https://api.azdo.io/{req.Headers["DISGUISED-HOST"].ToString().Split('.').First()}") :
        new RedirectResult($"https://api.azdo.io/{req.Headers["DISGUISED-HOST"].ToString().Split('.').First()}/{path}{req.QueryString}");