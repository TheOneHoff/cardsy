using Cardsy.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cardsy.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConcentrationGame.ConfigureModel(ref modelBuilder);
        }
    }
}
