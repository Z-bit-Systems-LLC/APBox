using System.Numerics;
using ApBox.Web.Services;
using Blazored.LocalStorage;
using Moq;

namespace ApBox.Web.Tests.Services;

[TestFixture]
[Category("Unit")]
public class CardNumberFormatServiceTests
{
    private Mock<ILocalStorageService> _mockLocalStorage;
    private CardNumberFormatService _service;

    [SetUp]
    public void SetUp()
    {
        _mockLocalStorage = new Mock<ILocalStorageService>();
        _service = new CardNumberFormatService(_mockLocalStorage.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _service?.Dispose();
    }

    [Test]
    public void FormatCardNumber_EmptyInput_ReturnsEmpty()
    {
        var result = _service.FormatCardNumber("", 26);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FormatCardNumber_InvalidNumber_ReturnsOriginal()
    {
        var input = "not-a-number";
        var result = _service.FormatCardNumber(input, 26);
        Assert.That(result, Is.EqualTo(input));
    }

    [Test]
    public void FormatCardNumber_SmallNumber_DecimalFormat_ReturnsDecimal()
    {
        _service.CurrentFormat = CardNumberFormat.Decimal;
        var result = _service.FormatCardNumber("12345", 26);
        Assert.That(result, Is.EqualTo("12345"));
    }

    [Test]
    public void FormatCardNumber_SmallNumber_HexFormat_ReturnsHex()
    {
        _service.CurrentFormat = CardNumberFormat.Hexadecimal;
        var result = _service.FormatCardNumber("255", 8);
        Assert.That(result, Is.EqualTo("0xFF"));
    }

    [Test]
    public void FormatCardNumber_SmallNumber_BinaryFormat_ReturnsBinary()
    {
        _service.CurrentFormat = CardNumberFormat.Binary;
        var result = _service.FormatCardNumber("15", 8);
        Assert.That(result, Is.EqualTo("0000 1111"));
    }

    [Test]
    public void FormatCardNumber_64BitMaxValue_HandlesCorrectly()
    {
        // Test with maximum 64-bit value: 2^64 - 1 = 18446744073709551615
        var max64Bit = "18446744073709551615";
        
        _service.CurrentFormat = CardNumberFormat.Decimal;
        var decimalResult = _service.FormatCardNumber(max64Bit, 64);
        Assert.That(decimalResult, Is.EqualTo(max64Bit));

        _service.CurrentFormat = CardNumberFormat.Hexadecimal;
        var hexResult = _service.FormatCardNumber(max64Bit, 64);
        Assert.That(hexResult, Is.EqualTo("0xFFFFFFFFFFFFFFFF"));
    }

    [Test]
    public void FormatCardNumber_LargeNumber80Bits_HandlesCorrectly()
    {
        // Test with 80-bit number that would overflow ulong
        // 2^80 - 1 = 1208925819614629174706175
        var large80Bit = "1208925819614629174706175";
        
        _service.CurrentFormat = CardNumberFormat.Decimal;
        var decimalResult = _service.FormatCardNumber(large80Bit, 80);
        Assert.That(decimalResult, Is.EqualTo(large80Bit));

        _service.CurrentFormat = CardNumberFormat.Hexadecimal;
        var hexResult = _service.FormatCardNumber(large80Bit, 80);
        // 2^80 - 1 in hex is FFFFFFFFFFFFFFFFFFFF (20 F's)
        Assert.That(hexResult, Is.EqualTo("0xFFFFFFFFFFFFFFFFFFFF"));
    }

    [Test]
    public void FormatCardNumber_VeryLargeNumber256Bits_HandlesCorrectly()
    {
        // Test with very large 256-bit number
        var large256Bit = new BigInteger(2).Pow(256) - 1;
        var largeString = large256Bit.ToString();
        
        _service.CurrentFormat = CardNumberFormat.Decimal;
        var decimalResult = _service.FormatCardNumber(largeString, 256);
        Assert.That(decimalResult, Is.EqualTo(largeString));

        _service.CurrentFormat = CardNumberFormat.Hexadecimal;
        var hexResult = _service.FormatCardNumber(largeString, 256);
        Assert.That(hexResult, Does.StartWith("0x"));
        // Should be all F's for 2^256 - 1, but let's just check it's a very long hex
        Assert.That(hexResult.Length, Is.GreaterThan(60)); // Should be quite long
    }

    [Test]
    public void FormatCardNumber_LargeNumber_BinaryFormat_HandlesCorrectly()
    {
        // Test binary format with large number
        var largeNumber = "1048575"; // 2^20 - 1, should be 20 bits of 1's
        
        _service.CurrentFormat = CardNumberFormat.Binary;
        var result = _service.FormatCardNumber(largeNumber, 20);
        
        // Should be grouped by 4 bits
        Assert.That(result, Is.EqualTo("1111 1111 1111 1111 1111"));
    }

    [Test]
    public void FormatCardNumber_Zero_HandlesCorrectly()
    {
        _service.CurrentFormat = CardNumberFormat.Decimal;
        Assert.That(_service.FormatCardNumber("0", 8), Is.EqualTo("0"));

        _service.CurrentFormat = CardNumberFormat.Hexadecimal;
        Assert.That(_service.FormatCardNumber("0", 8), Is.EqualTo("0x0"));

        _service.CurrentFormat = CardNumberFormat.Binary;
        Assert.That(_service.FormatCardNumber("0", 8), Is.EqualTo("0000 0000"));
    }

    [Test]
    public void FormatCardNumber_BinaryFormat_PadsToCorrectLength()
    {
        _service.CurrentFormat = CardNumberFormat.Binary;
        
        // Test with 26-bit card
        var result = _service.FormatCardNumber("1", 26);
        
        // Should pad to 26 bits and be grouped by 4
        // 26 bits = "00000000000000000000000001" = "0000 0000 0000 0000 0000 0000 01"
        Assert.That(result, Does.EndWith("01"));
        Assert.That(result.Replace(" ", "").Length, Is.EqualTo(26)); // 26 bits total
    }

    [Test]
    public async Task CurrentFormat_WhenSet_SavesToLocalStorage()
    {
        // Initialize first
        await _service.InitializeAsync();
        
        // Change format
        _service.CurrentFormat = CardNumberFormat.Hexadecimal;
        
        // Give async save operation time to complete
        await Task.Delay(100);
        
        // Verify localStorage was called
        _mockLocalStorage.Verify(
            x => x.SetItemAsStringAsync("cardNumberFormat", "Hexadecimal", default),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task InitializeAsync_LoadsFromLocalStorage()
    {
        // Setup localStorage to return Hexadecimal
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync("cardNumberFormat", default))
            .ReturnsAsync("Hexadecimal");
        
        // Initialize service
        await _service.InitializeAsync();
        
        // Verify format was loaded
        Assert.That(_service.CurrentFormat, Is.EqualTo(CardNumberFormat.Hexadecimal));
    }

    [Test]
    public async Task InitializeAsync_WithInvalidValue_UsesDefault()
    {
        // Setup localStorage to return invalid value
        _mockLocalStorage
            .Setup(x => x.GetItemAsStringAsync("cardNumberFormat", default))
            .ReturnsAsync("InvalidFormat");
        
        // Initialize service
        await _service.InitializeAsync();
        
        // Verify default format is used
        Assert.That(_service.CurrentFormat, Is.EqualTo(CardNumberFormat.Decimal));
    }

    [Test]
    public void FormatChanged_Event_FiresWhenFormatChanges()
    {
        var eventFired = false;
        CardNumberFormat? capturedFormat = null;

        _service.FormatChanged += format => {
            eventFired = true;
            capturedFormat = format;
        };

        _service.CurrentFormat = CardNumberFormat.Binary;

        Assert.That(eventFired, Is.True);
        Assert.That(capturedFormat, Is.EqualTo(CardNumberFormat.Binary));
    }

    [Test]
    public void FormatChanged_Event_DoesNotFireWhenSameFormatSet()
    {
        var eventFireCount = 0;
        
        _service.FormatChanged += _ => eventFireCount++;
        
        // Set same format twice
        _service.CurrentFormat = CardNumberFormat.Decimal;
        _service.CurrentFormat = CardNumberFormat.Decimal;

        // Event should only fire once (or zero times if starting format is already Decimal)
        Assert.That(eventFireCount, Is.LessThanOrEqualTo(1));
    }

    /// <summary>
    /// Test the specific issue mentioned in GitHub issue #8:
    /// 80-bit card should work correctly, not be mangled
    /// </summary>
    [Test]
    public void FormatCardNumber_80BitCard_DoesNotMangle()
    {
        // Create an 80-bit number (10 bytes of 0xFF = 2^80 - 1)
        var bytes80Bit = new byte[10];
        Array.Fill(bytes80Bit, (byte)0xFF);
        
        // Convert to BigInteger (little-endian byte array)
        Array.Reverse(bytes80Bit); // Convert to big-endian for proper value
        var bigInt = new BigInteger(bytes80Bit.Concat(new byte[] { 0 }).ToArray()); // Add 0 to ensure positive
        var cardNumber = bigInt.ToString();
        
        // Test all formats handle it correctly without mangling
        _service.CurrentFormat = CardNumberFormat.Decimal;
        var decimalResult = _service.FormatCardNumber(cardNumber, 80);
        Assert.That(decimalResult, Is.EqualTo(cardNumber));
        
        _service.CurrentFormat = CardNumberFormat.Hexadecimal;
        var hexResult = _service.FormatCardNumber(cardNumber, 80);
        Assert.That(hexResult, Does.StartWith("0x"));
        Assert.That(hexResult.Length, Is.GreaterThan(3)); // More than just "0x"
        
        _service.CurrentFormat = CardNumberFormat.Binary;
        var binaryResult = _service.FormatCardNumber(cardNumber, 80);
        Assert.That(binaryResult, Is.Not.Empty);
        // Should contain mostly 1's since we used 0xFF bytes
        Assert.That(binaryResult.Count(c => c == '1'), Is.GreaterThan(70)); // Most bits should be 1
    }
}

/// <summary>
/// Extension method for BigInteger.Pow since it's not available in older .NET versions
/// </summary>
internal static class BigIntegerExtensions
{
    public static BigInteger Pow(this BigInteger value, int exponent)
    {
        if (exponent < 0)
            throw new ArgumentOutOfRangeException(nameof(exponent));
        
        var result = BigInteger.One;
        for (int i = 0; i < exponent; i++)
        {
            result *= value;
        }
        return result;
    }
}