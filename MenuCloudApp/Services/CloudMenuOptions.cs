namespace MenuCloudApp.Services;

public sealed class CloudMenuOptions
{
    public string ProjectId { get; set; } = "your-gcp-project-id";
    public string BucketName { get; set; } = "your-eu-menu-images-bucket";
    public string MenuUploadsTopic { get; set; } = "menu-uploads-topic";
    public string TranslationFunctionUrl { get; set; } = "";
    public string RedisConnectionString { get; set; } = "";
}
