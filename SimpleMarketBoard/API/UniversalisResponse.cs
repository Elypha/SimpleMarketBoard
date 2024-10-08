#pragma warning disable CS8618

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


namespace SimpleMarketBoard.API;

public class UniversalisResponseStatus
{
    public const ulong Success = 0;
    public const ulong ServerError = 1;
    public const ulong InvalidData = 2;
    public const ulong UserCancellation = 3;
    public const ulong UnknownError = 99;
}

/// <summary>
/// A model representing a market data response from Universalis.
/// </summary>
public class UniversalisResponse
{
    public ulong Status { get; set; }

    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [JsonPropertyName("itemID")]
    public ulong ItemId { get; set; }

    /// <summary>
    /// If this is a cross-world query.
    /// </summary>
    public bool IsCrossWorld { get; set; }

    /// <summary>
    /// An edited entry to get how many hours have passed since last update for each world.
    /// </summary>
    public Dictionary<string, long> WorldOutOfDate { get; set; } = [];

    /// <summary>
    /// Gets the total amount of items currently for sale.
    /// </summary>
    [JsonPropertyName("unitsForSale")]
    public long UnitsForSale { get; set; }

    /// <summary>
    /// Gets or sets the average price.
    /// </summary>
    [JsonPropertyName("averagePrice")]
    public double AveragePrice { get; set; }

    /// <summary>
    /// Gets or sets the average price of the NQ items.
    /// </summary>
    [JsonPropertyName("averagePriceNQ")]
    public double AveragePriceNq { get; set; }

    /// <summary>
    /// Gets or sets the average price of the HQ items.
    /// </summary>
    [JsonPropertyName("averagePriceHQ")]
    public double AveragePriceHq { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity.
    /// </summary>
    [JsonPropertyName("velocity")]
    public double Velocity { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the NQ items.
    /// </summary>
    [JsonPropertyName("velocityNQ")]
    public double VelocityNq { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the HQ items.
    /// </summary>
    [JsonPropertyName("velocityHQ")]
    public double VelocityHq { get; set; }

    /// <summary>
    /// Gets or sets the listings.
    /// </summary>
    [JsonPropertyName("listings")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter required for JSON deserialization")]
    public IList<MarketDataListing> Listings { get; set; } = [];

    /// <summary>
    /// Gets or sets the recent history.
    /// </summary>
    [JsonPropertyName("entries")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter required for JSON deserialization")]
    public IList<MarketDataEntry> Entries { get; set; } = [];
}
