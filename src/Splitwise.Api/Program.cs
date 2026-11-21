using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Splitwise.Api.ExceptionHandling;
using Splitwise.Api.Extensions;
using Splitwise.Api.Filters;
using Splitwise.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Render (та інші хостинги) передають порт через змінну PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddInfrastructure(builder.Configuration);   // DbContext, репозиторії, UnitOfWork
builder.Services.AddApplicationServices();                   // сервіси бізнес-логіки + валідатори
builder.Services.AddIdentityAuth();                          // Microsoft Identity + Bearer-токени
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddSwaggerDocs();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());

// Єдина обробка помилок: винятки → ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

// HTTPS-редірект обрубував заголовки запитів з авторизацією, тому лише поза Development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicies.Frontend);
app.UseAuthentication();   // хто ти
app.UseAuthorization();    // що тобі можна

app.MapGroup("/api/auth").MapIdentityApi<ApplicationUser>();   // /register, /login, /refresh ...
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Створюємо/оновлюємо схему БД під час старту
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.GetMigrations().Any())
        db.Database.Migrate();        // є міграції → застосовуємо їх
    else
        db.Database.EnsureCreated();  // міграцій ще немає → створюємо схему за моделлю
}

app.Run();

public partial class Program;
