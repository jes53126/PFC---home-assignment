using System.Net.Http.Json;
using MenuCloudApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MenuCloudApp.Controllers;

[Authorize]
public sealed class TranslationController : Controller
{
    private readonly ITranslationCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CloudMenuOptions _options;

    public TranslationController(ITranslationCache cache, IHttpClientFactory httpClientFactory, IOptions<CloudMenuOptions> options)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Translate(string menuId, string itemName, string language)
    {
        var cached = await _cache.GetAsync(menuId, itemName, language);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return Json(new { translatedText = cached, source = "cache" });
        }

        if (string.IsNullOrWhiteSpace(_options.TranslationFunctionUrl))
        {
            return BadRequest(new { error = "TranslationFunctionUrl is not configured." });
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        var response = await client.PostAsJsonAsync(_options.TranslationFunctionUrl, new
        {
            text = itemName,
            targetLanguage = language
        });
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync();
            return StatusCode(502, new { error = $"Translation function failed: {(int)response.StatusCode} {details}" });
        }

        var payload = await response.Content.ReadFromJsonAsync<TranslationResponse>();
        var translated = payload?.TranslatedText ?? itemName;
        await _cache.SetAsync(menuId, itemName, language, translated);

        return Json(new { translatedText = translated, source = "function" });
    }

    private sealed record TranslationResponse(string TranslatedText);
}
