using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TixTapGo.AuthService.Cli.Operations;

namespace TixTapGo.AuthService.Cli;

internal sealed class AppManagementFlow
{
    private readonly IHost _appHost;

    public AppManagementFlow(IHost appHost)
    {
        _appHost = appHost;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                ShowInitialPrompt();
                var operationCode = ReadIntInput(v => v == 0 || Enum.IsDefined(typeof(AppOperations), v));
                if (operationCode == 0)
                {
                    Console.WriteLine("Goodbye!");
                    await Task.Delay(1000, cancellationToken);
                    return;
                }

                await ExecuteOperationAsync((AppOperations)operationCode, cancellationToken);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task ExecuteOperationAsync(AppOperations operation, CancellationToken cancellationToken)
    {
        Console.WriteLine();
        await using var scope = _appHost.Services.CreateAsyncScope();

        switch (operation)
        {
            case AppOperations.ListApps:
                Console.WriteLine("Registered apps Client IDs:");
                var appClientIds = await scope.ServiceProvider
                    .GetRequiredService<IListAppsOperation>()
                    .ExecuteAsync(cancellationToken);
                foreach (var clientId in appClientIds)
                {
                    Console.WriteLine($"- {clientId}");
                }

                break;

            case AppOperations.RegisterApp:
                string registeringAppClientId = RequestAppClientId();
                var registrationResult = await scope.ServiceProvider
                    .GetRequiredKeyedService<IManageAppOperation>(AppOperations.RegisterApp)
                    .ExecuteAsync(registeringAppClientId, cancellationToken);

                if (!registrationResult.IsSuccess)
                {
                    Console.WriteLine($"{registrationResult.Error}. Operation cancelled.");
                    return;
                }

                string registeredSecret = registrationResult.Value!;

                Console.WriteLine(
                    $"App with client ID {registeringAppClientId} registered successfully. Client secret:");
                Console.WriteLine(registeredSecret);
                Console.WriteLine("Be sure to save this secret, as it will not be shown again!");
                break;

            case AppOperations.DeleteApp:
                string deletingAppClientId = RequestAppClientId();
                var deletionResult = await scope.ServiceProvider
                    .GetRequiredKeyedService<IManageAppOperation>(AppOperations.DeleteApp)
                    .ExecuteAsync(deletingAppClientId, cancellationToken);

                if (!deletionResult.IsSuccess)
                {
                    Console.WriteLine($"{deletionResult.Error}. Operation cancelled.");
                    return;
                }

                Console.WriteLine($"App with client ID {deletingAppClientId} deleted successfully.");
                break;

            case AppOperations.RotateAppSecret:
                string rotatingAppClientId = RequestAppClientId();
                var rotationResult = await scope.ServiceProvider
                    .GetRequiredKeyedService<IManageAppOperation>(AppOperations.RotateAppSecret)
                    .ExecuteAsync(rotatingAppClientId, cancellationToken);

                if (!rotationResult.IsSuccess)
                {
                    Console.WriteLine($"{rotationResult.Error}. Operation cancelled.");
                    return;
                }

                string rotatedSecret = rotationResult.Value!;

                Console.WriteLine(
                    $"Secret of app with client ID {rotatingAppClientId} was rotated successfully. New client secret:");
                Console.WriteLine(rotatedSecret);
                Console.WriteLine("Be sure to save this secret, as it will not be shown again!");
                break;
        }
    }

    private static void ShowInitialPrompt()
    {
        Console.WriteLine();
        Console.WriteLine("=== TixTapGo Auth Admin ===");
        Console.WriteLine($"{(int)AppOperations.ListApps}) List apps");
        Console.WriteLine($"{(int)AppOperations.RegisterApp}) Register app");
        Console.WriteLine($"{(int)AppOperations.DeleteApp}) Delete app");
        Console.WriteLine($"{(int)AppOperations.RotateAppSecret}) Rotate app secret");
        Console.WriteLine("0) Exit");
    }

    private static int ReadIntInput(Func<int, bool> validate)
    {
        while (true)
        {
            Console.Write("> ");
            if (!int.TryParse(Console.ReadLine()?.Trim(), out int result) || !validate(result))
            {
                Console.WriteLine("Invalid input.");
            }
            else
            {
                return result;
            }
        }
    }

    private static string RequestAppClientId()
    {
        Console.Write("Enter app client ID (for example: my-awesome-service): ");
        while (true)
        {
            string appClientId = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(appClientId))
            {
                Console.WriteLine("Error: The value cannot be empty");
                continue;
            }

            return appClientId;
        }
    }
}
