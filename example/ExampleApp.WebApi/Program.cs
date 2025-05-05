using ExampleApp.Data.Inventory.Contexts;
using ExampleApp.WebApi.Exceptions.Handlers.Resources;
using ExampleApp.WebApi.Exceptions.Handlers;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.WebApi.Exceptions.Handlers;
using Paradigm.Enterprise.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext using an in-memory database for this example
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("ExampleAppDb"));
builder.Services.AddScoped<IExceptionHandler, ExceptionHandler>(_ =>
{
    var exceptionHandler = new ExceptionHandler(typeof(Exceptions));

    exceptionHandler.AddMatcher(new UniqueKeyExceptionMatcher());
    exceptionHandler.AddMatcher(new ForeignKeyExceptionMatcher());

    return exceptionHandler;
});

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

app.UseOwnExceptionHandler();
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
