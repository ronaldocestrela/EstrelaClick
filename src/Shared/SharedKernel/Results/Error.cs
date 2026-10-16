namespace EstrelaClick.Shared.SharedKernel.Results;

public readonly record struct Error(string Code, string Description)
{
    public static readonly Error None = new("None", string.Empty);
}
