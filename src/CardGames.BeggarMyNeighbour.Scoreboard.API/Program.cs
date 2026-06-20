/* Copyright (c) 2017 Oliver Sanders

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CardGames.BeggarMyNeighbour.Scoreboard.API;
using CardGames.BeggarMyNeighbour.Scoreboard.API.Services;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var conn = builder.Configuration.GetConnectionString("ScoreBoardDatabase");
builder.Services.AddDbContextPool<ScoreBoardContext>(options => options.UseSqlite(conn));

builder.Services.AddSingleton<ThresholdService>();
builder.Services.AddSingleton<IConnectionFactory>(new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMq:HostName"] ?? "beggareventbus",
    AutomaticRecoveryEnabled = true
});
builder.Services.AddSingleton<IVerifyService, VerifyService>();

builder.Services.AddHttpClient("upstream", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<UpstreamScoreboardService>();

// Allow the web front end (and any other origin) to read the scoreboard.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Limit score submissions to 30 per 10 seconds to guard against runaway workers.
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("post-scores", o =>
    {
        o.Window = TimeSpan.FromSeconds(10);
        o.PermitLimit = 30;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 5;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Ensure the database schema is up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ScoreBoardContext>();
    db.Database.Migrate();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All,
    RequireHeaderSymmetry = false,
    ForwardLimit = 2
});

app.UseCors();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapControllers();

// Eagerly start the verification listener so verify responses are processed.
app.Services.GetService<IVerifyService>();

app.Run();
