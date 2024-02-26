using Microsoft.EntityFrameworkCore;
using Server.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Server.Database
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Models.Message> Messages { get; set; }
        public DbSet<Models.User> Users { get; set; }
        public string DbPath { get; }
        public ApplicationDbContext()
        {
            DbPath = "C:\\Users\\hajde\\source\\repos\\Chatt\\Server\\chat.db";
            //DbPath = "C:\\Users\rami_\\source\\repos\\ChattApp\\Server\\chat.db";

		}

        // The following configures EF to create a SqlServer database file in the
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}