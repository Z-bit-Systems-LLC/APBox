namespace ApBox.Core.PacketTracing.Models;

/// <summary>
/// Represents the security state of an OSDP packet.
/// </summary>
public enum PacketSecurityState
{
    /// <summary>
    /// The packet was transmitted in clear text without encryption.
    /// </summary>
    ClearText,

    /// <summary>
    /// The packet was transmitted via secure channel using the default key.
    /// </summary>
    SecureDefaultKey,

    /// <summary>
    /// The packet was transmitted via secure channel using a custom key.
    /// </summary>
    Secure
}
