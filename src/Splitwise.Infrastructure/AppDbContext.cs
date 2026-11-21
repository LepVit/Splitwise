using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Splitwise.Core.Entities;

namespace Splitwise.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(100);
            e.Property(u => u.LastName).HasMaxLength(100);
        });

        builder.Entity<Group>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Name).IsRequired().HasMaxLength(100);
        });

        builder.Entity<GroupMember>(e =>
        {
            e.HasKey(m => new { m.GroupId, m.UserId });

            e.HasOne(m => m.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Expense>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).IsRequired().HasMaxLength(200);
            e.Property(x => x.TotalCost).HasPrecision(18, 2);

            // Група необов'язкова
            e.HasOne(x => x.Group)
                .WithMany(g => g.Expenses)
                .HasForeignKey(x => x.GroupId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExpenseSplit>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.PaidShare).HasPrecision(18, 2);
            e.Property(s => s.OwedShare).HasPrecision(18, 2);

            e.HasOne(s => s.Expense)
                .WithMany(x => x.Splits)
                .HasForeignKey(s => s.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(s => new { s.ExpenseId, s.UserId }).IsUnique();
            e.HasIndex(s => s.UserId);
        });
    }
}
