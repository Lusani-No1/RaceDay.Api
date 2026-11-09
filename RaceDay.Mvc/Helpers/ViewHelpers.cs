namespace RaceDay.Mvc.Helpers;

public static class ViewHelpers
{
    public static string Ordinal(int number)
    {
        var suffix = number % 100 is 11 or 12 or 13
            ? "th"
            : (number % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };

        return $"{number}{suffix}";
    }

    public static string FinishTime(TimeSpan time)
        => time.ToString(@"hh\:mm\:ss");

    public static string StatusBadge(string status) => status switch
    {
        "Confirmed" => "badge-confirmed",
        "Pending" => "badge-pending",
        "Cancelled" => "badge-cancelled",
        _ => "bg-secondary"
    };

    public static string EventTypeIcon(string type) => type switch
    {
        "Run" => "🏃",
        "Walk" => "🚶",
        "Cycle" => "🚴",
        _ => "🏁"
    };

    // Bundled RaceDay artwork served by the API. Events without an uploaded
    // banner use these so cards and detail pages always have a visual.
    private static readonly string[] DefaultBanners =
    {
        "/images/event-banners/event-banner-1.jpg",
        "/images/event-banners/event-banner-2.jpg",
        "/images/event-banners/event-banner-3.jpg"
    };

    public static string DefaultBanner(int eventId)
    {
        var index = (eventId - 1) % DefaultBanners.Length;
        if (index < 0) index += DefaultBanners.Length;
        return DefaultBanners[index];
    }
}
