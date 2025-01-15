using Microsoft.EntityFrameworkCore;

namespace Cardsy.Data.Interface
{
    public interface IEntity
    {
        public static void ConfigureModel(ref ModelBuilder modelBuilder) => throw new NotImplementedException();
    }
}
