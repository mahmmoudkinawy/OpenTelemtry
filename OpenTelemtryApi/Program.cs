using Bogus;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemtryApi.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder
	.Services.AddOpenTelemetry()
	.ConfigureResource(resource =>
		resource
			.AddService(serviceName: builder.Environment.ApplicationName, serviceVersion: "1.0.0", serviceInstanceId: Environment.MachineName)
			.AddAttributes(
				[
					// Deployment info
					new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName),
					new KeyValuePair<string, object>("cloud.provider", "azure"),
					new KeyValuePair<string, object>("cloud.region", "eastus"),
					new KeyValuePair<string, object>("cloud.zone", "eastus-1"),
					// Kubernetes info (example values, replace with dynamic if needed)
					new KeyValuePair<string, object>("k8s.cluster.name", "prod-cluster"),
					new KeyValuePair<string, object>("k8s.namespace.name", "ecommerce"),
					new KeyValuePair<string, object>("k8s.pod.name", Environment.MachineName),
					// Host info
					new KeyValuePair<string, object>("host.name", Environment.MachineName),
					new KeyValuePair<string, object>("host.arch", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()),
					new KeyValuePair<string, object>("host.os.name", System.Runtime.InteropServices.RuntimeInformation.OSDescription),
					// Custom tags for filtering
					new KeyValuePair<string, object>("team", "backend"),
					new KeyValuePair<string, object>("project", "ecommerce-api"),
					new KeyValuePair<string, object>("feature_flag", "checkout-redesign"),
					new KeyValuePair<string, object>("release_channel", "stable")
				]
			)
	)
	.WithTracing(trace => trace.AddHttpClientInstrumentation().AddAspNetCoreInstrumentation().AddNpgsql())
	.WithMetrics(metrics => metrics.AddHttpClientInstrumentation().AddAspNetCoreInstrumentation().AddMeter("MyApp.Metrics"))
	.UseOtlpExporter();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	using var scope = app.Services.CreateScope();
	var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	await dbContext.Database.MigrateAsync();
	app.Logger.LogInformation("Applying pending migrations with some seeding");

	if (!dbContext.Products.Any())
	{
		var faker = new Faker<Product>()
			.RuleFor(p => p.Name, f => f.Commerce.ProductName())
			.RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price()))
			.RuleFor(p => p.InStock, f => f.Random.Bool())
			.RuleFor(p => p.CreatedAt, f => f.Date.Past());

		var products = faker.Generate(50);

		dbContext.Products.AddRange(products);
		await dbContext.SaveChangesAsync();
	}
}

app.UseAuthorization();

app.MapControllers();

app.Run();
