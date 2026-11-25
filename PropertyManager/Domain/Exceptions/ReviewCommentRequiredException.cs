namespace PropertyManager.Domain.Exceptions;

public class ReviewCommentRequiredException(ApplicationStatus status)
    : Exception($"A review comment is required when changing an application to '{status}'.")
{
}
