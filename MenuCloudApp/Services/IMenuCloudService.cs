using Microsoft.AspNetCore.Http;

namespace MenuCloudApp.Services;

public interface IMenuCloudService
{
    Task<IReadOnlyList<RestaurantSummary>> GetRestaurantsAsync(CancellationToken cancellationToken);
    Task<UploadResult> UploadMenuImageAsync(string restaurantName, IFormFile file, string uploadedBy, CancellationToken cancellationToken);
    Task<IReadOnlyList<MenuSearchResult>> SearchAsync(string query, string sort, CancellationToken cancellationToken);
}
