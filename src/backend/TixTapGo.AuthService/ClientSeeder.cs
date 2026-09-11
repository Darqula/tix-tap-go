using OpenIddict.Abstractions;

namespace TixTapGo.AuthService;

/// <summary>
/// Seeds/validates the OpenIddict client-credentials applications managed by AppHost
/// </summary>
internal static class ClientSeeder
{
    public static async Task SeedClientsAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> clients = app.Configuration
            .GetSection("Authentication:ClientServices")
            .GetChildren()
            .Where(client => !string.IsNullOrWhiteSpace(client.Value))
            .ToDictionary(client => client.Key, client => client.Value!);

        if (clients.Count == 0)
        {
            return;
        }

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach ((string clientId, string clientSecret) in clients)
        {
            var existingApp = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
            if (existingApp is null)
            {
                await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
                    }
                }, cancellationToken);
                continue;
            }

            // Only write when the persisted secret actually changed
            bool secretStillValid = await applicationManager.ValidateClientSecretAsync(
                existingApp, clientSecret, cancellationToken);
            if (!secretStillValid)
            {
                await applicationManager.UpdateAsync(existingApp, clientSecret, cancellationToken);
            }
        }
    }
}
