using System.ComponentModel.DataAnnotations;

namespace PropertyManager.Models;

/// <summary>
/// View model for confirming the deletion of an entity.
/// </summary>
public sealed class ConfirmDeleteViewModel
{
    /// <summary>
    /// The URL to which the delete request should be sent.
    /// </summary>
    [Required]
    public required string ActionUrl { get; init; }

    /// <summary>
    /// The name of the entity being deleted.
    /// </summary>
    [Required]
    public required string EntityName { get; init; }

    /// <summary>
    /// Additional details about the deletion, if any.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Whether the delete action is available.
    /// </summary>
    public bool CanDelete { get; init; } = true;

    /// <summary>
    /// The title for the delete confirmation modal.
    /// </summary>
    public string Title => $"Delete {EntityName}?";
}
