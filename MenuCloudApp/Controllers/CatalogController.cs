using MenuCloudApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuCloudApp.Controllers;

[Authorize]
public sealed class CatalogController : Controller
{
    private readonly IMenuCloudService _menus;

    public CatalogController(IMenuCloudService menus)
    {
        _menus = menus;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string q = "", string sort = "asc", CancellationToken cancellationToken = default)
    {
        ViewData["Query"] = q;
        ViewData["Sort"] = sort;
        return View(await _menus.SearchAsync(q, sort, cancellationToken));
    }
}
