using System.Text.Json;
using Google.Cloud.Firestore;
using Google.Cloud.PubSub.V1;
using Google.Cloud.Storage.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace MenuCloudApp.Services;

public sealed class MenuCloudService : IMenuCloudService
{
    private readonly FirestoreDb _firestore;
    private readonly StorageClient _storage;
    private readonly PublisherClient _publisher;
    private readonly CloudMenuOptions _options;

    public MenuCloudService(FirestoreDb firestore, StorageClient storage, PublisherClient publisher, IOptions<CloudMenuOptions> options)
    {
        _firestore = firestore;
        _storage = storage;
        _publisher = publisher;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<RestaurantSummary>> GetRestaurantsAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _firestore.Collection("restaurants").OrderBy("name").GetSnapshotAsync(cancellationToken);
        return snapshot.Documents
            .Select(d => new RestaurantSummary(d.Id, d.GetValue<string>("name"), d.ContainsField("status") ? d.GetValue<string>("status") : "pending"))
            .ToList();
    }

    public async Task<UploadResult> UploadMenuImageAsync(string restaurantName, IFormFile file, string uploadedBy, CancellationToken cancellationToken)
    {
        var restaurantId = Slug(restaurantName);
        var menuId = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var imageId = Guid.NewGuid().ToString("N");
        var objectName = $"restaurants/{restaurantId}/menus/{menuId}/images/{imageId}{Path.GetExtension(file.FileName)}";

        await using var stream = file.OpenReadStream();
        await _storage.UploadObjectAsync(_options.BucketName, objectName, file.ContentType, stream, cancellationToken: cancellationToken);

        var restaurantRef = _firestore.Collection("restaurants").Document(restaurantId);
        var menuRef = restaurantRef.Collection("menus").Document(menuId);
        var imageRef = menuRef.Collection("images").Document(imageId);

        var now = Timestamp.FromDateTime(DateTime.UtcNow);
        await restaurantRef.SetAsync(new Dictionary<string, object>
        {
            ["name"] = restaurantName.Trim(),
            ["status"] = "pending",
            ["updatedAt"] = now
        }, SetOptions.MergeAll, cancellationToken);

        await menuRef.SetAsync(new Dictionary<string, object>
        {
            ["status"] = "pending",
            ["ocrText"] = "",
            ["items"] = Array.Empty<object>(),
            ["uploadedBy"] = uploadedBy,
            ["createdAt"] = now,
            ["updatedAt"] = now
        }, cancellationToken: cancellationToken);

        await imageRef.SetAsync(new Dictionary<string, object>
        {
            ["bucket"] = _options.BucketName,
            ["objectName"] = objectName,
            ["originalFileName"] = file.FileName,
            ["contentType"] = file.ContentType,
            ["uploadedAt"] = now
        }, cancellationToken: cancellationToken);

        var message = new
        {
            restaurantId,
            menuId,
            imageId,
            bucket = _options.BucketName,
            objectName
        };
        await _publisher.PublishAsync(new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(message)),
            Attributes = { ["type"] = "menu-image-uploaded" }
        });

        return new UploadResult(restaurantId, menuId, imageId, objectName, "pending");
    }

    public async Task<IReadOnlyList<MenuSearchResult>> SearchAsync(string query, string sort, CancellationToken cancellationToken)
    {
        var restaurantSnapshot = await _firestore.Collection("restaurants").GetSnapshotAsync(cancellationToken);
        var results = new List<MenuSearchResult>();

        foreach (var restaurant in restaurantSnapshot.Documents)
        {
            var name = restaurant.ContainsField("name") ? restaurant.GetValue<string>("name") : restaurant.Id;
            var menus = await restaurant.Reference.Collection("menus").WhereEqualTo("status", "completed").GetSnapshotAsync(cancellationToken);
            foreach (var menu in menus.Documents)
            {
                var ocrText = menu.ContainsField("ocrText") ? menu.GetValue<string>("ocrText") : "";
                if (!menu.ContainsField("items"))
                {
                    continue;
                }

                foreach (var item in menu.GetValue<IEnumerable<object>>("items").OfType<Dictionary<string, object>>())
                {
                    var itemName = item.TryGetValue("name", out var rawName) ? rawName?.ToString() ?? "" : "";
                    if (!string.IsNullOrWhiteSpace(query) && !itemName.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    results.Add(new MenuSearchResult(
                        restaurant.Id,
                        name,
                        menu.Id,
                        itemName,
                        Convert.ToDecimal(item.TryGetValue("price", out var rawPrice) ? rawPrice : 0),
                        item.TryGetValue("currency", out var rawCurrency) ? rawCurrency?.ToString() ?? "EUR" : "EUR",
                        ocrText,
                        "completed"));
                }
            }
        }

        return (sort?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
                ? results.OrderByDescending(r => r.Price)
                : results.OrderBy(r => r.Price))
            .ToList();
    }

    private static string Slug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
