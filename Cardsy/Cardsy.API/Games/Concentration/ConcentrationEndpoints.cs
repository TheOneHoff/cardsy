using Cardsy.Data;
using Cardsy.Data.Games.Concentration;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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
            var query = db.ConcentrationGames.AsQueryable();
            if (boardSize.HasValue)
            {
                query = query.Where(c => c.Size == boardSize.Value);
            }

            var result = await query.Skip(skip).Take(take).ToArrayAsync(cancellationToken);
            result ??= [];

            return TypedResults.Ok(result);
        }

        public static async Task<Results<Ok<ConcentrationGame>, NotFound>> Get(
            long id,
            ApplicationDbContext db,
            CancellationToken cancellationToken
            )
        {
            var result = await db.ConcentrationGames.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
            return result is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(result);
        }

        public static async Task<Results<Created<ConcentrationGame>, BadRequest<string>>> Create(
            ConcentrationGame toCreate,
            ApplicationDbContext db,
            CancellationToken cancellationToken
            )
        {
            if (toCreate.Solution.Length != MapBoardSize(toCreate.Size))
            {
                return TypedResults.BadRequest($"Parameter Solution (length: {toCreate.Solution.Length}) does not match size parameter ({MapBoardSize(toCreate.Size)})");
            }

            db.ConcentrationGames.Add(toCreate);
            await db.SaveChangesAsync(cancellationToken);

            return TypedResults.Created($"/{toCreate.Id}", toCreate);
        }

        public static int MapBoardSize(BoardSize boardSize)
        {
            return boardSize switch
            {
                BoardSize._2x2 => 4,
                BoardSize._5x5 => 25,
                BoardSize._7x5 => 35,
                BoardSize._7x6 => 42,
                _ => -1,
            };
        }
    }
}
