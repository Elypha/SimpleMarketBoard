#pragma warning disable CS8618

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


namespace SimpleMarketBoard.API;

/// <summary>
/// A model representing a market data recent history from Universalis.
/// </summary>
public class MarketDataHistory
{
    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [JsonPropertyName("itemID")]
    public ulong ItemId { get; set; } = 0;

    /// <summary>
    /// Gets or sets the ID of the world.
    /// </summary>
    [JsonPropertyName("worldID")]
    public ulong WorldID { get; set; }

    /// <summary>
    /// Gets or sets the last upload time.
    /// </summary>
    [JsonPropertyName("lastUploadTime")]
    public long LastUploadTime { get; set; }

    /// <summary>
    /// Gets or sets the recent history.
    /// </summary>
    [JsonPropertyName("entries")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter required for JSON deserialization")]
    public IList<MarketDataEntry> Entries { get; set; } = [];

    /// <summary>
    /// Gets or sets the name of the world.
    /// </summary>
    [JsonPropertyName("worldName")]
    public string WorldName { get; set; }

    /// <summary>
    /// Gets or sets the name of the datacenter.
    /// </summary>
    [JsonPropertyName("dcName")]
    public string DcName { get; set; }

    /// <summary>
    /// Gets the stack size histogram.
    /// </summary>
    [JsonPropertyName("stackSizeHistogram")]
    public Dictionary<string, long> StackSizeHistogram { get; set; } = [];

    /// <summary>
    /// Gets the stack size histogram of the NQ items.
    /// </summary>
    [JsonPropertyName("stackSizeHistogramNQ")]
    public Dictionary<string, long> StackSizeHistogramNq { get; set; } = [];

    /// <summary>
    /// Gets the stack size histogram of the HQ items.
    /// </summary>
    [JsonPropertyName("stackSizeHistogramHQ")]
    public Dictionary<string, long> StackSizeHistogramHq { get; set; } = [];

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
}
