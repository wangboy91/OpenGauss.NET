// See https://aka.ms/new-console-template for more information

// using System.Diagnostics.CodeAnalysis;
// using Microsoft.EntityFrameworkCore;

using OpenGauss.NET;

Console.WriteLine("Hello, World!");

var connString = "Server=localhost;Port=50432;Username=gaussdb;Password=openGauss@123;Database=testdb;Timeout=60;Command Timeout=60";

var connection = new OpenGaussConnection(connString);
await connection.OpenAsync();
await using var cmd = new OpenGaussCommand("SELECT * FROM public.userinfo", connection);
await using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    Console.WriteLine(reader.HasRows);
    Console.WriteLine(reader.FieldCount);
    for (int i = 0; i < reader.FieldCount; i++)
    {
        Console.WriteLine(reader.GetName(i));
        Console.WriteLine(reader.GetValue(i));
    }
}

// await using var ctx = new BlogContext();
// await ctx.Database.EnsureDeletedAsync();
// await ctx.Database.EnsureCreatedAsync();
//
// // Insert a Blog
// ctx.Blogs.Add(new() { Name = "FooBlog" });
// await ctx.SaveChangesAsync();
//
// // Query all blogs who's name starts with F
// var fBlogs = await ctx.Blogs.Where(b => b.Name.StartsWith("F")).ToListAsync();