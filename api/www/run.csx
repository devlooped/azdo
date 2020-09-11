using System.Net;
using Microsoft.AspNetCore.Mvc;

public static IActionResult Run(HttpRequest req) =>
    req.Host.Host == "browser.azdo.io" ? 
    new RedirectResult("https://chrome.google.com/webstore/detail/azdo-linkinator/copkjnnnmemkbfmolfacgmfiecfjopkk", true) :   
    new RedirectResult("https://www.azdo.io", true);