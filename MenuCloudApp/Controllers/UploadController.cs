using MenuCloudApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuCloudApp.Controllers;

[Authorize]
public sealed class UploadController : Controller
{
    private readonly IMenuCloudService _menus;

    public UploadController(IMenuCloudService menus)
    {
        _menus = menus;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload(string restaurantName, List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(restaurantName))
        {
            return BadRequest(new { error = "Restaurant name is required." });
        }

        if (files.Count == 0)
        {
            return BadRequest(new { error = "Select at least one menu image." });
        }

        var user = User.Identity?.Name ?? "unknown";
        var results = new List<UploadResult>();
        foreach (var file in files)
        {
            results.Add(await _menus.UploadMenuImageAsync(restaurantName, file, user, cancellationToken));
        }

        return Json(new { message = "Upload completed. Processing has been queued.", results });
    }
}
