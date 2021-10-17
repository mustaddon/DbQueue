using Microsoft.EntityFrameworkCore;

namespace DbQueue.EntityFrameworkCore
{
    public delegate void DfsDbContextConfigurator(DbContextOptionsBuilder optionsBuilder);

    internal class DbqDbContext : DbContext
    {
        public DbqDbContext(DfsDbContextConfigurator configurator)
        {
            _configurator = configurator;

            DbQueue = Set<DbItem>();
        }

        readonly DfsDbContextConfigurator _configurator;

        public DbSet<DbItem> DbQueue { get; private set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _configurator(optionsBuilder);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbItem>().Property(p => p.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<DbItem>().Property(s => s.LockId).IsConcurrencyToken();

            base.OnModelCreating(modelBuilder);
        }

    }

    internal class DbItem
    {
        public long Id { get; set; }
        public string Queue { get; set; } = string.Empty;
        public byte[] Data { get; set; } = DbqDatabase.BytesEmpty;
        public bool IsBlob { get; set; }
        public long? LockId { get; set; }

        public override int GetHashCode() => Id.GetHashCode();
        public override bool Equals(object obj) => Id == (obj as DbItem)?.Id;
    }
}
