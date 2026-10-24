using System.ComponentModel.DataAnnotations;

namespace SwissBowling.Server.Infrastructure;

/// <summary>Validiert alle Request-Objekte eines Endpunkts anhand ihrer DataAnnotations (aus Core).</summary>
public sealed class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null || argument.GetType().Namespace?.StartsWith("SwissBowling.Core", StringComparison.Ordinal) != true)
            {
                continue;
            }

            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(argument, new ValidationContext(argument), results, validateAllProperties: true))
            {
                var errors = results
                    .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty), (r, member) => (member, r.ErrorMessage ?? "Ungültig"))
                    .GroupBy(x => x.member)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Item2).ToArray());

                return Results.ValidationProblem(errors);
            }
        }

        return await next(context);
    }
}
