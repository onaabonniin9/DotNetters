
using System;
using System.Security.Cryptography;

namespace GenteFit.WPF.Services;

public static class SeguridadContrasenas
{
    private const int Iteraciones = 210_000;

    public static string CrearHash(string contrasena)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            contrasena,
            salt,
            Iteraciones,
            HashAlgorithmName.SHA256,
            32);

        return $"PBKDF2:{Convert.ToBase64String(salt)}:" +
               Convert.ToBase64String(hash);
    }

    public static bool Verificar(string contrasena, string almacenado)
    {
        string[] partes = almacenado.Split(':');

        if (partes.Length != 3 || partes[0] != "PBKDF2")
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(partes[1]);
            byte[] hashOriginal = Convert.FromBase64String(partes[2]);

            if (salt.Length != 16 || hashOriginal.Length != 32)
                return false;

            byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                contrasena,
                salt,
                Iteraciones,
                HashAlgorithmName.SHA256,
                32);

            return CryptographicOperations.FixedTimeEquals(
                hashCalculado, hashOriginal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
