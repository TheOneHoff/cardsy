using Cardsy.API.Options;
using Cardsy.Data.Database;
using Cardsy.Data.Games.Concentration;
using Dapper;

[module: DapperAot]
namespace Cardsy.API.Endpoints.Games.Concentration
{
    public interface IConcentrationService
    {
        Task<IEnumerable<ConcentrationGame>> GetAll(CancellationToken cancellationToken = default);
        Task<IEnumerable<ConcentrationGame>> GetAll(int size, CancellationToken cancellationToken = default);
        Task<ConcentrationGame?> Get(long id, CancellationToken cancellationToken = default);
        Task<ConcentrationGame> Create(ConcentrationGame game, CancellationToken cancellationToken = default);
        Task<ConcentrationGame> Update(ConcentrationGame game, CancellationToken cancellationToken = default);
    }

    public class ConcentrationService : IConcentrationService
    {
        readonly ILogger<ConcentrationService> _logger;
        readonly IDbConnectionFactory _dbFactory;

        public ConcentrationService(
            ILogger<ConcentrationService> logger,
            [FromKeyedServices(DatabaseNames.Cardsy)] IDbConnectionFactory dbFactory)
        {
            _logger = logger;
            _dbFactory = dbFactory;
        }

        public async Task<IEnumerable<ConcentrationGame>> GetAll(CancellationToken cancellationToken = default)
        {
            using var connection = await _dbFactory.OpenConnectionAsync(cancellationToken);
            return await connection.QueryAsync<ConcentrationGame>("select * from public.\"Concentration\"");
        }

        public async Task<IEnumerable<ConcentrationGame>> GetAll(int size, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbFactory.OpenConnectionAsync(cancellationToken);
            return await connection.QueryAsync<ConcentrationGame>("select * from public.\"Concentration\" where Size = @size", new { size });
        }

        public async Task<ConcentrationGame?> Get(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbFactory.OpenConnectionAsync(cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<ConcentrationGame>("select * from public.\"Concentration\" where \"Id\" = @id limit 1", new { id });
        }

        public async Task<ConcentrationGame> Create(ConcentrationGame game, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbFactory.OpenConnectionAsync(cancellationToken);
            await connection.ExecuteAsync("insert into public.\"Concentration\" values (@Id, @Size, @Solution)", game);
            return game;
        }
        public async Task<ConcentrationGame> Update(ConcentrationGame game, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbFactory.OpenConnectionAsync(cancellationToken);
            await connection.ExecuteAsync("update public.\"Concentration\" set \"Size\" = @Size, \"Solution\" = @Solution where \"Id\" = @Id", game);
            return game;
        }
    }
}
