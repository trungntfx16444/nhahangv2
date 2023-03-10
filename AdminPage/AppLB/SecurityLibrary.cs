namespace AdminPage.AppLB
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// AES 256 and Base64 using CryptoAPI.
    /// </summary>
    public class SecurityLibrary
    {
        public static string Md5Encrypt(string strEncrypt)
        {
            MD5 md5Service = MD5.Create();
            byte[] data = Encoding.ASCII.GetBytes(strEncrypt);

            data = md5Service.ComputeHash(data);

            var sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        #region Generate salt and key

        // webphukhang@2019
        private static string defaultPass = System.Configuration.ConfigurationManager.AppSettings["PassCode"];

        private static string GenerateSaltKey(string password)
        {
            var rfc2898db = new Rfc2898DeriveBytes(password, 16, 10000);

            byte[] data = new byte[48];
            Buffer.BlockCopy(rfc2898db.Salt, 0, data, 0, 16);
            Buffer.BlockCopy(rfc2898db.GetBytes(32), 0, data, 16, 32);
            return Convert.ToBase64String(data);
        }

        private static byte[] GenerateKey(string password, byte[] salt)
        {
            var rfc2898db = new Rfc2898DeriveBytes(password, salt, 10000);
            return rfc2898db.GetBytes(32);
        }

        #endregion

        public static string Encrypt(string plain)
        {
            try
            {
                if (plain == null || plain.Length == 0)
                {
                    return null;
                }

                byte[] encrypted;
                byte[] data = Encoding.UTF8.GetBytes(plain);

                string saltKeyStr = GenerateSaltKey(defaultPass);
                byte[] saltKeyB = Convert.FromBase64String(saltKeyStr);
                byte[] salt = new byte[16];
                byte[] key = new byte[32];
                Buffer.BlockCopy(saltKeyB, 0, salt, 0, 16);
                Buffer.BlockCopy(saltKeyB, 16, key, 0, 32);
                saltKeyStr = null;
                saltKeyB = null;

                using (var ms = new MemoryStream())
                {
                    using (var aes256 = new AesCryptoServiceProvider())
                    {
                        aes256.KeySize = 256;
                        aes256.BlockSize = 128;
                        aes256.GenerateIV();
                        aes256.Padding = PaddingMode.PKCS7;
                        aes256.Mode = CipherMode.CBC;
                        aes256.Key = key;
                        key = null;

                        using (var cs = new CryptoStream(ms, aes256.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            ms.Write(aes256.IV, 0, aes256.IV.Length);
                            ms.Write(salt, 0, 16);
                            cs.Write(data, 0, plain.Length);
                        }
                    }

                    encrypted = ms.ToArray();
                }

                return Convert.ToBase64String(encrypted);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string Decrypt(string cipher)
        {
            try
            {
                if (cipher == null || cipher.Length == 0)
                {
                    return null;
                }

                byte[] decrypted;

                // byte[] data = Convert.FromBase64String(cipher);
                byte[] data = Convert.FromBase64String(cipher.Replace(" ", "+"));

                using (var ms = new MemoryStream(data))
                {
                    using (var aes256 = new AesCryptoServiceProvider())
                    {
                        byte[] iv = new byte[16];
                        byte[] salt = new byte[16];
                        ms.Read(iv, 0, 16);
                        ms.Read(salt, 0, 16);

                        aes256.KeySize = 256;
                        aes256.BlockSize = 128;
                        aes256.IV = iv;
                        aes256.Padding = PaddingMode.PKCS7;
                        aes256.Mode = CipherMode.CBC;
                        aes256.Key = GenerateKey(defaultPass, salt);

                        using (var cs = new CryptoStream(ms, aes256.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            byte[] temp = new byte[ms.Length - 16 - 16 + 1];
                            decrypted = new byte[cs.Read(temp, 0, temp.Length)];
                            Buffer.BlockCopy(temp, 0, decrypted, 0, decrypted.Length);
                        }
                    }
                }

                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}