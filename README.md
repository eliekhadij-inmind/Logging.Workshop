# Running Seq Locally with Docker

## 1. Pull the Seq Docker Image
To pull the latest Seq image from Docker Hub, run:

```sh
docker pull datalust/seq
```

## 2. Run Seq Container Locally
Run Seq in a Docker container with persistent storage:

```sh
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 -v seq-data:/data datalust/seq
```

- `-d` runs the container in detached mode.
- `--name seq` names the container `seq`.
- `-e ACCEPT_EULA=Y` accepts the Seq EULA.
- `-p 5341:80` maps port 5341 on your machine to port 80 in the container.
- `-v seq-data:/data` creates a persistent volume for logs.

## 3. Access Seq
Once the container is running, open your browser and navigate to:

```
http://localhost:5341
```

## 4. Generate an API Key in Seq
To generate an API key:

1. Open Seq in your browser (`http://localhost:5341`).
2. Go to **Settings** (⚙️ in the top right corner).
3. Navigate to **API Keys**.
4. Click **+ New API Key**.
5. Enter a name for the key and configure permissions as needed.
6. Click **Save** to generate the key.
7. Copy and store the API key securely (Once generated you wont see it again so make sure you copy it).

## 5. Use the API Key
Include the API key in requests using the `X-Seq-ApiKey` header:

# Web Api 

## Configuring Serilog with OpenTelemetry and Sensitive Data Filtering
### Needed Libraries
1. `Serilog.AspNetCore`
2. `Serilog.Extensions.Logging`
3. `Serilog.Sinks.OpenTelemetry`
### Setting Up Serilog Logger
To configure Serilog and filter out sensitive data such as passwords, use the following setup:
1. Add Seq server url
2. Add the previously copied ApiKey in Header "X-Seq-ApiKey"

```csharp
Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(logEvent => logEvent.Properties.ContainsKey("Password")) // Exclude sensitive data
    .WriteTo.Console()
    .WriteTo.OpenTelemetry(x =>
    {
        // Add Seq URL and API key, specifying the protocol
        x.Endpoint = builder.Configuration["Seq:ServerUrl"];
        x.Protocol = OtlpProtocol.HttpProtobuf;
        x.Headers = new Dictionary<string, string>()
        {
            ["X-Seq-ApiKey"] = builder.Configuration["Seq:ApiKey"]!
        };
        x.ResourceAttributes = new Dictionary<string, object>()
        {
            ["service.name"] = "LoggingWorkshop",
            ["deployment.environment"] = builder.Environment.EnvironmentName
        };
    })
    .CreateLogger();
```

### Registering Serilog as a Service
Add Serilog to the services in `Program.cs`:

```csharp
builder.Services.AddSerilog();
```

## Setting Up Global Exception Handling 

### Adding Problem Details for API Responses
Configure Problem Details to enhance error handling:

```csharp
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? ctx.TraceIdentifier;
    };
});
```

### Configuring Middleware for Error Handling
Add middleware for error handling:

```csharp
app.UseExceptionHandler("/error");
app.Map("/error", (HttpContext httpContext) =>
{
    Exception? exception = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
    return exception switch
    {
        ServiceException se => Results.Problem(se.ErrorMessage, statusCode: se.StatusCode),
        _ => Results.Problem()
    };
});
```

## Database
Im using here Sqlite database

### Seeding
This code demonstrates how to seed the database if it is empty, I used Bogus here to generate some data.
This is just for demo purposes it is recommended to create seeders in seperate application to avoid concurrency issues.
I created a service scope and ensured that db is created in order to launch UseSeedingAsync.
To note that you can use UseSeeding() which is for synchronous seeding and UseAsyncSeeding() for asynchronous.

For some reason I had troubles with UseAsyncSeeding() when I regenerated the migrations. So I made it synchronous for demo purposes

I will show you both async and sync ways.


```csharp
builder.Services.AddDbContext<WorkShopDbContext>(x => x.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
.UseAsyncSeeding(async (context, _, ct) =>
{
    var hasData = await context.Set<Product>().AnyAsync(cancellationToken: ct);
    if (hasData) return;

    var faker = new Faker<Product>()
        .UseSeed(200)
        .CustomInstantiator(f => new Product(f.Commerce.ProductName(), Math.Floor(f.Random.Decimal(1, 1000) * 100) / 100));

    var products = faker.Generate(100);
    if (!await context.Set<Product>().ContainsAsync(products[0], cancellationToken: ct))
    {
        await context.Set<Product>().AddRangeAsync(products, ct);
        await context.SaveChangesAsync(ct);
    }
}));

var app = builder.Build();

await using (var serviceScope = app.Services.CreateAsyncScope())
{
    await using var context = serviceScope.ServiceProvider.GetRequiredService<WorkShopDbContext>();
    await context.Database.EnsureCreatedAsync();
}

```
```csharp
builder.Services.AddDbContext<WorkShopDbContext>(x => x.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
.UseSeeding((context,_) =>
{
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

var app = builder.Build();

using (var serviceScope = app.Services.CreateScope())
{
    using var context = serviceScope.ServiceProvider.GetRequiredService<WorkShopDbContext>();
    context.Database.EnsureCreated();
}

```

### Handling Database common exception

I used `EntityFrameworkCore.Exceptions.Sqlite` library in order to illustrate the exceptions returned from EfCore since EfCore always returns UpdateExceptions with inner exceptions illustrating the db that we are using.
In order to use the library in our code we simply do the following in our db context by overriding `OnConfiguring` method :

```csharp
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseExceptionProcessor();
        base.OnConfiguring(optionsBuilder);
    }

```


## Setting Up OpenAPI with Scalar (Good to know since in .Net 9 they removed swagger)
Enable OpenAPI and HTTPS redirection in development:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opt =>
    {
        opt.WithTitle("Inmind.ai Logging Workshop")
           .WithTheme(ScalarTheme.Mars)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();
```