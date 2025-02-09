using System.Security.Cryptography;
using System.Text;

namespace RepportingApp.Request_Connection_Core;

public static class RandomGenerator
{
    const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
    private static readonly Random _random = new Random();
    
    public static string GenerateRandomHexString(int length)
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        var tokenData = new byte[length / 2];
        rng.GetBytes(tokenData);
        var result = new StringBuilder(length);
        foreach (byte b in tokenData)
        {
            result.Append(b.ToString("x2"));
        }
        return result.ToString();
    }
    
    public static int GetRandomBetween2Numbers(int one, int two)
    {
        if (one > two) 
        {
            (one, two) = (two, one);
        }

        var rnd = new Random();
        return rnd.Next(one, two);
    }

}