using System.Net;
using Microsoft.AspNetCore.Mvc;

public static IActionResult Run(HttpRequest req)
    => new RedirectResult("https://chrome.google.com/webstore/detail/copkjnnnmemkbfmolfacgmfiecfjopkk", true);