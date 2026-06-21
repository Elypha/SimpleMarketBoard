using Dalamud.Interface.ImGuiNotification;
using Lumina.Excel.Sheets;
using Miosuke;
using SimpleMarketBoard.Assets;

namespace SimpleMarketBoard.Modules;

public static class MarketTargets
{
    private static readonly Lazy<IReadOnlyList<string>> WorldList = new(() => Data.WorldSheet
        .Where(x => x.IsPublic)
        .Select(FromWorld)
        .OrderBy(x => x)
        .ToList());
    private static readonly Lazy<IReadOnlyList<string>> DataCenterList = new(() => Data.WorldSheet
        .Where(x => x.IsPublic)
        .GroupBy(x => x.DataCenter.RowId)
        .Select(x => FromDataCenter(x.First()))
        .OrderBy(x => x)
        .ToList());
    private static readonly Lazy<IReadOnlyList<string>> RegionList = new(() => Data.WorldSheet
        .Where(x => x.IsPublic)
        .GroupBy(x => x.DataCenter.Value!.Region.RowId)
        .Select(x => FromRegion(x.First()))
        .OrderBy(x => x)
        .ToList());

    public static IReadOnlyList<string> Worlds => WorldList.Value;
    public static IReadOnlyList<string> DataCenters => DataCenterList.Value;
    public static IReadOnlyList<string> Regions => RegionList.Value;

    public static string FromWorld(World world) => world.Name.ToString();
    public static string FromDataCenter(World world) => world.DataCenter.Value!.Name.ToString();

    public static string FromRegion(World world)
    {
        var region = world.DataCenter.Value!.Region;
        return region.RowId switch
        {
            1 => "Japan",
            2 => "North-America",
            3 => "Europe",
            4 => "Oceania",
            _ => region.Value!.Name.ToString(),
        };
    }

    public static string? Resolve(string input, NotificationType? failureNotification = null)
    {
        var normalizedInput = input.Trim();
        if (normalizedInput == "") return null;

        var target = Regions.Concat(DataCenters).Concat(Worlds).FirstOrDefault(x => Same(x, normalizedInput));
        if (target is null && failureNotification is not null)
        {
            Service.NotificationManager.AddNotification(new Notification
            {
                Content = $"Target cannot be determined from name: {input}",
                Type = failureNotification.Value,
            });
        }

        return target;
    }

    public static bool Same(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

}
