using Cardsy.Data.Games.Concentration;
using System.Text.Json.Serialization;

namespace Cardsy.API.Serialization
{
    [JsonSerializable(typeof(ConcentrationGame))]
    [JsonSerializable(typeof(ConcentrationGame[]))]
    [JsonSerializable(typeof(BoardSize?))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {

    }
}
