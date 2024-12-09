using System.Security.Cryptography;

namespace BlazorHybridApp.Core;

public class HashHelper
{
    public static string ComputeHash(byte[] data)
    {
        // Use the most efficient method with SHA256
        using SHA256 sha256 = SHA256.Create();

        // Compute the hash directly from the byte array
        byte[] hashBytes = sha256.ComputeHash(data);

        // Convert hash bytes to a hexadecimal string
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}