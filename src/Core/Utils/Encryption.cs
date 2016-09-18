using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Core.Utils
{
	public class Encryption
	{
		/// <summary>
		/// Decrypts Base64 string with AES256 (vector 16 bytes, key 32 bytes)
		/// </summary>
		/// <param name="base64"></param>
		/// <param name="vector"></param>
		/// <param name="key"></param>
		/// <returns>Decrypted string</returns>
		public static string DecryptAesString(string base64, string vector, string key)
		{
			string plaintext;
			
			using (var aesAlg = Aes.Create())
			{
				aesAlg.KeySize = 256;

				aesAlg.IV = Encoding.UTF8.GetBytes(vector);
				aesAlg.Key = Encoding.UTF8.GetBytes(key);

				var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
				
				using (var msDecrypt = new MemoryStream(Convert.FromBase64String(base64)))
				{
					using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						using (var srDecrypt = new StreamReader(csDecrypt))
						{
							plaintext = srDecrypt.ReadToEnd();
						}
					}
				}
			}

			return plaintext;
		}

		/// <summary>
		/// Encrypts data with AES256 (vector 16 bytes, key 32 bytes)
		/// </summary>
		/// <param name="data"></param>
		/// <param name="vector"></param>
		/// <param name="key"></param>
		/// <returns>Base64 encrypted string</returns>
		public static string EncryptAesString(string data, string vector, string key)
		{
			byte[] encrypted;

			using (var aesAlg = Aes.Create())
			{
				aesAlg.KeySize = 256;
				
				aesAlg.IV = Encoding.UTF8.GetBytes(vector);
				aesAlg.Key = Encoding.UTF8.GetBytes(key);

				var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
				
				using (var msEncrypt = new MemoryStream())
				{
					using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						using (var swEncrypt = new StreamWriter(csEncrypt))
						{
							swEncrypt.Write(data);
						}
						encrypted = msEncrypt.ToArray();
					}
				}
			}

			return Convert.ToBase64String(encrypted);
		}
	}
}
