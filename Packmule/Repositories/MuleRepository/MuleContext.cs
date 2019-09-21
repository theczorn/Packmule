using Microsoft.EntityFrameworkCore;

namespace Packmule.Repositories.MuleRepository
{
    public partial class MuleContext : DbContext
    {
        public MuleContext()
        {
        }

        public MuleContext(DbContextOptions<MuleContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Notifications> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity<Notifications>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

                entity.Property(e => e.EmailAddress)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(101);

                entity.Property(e => e.LastMessaged).HasColumnType("datetime");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.OfficeLocation)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.HasSequence<int>("SalesOrderNumber");
        }
    }
}
