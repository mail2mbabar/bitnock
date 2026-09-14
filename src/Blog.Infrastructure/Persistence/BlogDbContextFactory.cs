using Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Blog.Infrastructure.Persistence;

public sealed class BlogDbContextFactory : IDesignTimeDbContextFactory<BlogDbContext>
{
    public BlogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=nexus;Username=nexus;Password=nexus")
            .Options;
        return new BlogDbContext(options);
    }
}
