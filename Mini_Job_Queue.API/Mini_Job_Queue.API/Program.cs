using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.Queue;
using Mini_Job_Queue.API.Services;
using Mini_Job_Queue.API.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddSingleton<IJobQueue, JobQueue>();
builder.Services.AddHostedService<JobWorker>();

builder.Services.AddControllers();

builder.Services.AddHttpClient();
builder.Services.AddScoped<UrlCheckService>();
builder.Services.AddScoped<JobsService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Настройка HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();