using ContainerRfBot.API.Extensions;
using ContainerRfBot.Bot;
using ContainerRfBot.Bot.Services.MaxBot;
using ContainerRfBot.Infrastructure.DAL.DbContext;
using Max.Bot;
using Max.Bot.Polling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCoreServices();
builder.Services.AddBotLayer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Запускаем polling для Max.Bot
var maxClient = app.Services.GetRequiredService<MaxClient>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

var maxHandler = new DelegatingUpdateHandler(
    onMessage: async (updateContext, ct) =>
    {
        try
        {
            // Важно создавать Scope для каждого сообщения, так как DbContext не потокобезопасен
            using var scope = app.Services.CreateScope();
            var maxBotService = scope.ServiceProvider.GetRequiredService<MaxBotService>();
            await maxBotService.HandleUpdateAsync(updateContext.Update, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling Max bot update");
        }
    }
);

using var maxCts = new CancellationTokenSource();
_ = Task.Run(async () =>
{
    try
    {
        logger.LogInformation("Starting Max bot in polling mode...");
        await maxClient.StartPollingAsync(maxHandler, cancellationToken: maxCts.Token);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Max bot polling crashed");
    }
}, maxCts.Token);

app.Run();