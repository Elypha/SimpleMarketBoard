#pragma warning disable CS8618

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SimpleMarketBoard.UniversalisModels;

/// <summary>
/// A model representing a market data response from Universalis.
/// </summary>
public class MarketDataCurrent
{
    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [JsonPropertyName("itemID")]
    public ulong ItemId { get; set; } = 0;

    /// <summary>
    /// Gets or sets the last upload time.
    /// </summary>
    [JsonPropertyName("lastUploadTime")]
    public long LastUploadTime { get; set; }

    /// <summary>
    /// Gets or sets the listings.
    /// </summary>
    [JsonPropertyName("listings")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter required for JSON deserialization")]
    public IList<MarketDataListing> Listings { get; set; } = new List<MarketDataListing>();

    /// <summary>
    /// Gets or sets the recent history.
    /// </summary>
    [JsonPropertyName("recentHistory")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter required for JSON deserialization")]
    public IList<MarketDataEntry> Entries { get; set; } = new List<MarketDataEntry>();

    /// <summary>
    /// Gets or sets the name of the datacenter.
    /// </summary>
    [JsonPropertyName("dcName")]
    public string DcName { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity.
    /// </summary>
    [JsonPropertyName("regularSaleVelocity")]
    public double Velocity { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the NQ items.
    /// </summary>
    [JsonPropertyName("nqSaleVelocity")]
    public double VelocityNq { get; set; }

    /// <summary>
    /// Gets or sets the sale velocity of the HQ items.
    /// </summary>
    [JsonPropertyName("hqSaleVelocity")]
    public double VelocityHq { get; set; }

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
    /// Gets the stack size histogram.
    /// </summary>
    [JsonPropertyName("stackSizeHistogram")]
    public Dictionary<string, long> StackSizeHistogram { get; set; } = new Dictionary<string, long>();

    /// <summary>
    /// Gets the stack size histogram of the NQ items.
    /// </summary>
    [JsonPropertyName("stackSizeHistogramNQ")]
    public Dictionary<string, long> StackSizeHistogramNq { get; set; } = new Dictionary<string, long>();

    /// <summary>
    /// Gets the stack size histogram of the HQ items.
    /// </summary>
    [JsonPropertyName("stackSizeHistogramHQ")]
    public Dictionary<string, long> StackSizeHistogramHq { get; set; } = new Dictionary<string, long>();

    /// <summary>
    /// Gets or sets the name of the world.
    /// </summary>
    [JsonPropertyName("worldName")]
    public string WorldName { get; set; }

    /// <summary>
    /// (cross-world query only) Gets the last upload times for each world.
    /// </summary>
    [JsonPropertyName("worldUploadTimes")]
    public Dictionary<string, long> WorldUploadTimes { get; set; } = new Dictionary<string, long>();

    /// <summary>
    /// Gets the total amount of items currently for sale.
    /// </summary>
    [JsonPropertyName("unitsForSale")]
    public long UnitsForSale { get; set; }
}
