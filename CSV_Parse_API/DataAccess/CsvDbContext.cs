using CSV_Parse_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CSV_Parse_API.DataAccess
{
    public class CsvDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public CsvDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DbSet<Models.Values> Values { get; set; }
        public DbSet<Models.Results> Results { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("DatabaseCon"));
        }
    }
}
