namespace PropertyManager.Domain.Exceptions;

public class ApplicationIncompleteException(IReadOnlyList<string> incompleteSections)
    : DomainException($"The following sections must be saved first: {string.Join(", ", incompleteSections)}.")
{
    public IReadOnlyList<string> IncompleteSections { get; } = incompleteSections;
}
