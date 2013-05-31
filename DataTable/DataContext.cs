using System.Data.Entity;

namespace DataTable
{
    public class DataContext : DbContext
    {
        public DbSet<Friend> Friends { get; set; }
    }
}