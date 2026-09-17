using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TixTapGo.Shared.Web.Idempotency;

internal sealed class IdempotentEndpointFilter : IEndpointFilter
{
    private readonly IdempotencyPrerequisitesValidator _validator = new();

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validationResult = _validator.Validate(context.HttpContext);
        if (!validationResult.Success)
        {
            return TypedResults.Problem(
                detail: validationResult.Error,
                statusCode: validationResult.StatusCode);
        }
        
        var serviceProvider = context.HttpContext.RequestServices;
        var stateManager = serviceProvider.GetRequiredService<IdempotencyStateManager>();

        try
        {
            return await stateManager.HandleIdempotencyKeyState(
                context.HttpContext,
                validationResult.IdempotencyKey!.Value,
                () => next(context)
            );
        }
        catch (IdempotencyException ie)
        {
            return TypedResults.Problem(
                detail: ie.Message,
                statusCode: ie.StatusCode
            );
        }
    }
}
