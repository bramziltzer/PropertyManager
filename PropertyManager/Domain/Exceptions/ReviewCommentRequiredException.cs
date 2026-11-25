namespace PropertyManager.Domain.Exceptions;

public class ReviewCommentRequiredException(ApplicationStatus status)
    : DomainException($"A review comment is required when changing an application to '{status}'.")
{
}
