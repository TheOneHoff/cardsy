using Cardsy.API.Serialization;
using Cardsy.Data;
using Cardsy.Data.Games.Concentration;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Cardsy.API.Endpoints.Games.Concentration
{
    public static class ConcentrationEndpoints
    {
        public static void MapConcentrationEndpoints(this IEndpointRouteBuilder app)
        {
            var endpoints = app.MapGroup("/concentration").WithTags("Concentration");

            endpoints.MapGet("/search", GetAll);
            endpoints.MapGet("/{id}", Get);
            endpoints.MapPost("/create", Create);
            endpoints.MapDelete("/delete/{id}", Delete);
        }

        public static async Task<Ok<ConcentrationGame[]>> GetAll(
            ApplicationDbContext db,
            CancellationToken cancellationToken,
            BoardSize? boardSize = null,
            int take = 200,
            int skip = 0)
        {
            ConcentrationGame[] result = [];

            if (boardSize.HasValue)
            {
                result = await GetAllWithBoardSize(db, boardSize.Value, take, skip).ToArrayAsync(cancellationToken);
            }
            else
            {
                result = await GetAllWithNoBoardSize(db, take, skip).ToArrayAsync(cancellationToken);
            }

            return TypedResults.Ok(result);
        }

        public static readonly Func<ApplicationDbContext, BoardSize, int, int, IAsyncEnumerable<ConcentrationGame>> GetAllWithBoardSize
            = EF.CompileAsyncQuery(
                (ApplicationDbContext db, BoardSize size, int take, int skip)
                => db.ConcentrationGames.Where(c => c.Size == size).OrderBy(c => c.Id).Skip(skip).Take(take).AsNoTracking());

        public static readonly Func<ApplicationDbContext, int, int, IAsyncEnumerable<ConcentrationGame>> GetAllWithNoBoardSize
            = EF.CompileAsyncQuery(
                (ApplicationDbContext db, int take, int skip)
                => db.ConcentrationGames.OrderBy(c => c.Id).Skip(skip).Take(take).AsNoTracking());

        public static async Task<Results<Ok<ConcentrationGame>, NotFound>> Get(
            long id,
            ApplicationDbContext db,
            IDistributedCache cache,
            CancellationToken cancellationToken
            )
        {
            ConcentrationGame? result;

            string key = $"concentration-{id}";

            byte[]? cachedGame = await cache.GetAsync(key, cancellationToken);

            if (cachedGame is not null && cachedGame.Length > 0)
            {
                using Stream cachedStream = new MemoryStream(cachedGame);
                result = await JsonSerializer.DeserializeAsync(
                    cachedStream,
                    AppJsonSerializerContext.Default.ConcentrationGame,
                    cancellationToken);
                return result is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(result);
            }

            result = await GetByIdWithNoTracking(db, id).SingleOrDefaultAsync(cancellationToken);

            if (result is not null)
            {
                cachedGame = JsonSerializer.SerializeToUtf8Bytes(result, AppJsonSerializerContext.Default.ConcentrationGame);
                await cache.SetAsync(key, cachedGame, cancellationToken);
                return TypedResults.Ok(result);
            }

            return TypedResults.NotFound();
        }

        private static readonly Func<ApplicationDbContext, long, IAsyncEnumerable<ConcentrationGame?>> GetById
            = EF.CompileAsyncQuery(
                (ApplicationDbContext db, long id)
                => db.ConcentrationGames.Where(c => c.Id == id));

        private static readonly Func<ApplicationDbContext, long, IAsyncEnumerable<ConcentrationGame?>> GetByIdWithNoTracking
            = EF.CompileAsyncQuery(
                (ApplicationDbContext db, long id)
                => db.ConcentrationGames.Where(c => c.Id == id).AsNoTracking());

        public static async Task<Results<Created<ConcentrationGame>, BadRequest<string>>> Create( 
            ConcentrationGame toCreate,
            ApplicationDbContext db,
            CancellationToken cancellationToken
            )
        {
            if (toCreate.Solution.Length != MapBoardSize(toCreate.Size))
            {
                return TypedResults.BadRequest($"Parameter 'Solution' (length: {toCreate.Solution.Length}) does not match 'Size' parameter ({MapBoardSize(toCreate.Size)})");
            }

            if (!IsSolutionValid(toCreate.Solution))
            {
                return TypedResults.BadRequest($"Parameter 'Solution' is invalid; A solution requies an array of integers such that each int must appear exactly twice");
            }

            var check = await GetById(db, toCreate.Id).SingleOrDefaultAsync(cancellationToken);
            if (check is not null)
            {
                return TypedResults.BadRequest($"Parameter 'Id' is invalid; A game with that 'Id' already exists");
            }

            db.ConcentrationGames.Add(toCreate);
            await db.SaveChangesAsync(cancellationToken);

            return TypedResults.Created($"/{toCreate.Id}", toCreate);
        }

        public static async Task<Results<NoContent, NotFound>> Delete(
            long id,
            ApplicationDbContext db,
            IDistributedCache cache,
            CancellationToken cancellationToken
            )
        {
            ConcentrationGame? toDelete = await GetById(db, id).SingleOrDefaultAsync(cancellationToken);

            if (toDelete is null)
            {
                return TypedResults.NotFound();
            }

            string key = $"concentration-{id}";
            await cache.RemoveAsync(key, cancellationToken);

            db.Remove(toDelete);
            await db.SaveChangesAsync(cancellationToken);

            return TypedResults.NoContent();
        }

        private static bool IsSolutionValid(int[] solution)
        {
            if (solution.Length == 0)
                return false;

            if (solution.Length % 2 != 0)
                return false;

            Dictionary<int, int> pairs = [];

            foreach (int i in solution)
            {
                pairs.TryGetValue(i, out int count);
                pairs[i] = ++count;
            }

            if (pairs.Count == 0)
                return false;

            foreach (int i in pairs.Values)
            {
                if (i != 2)
                    return false;
            }

            return true;
        }

        public static int MapBoardSize(BoardSize boardSize)
        {
            return boardSize switch
            {
                BoardSize._2x2 => 4,
                BoardSize._6x5 => 30,
                BoardSize._7x4 => 28,
                BoardSize._7x6 => 42,
                _ => -1,
            };
        }
    }
}
