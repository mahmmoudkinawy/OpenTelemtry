using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemtryApi.Database;

namespace OpenTelemtryApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
	private readonly Meter _meter;
	private readonly Counter<int> _requestCounter;

	private readonly AppDbContext _appDbContext;

	public TestController(AppDbContext appDbContext)
	{
		_appDbContext = appDbContext;
		_meter = new("MyApp.Metrics", "1.0");
		_requestCounter = _meter.CreateCounter<int>("api_request_count");
	}

	[HttpGet("products")]
	public IActionResult GetProducts()
	{
		_requestCounter.Add(1, KeyValuePair.Create<string, object?>("endpoint", "GET /products"));

		return Ok(_appDbContext.Products.ToArray());
	}

	[HttpGet]
	public IActionResult Get()
	{
		return Ok("Hello from OpenTelemetry!");
	}

	[HttpGet("fail")]
	public IActionResult Fail()
	{
		throw new Exception("Oops! Something broke.");
	}
}
