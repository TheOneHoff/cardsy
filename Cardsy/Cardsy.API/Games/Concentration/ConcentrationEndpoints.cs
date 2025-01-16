using Cardsy.Data;
using Cardsy.Data.Games.Concentration;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Cardsy.API.Games.Concentration
{
    public static class ConcentrationEndpoints
    {
        public static void MapConcentrationEndpoints(this IEndpointRouteBuilder app)
        {
            var endpoints = app.MapGroup("/concentration");

            endpoints.MapGet("", GetAll);
            endpoints.MapGet("/{id}", Get);
            endpoints.MapPost("/create", Create);
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
                => db.ConcentrationGames.Where(c => c.Size == size).Skip(skip).Take(take));

        public static readonly Func<ApplicationDbContext, int, int, IAsyncEnumerable<ConcentrationGame>> GetAllWithNoBoardSize
            = EF.CompileAsyncQuery(
                (ApplicationDbContext db, int take, int skip)
                => db.ConcentrationGames.Skip(skip).Take(take));

        public static async Task<Results<Ok<ConcentrationGame>, NotFound>> Get(
            long id,
            ApplicationDbContext db,
            CancellationToken cancellationToken
            )
        {
            var result = await GetById(db, id).SingleOrDefaultAsync(cancellationToken);
            return result is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(result);
        }

        private static readonly Func<ApplicationDbContext, long, IAsyncEnumerable<ConcentrationGame?>> GetById
            = EF.CompileAsyncQuery(
                (ApplicationDbContext db, long id)
                => db.ConcentrationGames.Where(c => c.Id == id));

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

            foreach(int i in pairs.Values)
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
