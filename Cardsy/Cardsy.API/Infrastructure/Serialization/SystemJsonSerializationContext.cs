using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Cardsy.API.Infrastructure.Serialization
{
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(ProblemDetails))]
    internal partial class SystemJsonSerializationContext : JsonSerializerContext
    {
    }
}
