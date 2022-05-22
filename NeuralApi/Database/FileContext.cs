using NeuralApi.Database;
using Microsoft.EntityFrameworkCore;

namespace NeuralApi.Database;

public class ReadyContext : DbContext
{
    public ReadyContext(DbContextOptions<ReadyContext> options) : base(options)
    {
    }

    public DbSet<FileModel> Files { get; set; } = null;
    public DbSet<FileModel> Pending { get; set; } = null;
}

public class PendingContext : DbContext
{
    public PendingContext(DbContextOptions<PendingContext> options) : base(options)
    {
    }

    public DbSet<FileModel> Files { get; set; } = null;
}
