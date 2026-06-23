using CardGames.BeggarMyNeighbour.Scoreboard.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

// Typed client that reads the leaderboard from the Scoreboard API.
var apiBaseUrl = builder.Configuration["ScoreboardApi:BaseUrl"] ?? "http://scoreboard.api";
builder.Services.AddHttpClient<ScoreboardClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Named client for the RabbitMQ management API (basic auth: guest/guest).
builder.Services.AddHttpClient("rabbitmq", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
    var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("guest:guest"));
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapRazorPages();

app.Run();
