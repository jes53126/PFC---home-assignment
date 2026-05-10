namespace MenuCloudApp.Services;

public sealed record RestaurantSummary(string Id, string Name, string Status);

public sealed record MenuSearchResult(
    string RestaurantId,
    string RestaurantName,
    string MenuId,
    string ItemName,
    decimal Price,
    string Currency,
    string OcrText,
    string Status);

public sealed record UploadResult(
    string RestaurantId,
    string MenuId,
    string ImageId,
    string ObjectName,
    string PublicStatus);
