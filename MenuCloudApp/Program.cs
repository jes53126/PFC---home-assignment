using Google.Cloud.Firestore;
using Google.Cloud.PubSub.V1;
using Google.Cloud.SecretManager.V1;
using Google.Cloud.Storage.V1;
using MenuCloudApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CloudMenuOptions>(builder.Configuration.GetSection("CloudMenu"));
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSingleton(StorageClient.Create());
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CloudMenuOptions>>().Value;
    return FirestoreDb.Create(options.ProjectId);
});
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CloudMenuOptions>>().Value;
    return PublisherClient.Create(new Google.Cloud.PubSub.V1.TopicName(options.ProjectId, options.MenuUploadsTopic));
});
builder.Services.AddSingleton<IMenuCloudService, MenuCloudService>();
builder.Services.AddSingleton<ITranslationCache, RedisTranslationCache>();

var redisConnection = builder.Configuration["CloudMenu:RedisConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
}

var googleClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
var secretName = builder.Configuration["Authentication:Google:ClientSecretSecretName"];
if (!string.IsNullOrWhiteSpace(secretName))
{
    try
    {
        var secretManager = SecretManagerServiceClient.Create();
        var secret = secretManager.AccessSecretVersion(secretName);
        googleClientSecret = secret.Payload.Data.ToStringUtf8();
    }
    catch
    {
        // Local development can use user-secrets/appsettings instead of Secret Manager.
    }
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
