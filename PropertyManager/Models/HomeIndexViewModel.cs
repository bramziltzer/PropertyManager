namespace PropertyManager.Models;

public sealed record HomeIndexCardViewModel(
    string Title,
    string Description,
    string Controller,
    string Action,
    string LinkText,
    bool IsPrimary);

public sealed record HomeIndexViewModel(
    bool IsAuthenticated,
    bool IsPropertyManager,
    bool IsApplicant)
{
    public IReadOnlyList<HomeIndexCardViewModel> Cards => IsPropertyManager
        ? PropertyManagerCards
        : IsApplicant
            ? ApplicantCards
            : [];

    private static IReadOnlyList<HomeIndexCardViewModel> PropertyManagerCards { get; } =
    [
        new(
            "Properties & Units",
            "Create, update, and remove properties and maintain unit listings and pricing.",
            "Properties",
            "Index",
            "Manage Properties",
            true),
        new(
            "Rental Applications",
            "Review submitted applications, view status history, and approve, return, or deny requests.",
            "Applications",
            "Index",
            "View Applications",
            false)
    ];

    private static IReadOnlyList<HomeIndexCardViewModel> ApplicantCards { get; } =
    [
        new(
            "Available Units",
            "Filter available apartments by property, bedroom count, and monthly rent, and start an application.",
            "Units",
            "Index",
            "Browse Units",
            true),
        new(
            "My Applications",
            "Continue draft applications, respond to returned applications, or check your application status.",
            "Applications",
            "Index",
            "View Applications",
            false)
    ];
}
