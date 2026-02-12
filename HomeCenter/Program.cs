using HomeCenter.Data;
using HomeCenter.Extensions;
using HomeCenter.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddQuizServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    DatabaseMigrator.EnsureVersioningSchema(db);

    var testService = scope.ServiceProvider.GetRequiredService<ITestFileService>();
    testService.SyncTopicsFromFiles();
}

app.UseQuizPipeline();

app.Run();
