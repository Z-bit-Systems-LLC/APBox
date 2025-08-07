using System.Numerics;

namespace ApBox.Core.Tests.OSDP;

[TestFixture]
[Category("Unit")]
public class CardNumberConversionTests
{
    [Test]
    public void ConvertCardDataToNumber_EmptyData_ReturnsZero()
    {
        var result = ConvertCardDataToNumber(Array.Empty<byte>());
        
        Assert.That(result, Is.EqualTo("0"));
    }
    
    [Test]
    public void ConvertCardDataToNumber_StandardWiegand26_ConvertsCorrectly()
    {
        // Standard Wiegand 26-bit card number (3 bytes + 2 padding bits)
        // Card number 12345678 in binary: 00000000 10111100 01100001 01001110
        var cardData = new byte[] { 0x00, 0xBC, 0x61, 0x4E };
        var result = ConvertCardDataToNumber(cardData);
        
        Assert.That(result, Is.EqualTo("12345678"));
    }
    
    [Test]
    public void ConvertCardDataToNumber_LargeNumber_HandlesCorrectly()
    {
        // Test with a large number that would overflow standard integer types
        // 16 bytes = 128 bits, well within the 200-bit requirement
        var cardData = new byte[] 
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };
        
        var result = ConvertCardDataToNumber(cardData);
        
        // This should be a very large number (2^128 - 1)
        var expected = BigInteger.Parse("340282366920938463463374607431768211455");
        Assert.That(result, Is.EqualTo(expected.ToString()));
    }
    
    [Test]
    public void ConvertCardDataToNumber_VeryLargeNumber200Bits_HandlesCorrectly()
    {
        // Test with maximum size - 25 bytes = 200 bits
        var cardData = new byte[25];
        for (int i = 0; i < cardData.Length; i++)
        {
            cardData[i] = (byte)(i + 1); // Sequential pattern for testing
        }
        
        var result = ConvertCardDataToNumber(cardData);
        
        // Should handle this without throwing and return a valid number string
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(BigInteger.Parse(result), Is.GreaterThan(BigInteger.Zero));
    }
    
    [Test]
    public void ConvertCardDataToNumber_SingleByte_ConvertsCorrectly()
    {
        var cardData = new byte[] { 0xFF };
        var result = ConvertCardDataToNumber(cardData);
        
        Assert.That(result, Is.EqualTo("255"));
    }
    
    [Test]
    public void ConvertCardDataToNumber_ByteOrderHandling_ConvertsCorrectly()
    {
        // Test that byte order conversion works correctly
        // Big-endian input: 0x01 0x02 should become little-endian for BigInteger
        var cardData = new byte[] { 0x01, 0x02 };
        var result = ConvertCardDataToNumber(cardData);
        
        // 0x01 0x02 in big-endian = 258 decimal
        // When reversed for little-endian BigInteger: 0x02 0x01 = 513 decimal
        Assert.That(result, Is.EqualTo("258"));
    }
    
    [Test]
    public void ConvertCardDataToNumber_SmallNumbers_ConvertCorrectly()
    {
        // Test some small numbers to verify conversion works correctly
        var testCases = new[]
        {
            (new byte[] { 0x01 }, "1"),
            (new byte[] { 0x0A }, "10"), 
            (new byte[] { 0x64 }, "100"),
            (new byte[] { 0x01, 0x00 }, "256"), // 0x01 0x00 in big-endian = 256
            (new byte[] { 0x01, 0x01 }, "257")  // 0x01 0x01 in big-endian = 257
        };
        
        foreach (var (input, expected) in testCases)
        {
            var result = ConvertCardDataToNumber(input);
            Assert.That(result, Is.EqualTo(expected), $"Failed for input {Convert.ToHexString(input)}");
        }
    }
    
    /// <summary>
    /// This is a copy of the ConvertWiegandToCardNumber method from OsdpDevice
    /// for isolated testing without needing the full OSDP infrastructure
    /// </summary>
    private string ConvertWiegandToCardNumber(System.Collections.BitArray? cardData)
    {
        if (cardData == null || cardData.Length == 0) return "0";
        
        // Convert bit array to binary string for processing
        var bitString = BuildRawBitString(cardData);
        
        try
        {
            // Convert binary string to decimal using BigInteger for large number support
            if (bitString.All(c => c == '0'))
            {
                return "0";
            }
            
            var cardNumber = BigInteger.Zero;
            var powerOfTwo = BigInteger.One;
            
            // Process bits from right to left (least significant to most significant)
            for (int i = bitString.Length - 1; i >= 0; i--)
            {
                if (bitString[i] == '1')
                {
                    cardNumber += powerOfTwo;
                }
                powerOfTwo *= 2;
            }
            
            return cardNumber.ToString();
        }
        catch (Exception)
        {
            return "0";
        }
    }
    
    /// <summary>
    /// Converts a BitArray to a binary string representation (as per Aporta WiegandCredentialHandler)
    /// </summary>
    private static string BuildRawBitString(System.Collections.BitArray cardData)
    {
        var cardNumberBuilder = new System.Text.StringBuilder();
        foreach (bool bit in cardData)
        {
            cardNumberBuilder.Append(bit ? "1" : "0");
        }
        return cardNumberBuilder.ToString();
    }
    
    private string ConvertCardDataToNumber(byte[] data)
    {
        // Convert byte array to BitArray for proper Wiegand processing
        if (data.Length == 0) return "0";
        
        var bitArray = CreateWiegandBitArray(data);
        return ConvertWiegandToCardNumber(bitArray);
    }
    
    /// <summary>
    /// Creates a BitArray from byte data that matches how OSDP.Net provides card data.
    /// The BitArray constructor creates bit-level little-endian representation where
    /// bits are reversed within each byte, so we need to correct this for Wiegand data.
    /// </summary>
    private static System.Collections.BitArray CreateWiegandBitArray(byte[] cardData)
    {
        // Since we're simulating what OSDP.Net would provide,
        // we need to account for the BitArray constructor's bit ordering
        var correctedBytes = new byte[cardData.Length];
        for (int i = 0; i < cardData.Length; i++)
        {
            correctedBytes[i] = ReverseBits(cardData[i]);
        }
        return new System.Collections.BitArray(correctedBytes);
    }
    
    /// <summary>
    /// Reverses the bits within a byte
    /// </summary>
    private static byte ReverseBits(byte b)
    {
        byte result = 0;
        for (int i = 0; i < 8; i++)
        {
            result = (byte)((result << 1) | (b & 1));
            b >>= 1;
        }
        return result;
    }
}