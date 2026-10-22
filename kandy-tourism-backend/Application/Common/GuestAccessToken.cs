using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace KandyTourism.Api.Application.Common;

/// <summary>
/// Secret that grants access to one guest itinerary. 256 random bits, base64url-encoded.
/// Only its SHA-256 hash is stored, so a database leak does not expose working tokens.
/// </summary>
public static class GuestAccessToken
{
    private const int TokenBytes = 32;
    /// <summary>Length of a base64url-encoded 32-byte value without padding.</summary>
    public const int EncodedLength = 43;

    public static string Generate() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));

    public static byte[] Hash(string token) => SHA256.HashData(Encoding.ASCII.GetBytes(token));

    public static bool IsWellFormed(string? token) =>
        token is { Length: EncodedLength } && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public static bool Matches(string token, byte[] expectedHash) =>
        CryptographicOperations.FixedTimeEquals(Hash(token), expectedHash);
}
