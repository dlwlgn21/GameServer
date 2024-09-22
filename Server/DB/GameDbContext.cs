using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.DB
{
    public class GameDbContext : DbContext
    {
        public DbSet<AccountDb> Accounts { get; set; }
        public DbSet<PlayerDb> Players { get; set; }
        public DbSet<ItemDb> Items { get; set; }

        static readonly ILoggerFactory _logger = LoggerFactory.Create(b => { b.AddConsole(); });

        const string CONNECTION_STRING = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=GameDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        protected override void OnConfiguring(DbContextOptionsBuilder b)
        {
            b.UseLoggerFactory(_logger).UseSqlServer(ConfigManager.Config == null ? CONNECTION_STRING : ConfigManager.Config.connectionString);
        }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<AccountDb>().HasIndex(acc => acc.AccountName).IsUnique();
            b.Entity<PlayerDb>().HasIndex(player => player.PlayerName).IsUnique();
        }
    }
}
