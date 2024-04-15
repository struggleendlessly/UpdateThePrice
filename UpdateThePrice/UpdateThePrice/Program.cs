using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add services to the container.
builder.Services.AddDbContext<ProductContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPut("/update-prices", async (
    List<Product> products,
    ProductContext context) =>
{
    var updateErrors = new List<int>();

    foreach (var product in products)
    {
        try
        {
            // Fetch the product from the database
            var productInDb = await context.Products.FindAsync(product.Id);

            if (productInDb == null)
            {
                updateErrors.Add(product.Id);
                continue;
            }

            // Update the price
            productInDb.Price = product.Price;

            // Save changes to the database
            context.Products.Update(productInDb);
            await context.SaveChangesAsync();
        }
        catch
        {
            updateErrors.Add(product.Id);
        }
    }

    if (updateErrors.Any())
    {
        return Results.BadRequest(new { error = "Failed to update some products", ids = updateErrors });
    }

    return Results.Ok(new { status = "Success" });
}).WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class Product
{
    public int Id { get; set; }
    public decimal Price { get; set; }
}

public class ProductContext : DbContext
{
    public ProductContext(DbContextOptions<ProductContext> options)
    : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
}
