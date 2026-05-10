namespace MenuCloudApp.Services;

public interface ITranslationCache
{
    Task<string?> GetAsync(string menuId, string itemName, string language);
    Task SetAsync(string menuId, string itemName, string language, string translatedText);
}
