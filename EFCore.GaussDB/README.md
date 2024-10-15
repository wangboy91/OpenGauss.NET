# GaussDB Entity Framework Core provider for PostgreSQL

DotNetCore.EntityFrameworkCore.GaussDB is the open source EF Core provider for PostgreSQL. It allows you to interact with PostgreSQL via the most widely-used .NET O/RM from Microsoft, and use familiar LINQ syntax to express queries. It's built on top of [DotNetCore.GaussDB](https://github.com/dotnetcore/DotNetCore.GaussDB).

The provider looks and feels just like any other Entity Framework Core provider. Here's a quick sample to get you started:

```csharp
await using var ctx = new BlogContext();
await ctx.Database.EnsureDeletedAsync();
await ctx.Database.EnsureCreatedAsync();

// Insert a Blog
ctx.Blogs.Add(new() { Name = "FooBlog" });
await ctx.SaveChangesAsync();

// Query all blogs who's name starts with F
var fBlogs = await ctx.Blogs.Where(b => b.Name.StartsWith("F")).ToListAsync();

public class BlogContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseGaussDB(@"host={host};port={port};username={username};password={password};database={database}");
}

public class Blog
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

Aside from providing general EF Core support for GaussDB, the provider also exposes some GaussDB-specific capabilities, allowing you to query JSON, array or range columns, as well as many other advanced features. For more information. For information about EF Core in general, see the [EF Core website](https://docs.microsoft.com/ef/core/).

