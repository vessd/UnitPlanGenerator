using System;
using System.Security.Cryptography;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.Services
{
    /// <summary>Реализует функции хеширования пароля.</summary>
    /// <seealso cref="IPasswordHasher" />
    public class PasswordHasher : IPasswordHasher
    {
        private const int Iterations = 10000;
        private const int SaltLength = 16;
        private const int HashLength = 20;
        private const int Base64Length = 4 * ((SaltLength + HashLength + 2) / 3);

        /// <summary>Создает хеш пароля.</summary>
        /// <param name="password">Пользовательский пароль.</param>
        /// <returns>Хешированный пароль.</returns>
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            byte[] salt = new byte[SaltLength];
            byte[] hash;
            byte[] hashBytes = new byte[SaltLength + HashLength];

            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                hash = pbkdf2.GetBytes(HashLength);
            }

            Array.Copy(salt, 0, hashBytes, 0, SaltLength);
            Array.Copy(hash, 0, hashBytes, SaltLength, HashLength);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>Проверяет, соответствует ли пароль хешу.</summary>
        /// <param name="hashedPassword">Хеш, созданный функцией <see cref="HashPassword(string)"/></param>
        /// <param name="providedPassword">Пользовательский пароль.</param>
        /// <returns>Значение <see langword="true" />, если указанный объект равен текущему объекту; в противном случае — значение <see langword="false" />.</returns>
        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
            {
                throw new ArgumentNullException(nameof(hashedPassword));
            }

            if (hashedPassword.Length != Base64Length)
            {
                throw new ArgumentException(string.Format("Длина хеша должна быть равна {0}", Base64Length), nameof(hashedPassword));
            }

            if (string.IsNullOrEmpty(providedPassword))
            {
                throw new ArgumentNullException(nameof(providedPassword));
            }

            byte[] hashBytes = Convert.FromBase64String(hashedPassword);
            byte[] salt = new byte[SaltLength];
            byte[] hash = new byte[HashLength];
            byte[] testHash;

            Array.Copy(hashBytes, 0, salt, 0, SaltLength);
            Array.Copy(hashBytes, SaltLength, hash, 0, HashLength);

            using (var pbkdf2 = new Rfc2898DeriveBytes(providedPassword, salt, Iterations))
            {
                testHash = pbkdf2.GetBytes(HashLength);
            }

            return SlowEquals(hash, testHash);
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
    }
}
