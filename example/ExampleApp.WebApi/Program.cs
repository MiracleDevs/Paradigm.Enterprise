using ExampleApp.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext using an in-memory database for this example
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("ExampleAppDb"));

// Register the Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.RegisterRepositories()
    .RegisterProviders()
    .RegisterServices([])
    .RegisterMappers()
    .RegisterEntities()
    .RegisterDtos();

// build the app
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Initialize the database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // This will trigger the creation of the database and the seed data
    dbContext.Database.EnsureCreated();
}

app.Run();
