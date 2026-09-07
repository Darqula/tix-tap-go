namespace TixTapGo.Shared.Validation;

public class ValidationErrorsDictionary : Dictionary<string, List<string>>
{
    public bool IsValid => Count == 0;

    public void AddError(string propertyName, string error)
    {
        if (TryGetValue(propertyName, out var existingErrors))
        {
            existingErrors.Add(error);
        }
        else
        {
            Add(propertyName, [error]);
        }
    }

    public Dictionary<string, string[]> ToValidationProblemPayload() =>
        this.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
}
