using System.Security.Claims;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace TixTapGo.AuthService.Controllers;

public class AuthorizationController(IOpenIddictApplicationManager applicationManager) : Controller
{
    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request != null && request.IsClientCredentialsGrantType())
        {
            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                return BadRequest("ClientId must be provided");
            }

            var application = await applicationManager.FindByClientIdAsync(request.ClientId);
            if (application == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The application was not found."
                    }));
            }

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType,
                OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);

            identity.SetClaim(OpenIddictConstants.Claims.Subject,
                await applicationManager.GetClientIdAsync(application));
            identity.SetClaim(OpenIddictConstants.Claims.Name,
                await applicationManager.GetDisplayNameAsync(application));

            identity.SetDestinations(static claim => claim.Type switch
            {
                OpenIddictConstants.Claims.Name when claim.Subject != null &&
                                                     claim.Subject.HasScope(OpenIddictConstants.Scopes.Profile) =>
                [
                    OpenIddictConstants.Destinations.AccessToken,
                    OpenIddictConstants.Destinations.IdentityToken
                ],
                _ => [OpenIddictConstants.Destinations.AccessToken]
            });

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest("The specified grant is not implemented");
    }
}
