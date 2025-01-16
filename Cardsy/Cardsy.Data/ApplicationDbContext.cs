using Cardsy.Data.Games.Concentration;
using Microsoft.EntityFrameworkCore;

namespace Cardsy.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<ConcentrationGame> ConcentrationGames { get; set; } = default!;
    }
}
