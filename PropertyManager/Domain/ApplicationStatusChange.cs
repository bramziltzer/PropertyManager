namespace PropertyManager.Domain;

public record ApplicationStatusChange(ApplicationStatus OldStatus, ApplicationStatus NewStatus, string? Comment, string CreateBy, DateTime CreateDate);