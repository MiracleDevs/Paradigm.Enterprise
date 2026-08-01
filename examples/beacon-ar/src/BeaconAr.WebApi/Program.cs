using BeaconAr.Data.Receivables;
using Microsoft.Extensions.Hosting;
using Paradigm.Enterprise.Data.SqlServer.Context;
using Paradigm.Enterprise.Data.SqlServer.Extensions;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Uow;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddHealthChecks().AddSqlServer(
    builder.Configuration.GetConnectionString("DatabaseConnection") ?? throw new InvalidOperationException("DatabaseConnection is required."),
    name: "database",
    timeout: TimeSpan.FromSeconds(3),
    tags: ["ready"]);
builder.Services.AddScoped<SqlServerDbContextConnectionProvider>();
builder.Services.RegisterContext<ReceivablesDbContext>("DatabaseConnection");
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();
app.UseHttpsRedirection();
app.MapDefaultEndpoints();
app.MapControllers();
app.Run();
