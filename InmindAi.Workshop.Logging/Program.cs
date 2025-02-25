using Bogus;
using HealthChecks.UI.Client;
using InmindAi.Workshop.Logging.Application.Contracts.Dtos;
using InmindAi.Workshop.Logging.Application.Contracts.Services;
using InmindAi.Workshop.Logging.Application.Orders;
using InmindAi.Workshop.Logging.Application.Products;
using InmindAi.Workshop.Logging.Correlation;
using InmindAi.Workshop.Logging.Domain;
using InmindAi.Workshop.Logging.Errors;
using InmindAi.Workshop.Logging.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

#region Serilog Configuration

Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(logEvent => logEvent.Properties.ContainsKey("Password")) // Implementing Serilog Filter to exclude sensitive data such as password.
    .Enrich.WithCorrelationIdHeader("X-Correlation-Id")
    .Enrich.WithCorrelationId()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadName()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("EnvironmentTester", builder.Environment.EnvironmentName) // Enrich from serilog With custom properties.
    .WriteTo.Console()
    .WriteTo.OpenTelemetry(x =>
    {
        // Add Seq url and API key and specify the protocol
        x.Endpoint = builder.Configuration["Seq:ServerUrl"];
        x.Protocol = OtlpProtocol.HttpProtobuf;
        x.Headers = new Dictionary<string, string>()
        {
            ["X-Seq-ApiKey"] = builder.Configuration["Seq:ApiKey"]!
        };
        x.ResourceAttributes = new Dictionary<string, object>()
        {
            ["service.name"] = "LoggingWorkshop",
            ["opentelemetry.enricher"] = "OpenTelemetry" //Enrich using OpenTelemetry
        };
    })
    .CreateLogger();

#endregion

#region Service Registration

builder.Services.AddHttpContextAccessor(); // We need to add this if we want to visualize properties from http Request / Client info I.E: Headers in our case x-correlation-Id
builder.Services.AddSerilog();

builder.Services.AddHealthChecks()
    //.AddCheck<DatabaseHealthCheck>("Database") // I disabled my created database health check since we can use the library.
    .AddSqlite(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "Sqlite database", tags: ["Database"]);

builder.Services.AddHealthChecksUI(setup =>
{
    setup.AddHealthCheckEndpoint("Application", "/health"); //mapping to UI to pull from the health endpoint
})
    .AddInMemoryStorage();

builder.Services.AddCorrelationId(); //This is the custom one created.

// Configuring telemetry with OpenTelemetry

//builder.Logging.ClearProviders();
//builder.Logging.AddOpenTelemetry(x => 
//{
//    x.SetResourceBuilder(ResourceBuilder.CreateEmpty()
//        .AddService("LoggingWorkshop")
//        .AddAttributes(new Dictionary<string, object>()
//        {
//            ["deployment.environment"] = builder.Environment.EnvironmentName
//        }));

//    x.IncludeFormattedMessage = true;
//    x.IncludeScopes = true;
//    x.AddOtlpExporter(exporter =>
//    {
//        exporter.Endpoint = new Uri("http://localhost:5341/ingest/otlp/v1/logs");

//    });
//});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier; //Identifier of the request
    };
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<WorkShopDbContext>(x => x.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
.UseSeeding((context,_) =>
{
    // I Added this seeding here for demo purposes, the proper way to do so is to create another application that seeds the data to avoid concurrency issues
    var hasData = context.Set<Product>().Any();
    if (hasData)
    {
        return;
    }
    var faker = new Faker<Product>()
        .UseSeed(200)
        .CustomInstantiator(f => new Product(f.Commerce.ProductName(), Math.Floor(f.Random.Decimal(1, 1000) * 100) / 100));

    var products = faker.Generate(100);
    var contains = context.Set<Product>().Contains(products[0]);
    if (!contains)
    {
        context.Set<Product>().AddRange(products);
        context.SaveChanges();
    }
}));

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();

#endregion

var app = builder.Build();

using (var serviceScope = app.Services.CreateScope())
{
    using var context = serviceScope.ServiceProvider.GetRequiredService<WorkShopDbContext>();
    context.Database.EnsureCreated();
}

#region Middleware Configuration

app.UseExceptionHandler("/error");

app.Map("/error", (HttpContext httpContext) =>
{
    Exception? exception = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is null)
    {
        return Results.Problem("An error occurred.");
    }

    return exception switch
    {
        ServiceException se => Results.Problem(detail: se.ErrorMessage, statusCode: se.StatusCode),
        _ => Results.Problem()
    };
});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Adding Scalar API reference instead of Swagger.
    app.MapScalarApiReference(opt =>
    {
        opt.WithTitle("Inmind.ai Logging Workshop")
        .WithTheme(ScalarTheme.Mars)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

app.UseCorrelationId(); // customely created

app.MapHealthChecks("/health", new HealthCheckOptions()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
//.RequireHost("*:5001") // In order to restrict the access to the health endpoint, you can use this method.
//.RequireAuthorization(); // Or you can use this method to require authorization.

app.MapHealthChecksUI(options =>
{
    options.UseRelativeApiPath = true;
    options.PageTitle = "Logging Workshop HealthChecks";
    options.UIPath = "/health-ui";
});

#endregion

#region Orders Endpoint

app.MapPost("/orders", async (IOrderService orderService, CreateOrderDto orderDto) =>
{
    var order = await orderService.CreateOrderAsync(orderDto);
    return Results.Ok(order);
})
    .WithName("CreateOrder");

app.MapGet("/orders", async (IOrderService orderService) =>
{
    var orders = await orderService.GetOrdersAsync();
    return Results.Ok(orders);
})
    .WithName("GetOrders");

app.MapGet("/orders/{id}", async (IOrderService orderService, Guid id) =>
{
    var order = await orderService.GetOrderAsync(id);
    return Results.Ok(order);
})
    .WithName("GetOrderById");

app.MapPut("/orders/{id}", async (IOrderService orderService, Guid id, IEnumerable<OrderLineDto> orderLines) =>
{
    var order = await orderService.UpdateOrderAsync(id, orderLines);
    return Results.Ok(order);
})
    .WithName("UpdateOrder");

app.MapDelete("/orders/{id}", async (IOrderService orderService, Guid id) =>
{
    await orderService.DeleteOrderAsync(id);
    return Results.NoContent();
})
    .WithName("DeleteOrder");

#endregion

#region Products Endpoint

app.MapGet("/products", async (IProductService productService) =>
{
    var products = await productService.GetProductsAsync();
    return Results.Ok(products);
})
    .WithName("GetProducts");

app.MapGet("/products/{id}", async (IProductService productService, Guid id) =>
{
    var product = await productService.GetProductAsync(id);
    return Results.Ok(product);
})
    .WithName("GetProductById");

#endregion

app.Run();
