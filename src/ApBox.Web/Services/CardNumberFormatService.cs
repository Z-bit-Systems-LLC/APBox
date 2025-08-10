using System.Numerics;
using Blazored.LocalStorage;

namespace ApBox.Web.Services;

/// <summary>
/// Defines the available card number display formats
/// </summary>
public enum CardNumberFormat
{
    Decimal,
    Hexadecimal,
    Binary
}

/// <summary>
/// Service for managing card number display format preferences and formatting card numbers
/// </summary>
public class CardNumberFormatService : IDisposable
{
    private readonly ILocalStorageService _localStorage;
    private CardNumberFormat _currentFormat = CardNumberFormat.Decimal;
    private bool _initialized = false;
    private const string STORAGE_KEY = "cardNumberFormat";

    public CardNumberFormatService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Event fired when the card number format changes
    /// </summary>
    public event Action<CardNumberFormat>? FormatChanged;

    /// <summary>
    /// Gets or sets the current card number display format
    /// </summary>
    public CardNumberFormat CurrentFormat
    {
        get => _currentFormat;
        set
        {
            if (_currentFormat != value)
            {
                _currentFormat = value;
                _ = SaveToLocalStorageAsync(value);
                FormatChanged?.Invoke(value);
            }
        }
    }

    /// <summary>
    /// Initializes the service by loading the saved format from localStorage
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            var savedFormat = await _localStorage.GetItemAsStringAsync(STORAGE_KEY);
            if (!string.IsNullOrEmpty(savedFormat) && Enum.TryParse<CardNumberFormat>(savedFormat, out var format))
            {
                _currentFormat = format;
            }
        }
        catch (Exception)
        {
            // Fall back to default if localStorage is not available
            _currentFormat = CardNumberFormat.Decimal;
        }

        _initialized = true;
    }

    /// <summary>
    /// Formats a card number according to the current display format
    /// </summary>
    /// <param name="cardNumber">The card number as a decimal string</param>
    /// <param name="bitLength">The bit length of the card data</param>
    /// <returns>Formatted card number string</returns>
    public string FormatCardNumber(string cardNumber, int bitLength)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return string.Empty;

        // Try to parse the card number using BigInteger for large number support
        if (!BigInteger.TryParse(cardNumber, out var number))
            return cardNumber; // Return as-is if parsing fails

        return _currentFormat switch
        {
            CardNumberFormat.Decimal => number.ToString(),
            CardNumberFormat.Hexadecimal => FormatHexadecimal(number),
            CardNumberFormat.Binary => FormatBinary(number, bitLength),
            _ => cardNumber
        };
    }

    /// <summary>
    /// Formats a BigInteger as a hexadecimal string
    /// </summary>
    private static string FormatHexadecimal(BigInteger number)
    {
        if (number == 0)
            return "0x0";

        // Convert to hex without leading zeros
        var hexString = number.ToString("X");
        
        // Remove any leading zeros that BigInteger.ToString might add
        hexString = hexString.TrimStart('0');
        
        // If all digits were zeros (shouldn't happen since we check for 0 above), use "0"
        if (string.IsNullOrEmpty(hexString))
            hexString = "0";

        return $"0x{hexString}";
    }

    /// <summary>
    /// Formats a BigInteger as a binary string with proper bit width padding
    /// </summary>
    private static string FormatBinary(BigInteger number, int bitLength)
    {
        // Ensure we have a reasonable bit length
        var actualBitLength = Math.Max(bitLength, 8);
        
        // Convert BigInteger to binary string
        var binaryString = ConvertToBinaryString(number);
        
        // Pad with leading zeros to match bit length
        if (binaryString.Length < actualBitLength)
        {
            binaryString = binaryString.PadLeft(actualBitLength, '0');
        }
        
        // Group by 4 bits for readability
        var grouped = string.Empty;
        for (int i = 0; i < binaryString.Length; i += 4)
        {
            if (i > 0) grouped += " ";
            var length = Math.Min(4, binaryString.Length - i);
            grouped += binaryString.Substring(i, length);
        }
        
        return grouped;
    }

    /// <summary>
    /// Converts a BigInteger to binary string representation
    /// </summary>
    private static string ConvertToBinaryString(BigInteger number)
    {
        if (number == 0)
            return "0";
        
        var result = string.Empty;
        var tempNumber = number;
        
        while (tempNumber > 0)
        {
            result = ((tempNumber % 2) == 0 ? "0" : "1") + result;
            tempNumber /= 2;
        }
        
        return result;
    }

    /// <summary>
    /// Saves the current format to localStorage
    /// </summary>
    private async Task SaveToLocalStorageAsync(CardNumberFormat format)
    {
        try
        {
            await _localStorage.SetItemAsStringAsync(STORAGE_KEY, format.ToString());
        }
        catch (Exception)
        {
            // Ignore localStorage errors
        }
    }

    public void Dispose()
    {
        // Nothing to dispose currently
    }
}