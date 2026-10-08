using Idp.Swiyu.Passkeys.Sts.Domain.Models;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Idp.Swiyu.Passkeys.Sts.Domain;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SwiyuIdentity> SwiyuIdentity => Set<SwiyuIdentity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure SwiyuIdentity
        builder.Entity<SwiyuIdentity>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(b => b.Id).ValueGeneratedOnAdd();
        });
    }
    
    // Override to include passkey model
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }
}
