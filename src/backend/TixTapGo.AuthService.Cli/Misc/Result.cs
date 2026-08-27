namespace TixTapGo.AuthService.Cli.Misc;

internal sealed record Result<TResult>
{
    public static Result<TResult> Success(TResult value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static Result<TResult> Failure(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };

    public bool IsSuccess { get; private set; }
    public TResult? Value { get; private set; }
    public string? Error { get; private set; }
}
