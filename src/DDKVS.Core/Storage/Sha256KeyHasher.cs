using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DDKVS.Core.Storage
{
    public class Sha256KeyHasher : IKeyHasher
    {
        public IKeyValidator KeyValidator { get; }
        public Sha256KeyHasher(IKeyValidator keyValidator)
        {
            KeyValidator = keyValidator;
        }
        private byte[] ComputeShaHash(byte[] data)
        {
            using var hasher = SHA256.Create();
            return hasher.ComputeHash(data);
        }
        private byte[] ComputeShaHash(string data)
        {
            return ComputeShaHash(Encoding.UTF8.GetBytes(data));
        }

        public IKey ComputeHash(string key)
        {
            KeyValidator.Validate(key);
            var hash = ComputeShaHash(key);
            var reverseHash = ComputeShaHash(new string(key.Reverse().ToArray()));
            unchecked
            {
                var hashCode = ((uint)key.Length * 31) + (ComputeHashCode(hash) * 31) + ComputeHashCode(reverseHash);
                return new Key(key, hashCode);
            }
        }

        /// <summary>
        /// Ingest a SHA256 hash, and output a stable 32 bit unsigned hash code
        /// </summary>
        /// <param name="hash">Byte array containing 256 bits to compute the hash code from</param>
        /// <returns>Computed hash code</returns>
        private uint ComputeHashCode(byte[] hash)
        {
            var hashSpan = hash.AsSpan();
            uint hashCode = 0;
            for (var x = 0; x < 8; x++)
            {
                var data = hashSpan.Slice(x * 4, 4).ToArray();
                if (!BitConverter.IsLittleEndian) // Make sure the hash is consistent, regardless of endian!
                    Array.Reverse(data);
                hashCode = hashCode * 31 + BitConverter.ToUInt32(data);
            }

            return hashCode;
        }
    }
}