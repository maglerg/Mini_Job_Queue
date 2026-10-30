using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.Queue;
using Mini_Job_Queue.API.Services;
using Mini_Job_Queue.API.Services.LogAnalysis;
using Mini_Job_Queue.API.Workers;
using Serilog;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Mini_Job_Queue.API.Services.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            fileSizeLimitBytes: 10 * 1024 * 1024,
            rollOnFileSizeLimit: true,
            outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}"
        );
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration["Redis:ConnectionString"];

    options.InstanceName = "MiniJobQueue:";
});

builder.Services.AddSingleton<IJobQueue, JobQueue>();

const int workerCount = 4;

for (int i = 0; i < workerCount; i++)
{
    builder.Services.AddSingleton<IHostedService>(serviceProvider =>
        new JobWorker(
            serviceProvider.GetRequiredService<IJobQueue>(),
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            serviceProvider.GetRequiredService<ILogger<JobWorker>>()
        ));
}

builder.Services.AddControllers();

builder.Services.AddHttpClient();
builder.Services.AddScoped<UrlCheckService>();
builder.Services.AddScoped<JobsService>();
builder.Services.AddSingleton<LinuxLogParser>();
builder.Services.AddSingleton<LinuxLogClassifier>();
builder.Services.AddSingleton<LogAnalysisService>();
builder.Services.AddSingleton<AutomationScheduleSignal>();

builder.Services.AddScoped<AutomationScheduleNotifier>();

builder.Services.AddHostedService<PostgresScheduleNotificationListener>();
builder.Services.AddHostedService<AutomationScheduler>();
builder.Services.AddScoped<AutomationRulesService>();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();


// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
