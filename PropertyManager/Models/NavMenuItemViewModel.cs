namespace PropertyManager.Models;

public sealed record NavMenuItemViewModel(
    string Title,
    string Controller,
    string Action,
    bool IsActive);
