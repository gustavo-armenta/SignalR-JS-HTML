using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace DataTable
{
    public class DataContext : DbContext
    {
        public DbSet<Friend> Friends { get; set; }
    }
}