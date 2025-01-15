using Cardsy.Data.Interface;
using Microsoft.EntityFrameworkCore;

namespace Cardsy.Data.Entities
{
    public class ConcentrationGame : IEntity
    {
        public long Id { get; set; }
        public BoardSize Size { get; set; }
        public int[] Solution { get; set; } = [];

        public static void ConfigureModel(ref ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConcentrationGame>(builder =>
            {
                builder.HasKey(c => c.Id);
                builder.Property(c => c.Size).IsRequired();
                builder.Property(c => c.Solution).IsRequired();
            });
        }
    }

    public enum BoardSize
    {
        _2x2,
        _5x5,
        _7x5,
        _7x6
    }
}
