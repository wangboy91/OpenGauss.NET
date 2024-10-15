// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;
//
// namespace System.Diagnostics.CodeAnalysis;
//
// public class BlogContext : DbContext
// {
//     public DbSet<Blog> Blogs { get; set; }
//
//     public const string ConnString = "Server=localhost;Port=50432;Username=gaussdb;Password=openGauss@123;Database=testdb;Timeout=60;Command Timeout=60";
//     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//         => optionsBuilder.UseGaussDB(@"host=localhost;port=50432;username=gaussdb;password=openGauss@123;database=testdb");
//     
//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         base.OnModelCreating(modelBuilder);
//         modelBuilder.ApplyConfiguration(new BlogConfig());
//     }
//
// }
//
// public class Blog
// {
//     public int Id { get; set; }
//     public string Name { get; set; }
// }
// public class BlogConfig:IEntityTypeConfiguration<Blog>
// {
//     public void Configure(EntityTypeBuilder<Blog> builder)
//     {
//         builder.ToTable("blogs");
//         builder.HasKey(r => r.Id);
//         builder.Property(r => r.Id).HasColumnName("id");
//         builder.Property(r => r.Name).HasColumnName("name");
//     }
// }