using Microsoft.EntityFrameworkCore;

namespace OpenTelemtryApi.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<Product> Products => Set<Product>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
	}
}

public class Product
{
	public int Id { get; set; }

	public string Name { get; set; } = default!;

	public decimal Price { get; set; }

	public bool InStock { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}
