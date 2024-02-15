#pragma warning disable CS8618

using System.Text.Json.Serialization;


namespace SimpleMarketBoard.UniversalisModels;

/// <summary>
/// A model representing a market data recent history from Universalis.
/// </summary>
public class MarketDataEntry
{
    /// <summary>
    /// Gets or sets a value indicating whether the items are HQ.
    /// </summary>
    [JsonPropertyName("hq")]
    public bool Hq { get; set; }

    /// <summary>
    /// Gets or sets the price per unit.
    /// </summary>
    [JsonPropertyName("pricePerUnit")]
    public long PricePerUnit { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    [JsonPropertyName("quantity")]
    public long Quantity { get; set; }

    /// <summary>
    /// Gets or sets the name of the buyer.
    /// </summary>
    [JsonPropertyName("buyerName")]
    public string BuyerName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the name of the world.
    /// </summary>
    [JsonPropertyName("worldName")]
    public string WorldName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the world.
    /// </summary>
    [JsonPropertyName("worldID")]
    public ulong WorldID { get; set; }
}
