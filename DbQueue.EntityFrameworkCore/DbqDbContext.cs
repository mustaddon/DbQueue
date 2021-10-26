using Microsoft.EntityFrameworkCore;
using System;

namespace DbQueue.EntityFrameworkCore
{
    public delegate void DfsDbContextConfigurator(DbContextOptionsBuilder optionsBuilder);

    internal class DbqDbContext : DbContext
    {
        public DbqDbContext(DfsDbContextConfigurator configurator)
        {
            _configurator = configurator;

            DbQueue = Set<EfcItem>();
        }

        readonly DfsDbContextConfigurator _configurator;

        public DbSet<EfcItem> DbQueue { get; private set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _configurator(optionsBuilder);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EfcItem>().Property(p => p.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<EfcItem>().Property(s => s.LockId).IsConcurrencyToken();

            base.OnModelCreating(modelBuilder);
        }

    }

    internal class EfcItem
    {
        public long Id { get; set; }
        public string Queue { get; set; } = string.Empty;
        public byte[] Data { get; set; } = BytesEmpty;
        public bool IsBlob { get; set; }
        public long Hash { get; set; }
        public string? Type { get; set; }
        public DateTime? AvailableAfter { get; set; }
        public DateTime? RemoveAfter { get; set; }
        public long? LockId { get; set; }


        public override int GetHashCode() => Id.GetHashCode();
        public override bool Equals(object obj) => Id == (obj as EfcItem)?.Id;

        static readonly byte[] BytesEmpty = new byte[0];
    }
}
