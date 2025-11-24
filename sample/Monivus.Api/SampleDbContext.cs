using Microsoft.EntityFrameworkCore;

namespace Monivus.Api
{
    public class SampleDbContext : DbContext
    {
        public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
        {
        }
        public DbSet<SampleEntity> SampleEntities { get; set; }
    }

    public class SampleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}