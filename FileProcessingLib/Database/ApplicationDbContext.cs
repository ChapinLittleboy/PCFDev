using Microsoft.EntityFrameworkCore;

namespace FileProcessingLib.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<PCFHeaderEntity> PCFHeaders { get; set; }
    public DbSet<PCFItemEntity> PCFItems { get; set; }
    //public DbSet<RepEntity> Reps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships and keys
        modelBuilder.Entity<PCFHeaderEntity>()
            .HasMany(h => h.PCFItems)
            .WithOne(d => d.PCFHeader)
            .HasForeignKey(d => d.PCFHeaderId);
    }
}
