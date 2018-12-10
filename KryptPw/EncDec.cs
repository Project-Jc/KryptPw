using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EncryptDecrypt
{
    public static class EncDec
    {
        // Sauce
        // https://ourcodeworld.com/articles/read/471/how-to-encrypt-and-decrypt-files-using-the-aes-encryption-algorithm-in-c-sharp
        //

        private const int HashRate = 50000;

        private static RijndaelManaged DefaultAES()
        {
            return new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CFB
            };
        }

        private static RijndaelManaged DefaultAESWithKey(Rfc2898DeriveBytes rfc2898DeriveBytes)
        {
            var rijndaelManaged = DefaultAES();

            rijndaelManaged.Key = rfc2898DeriveBytes.GetBytes(rijndaelManaged.KeySize / 8);
            rijndaelManaged.IV = rfc2898DeriveBytes.GetBytes(rijndaelManaged.BlockSize / 8);

            return rijndaelManaged;
        }

        /// <summary>
        /// Creates a random salt that will be used to encrypt your file.
        /// </summary>
        /// <returns></returns>
        private static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[32];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < 10; i++)
                    rng.GetBytes(data);
            }

            return data;
        }

        /// <summary>
        /// Attempts to encrypt the contents of a file.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="password"></param>
        /// <param name="newFileExtension"></param>
        /// <param name="overwrite"></param>
        public static bool TryEncryptFile(string inputFile, string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            byte[] salt = GenerateRandomSalt();

            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (FileStream inputFileStream = new FileStream(inputFile, FileMode.Open))
                    {
                        inputFileStream.CopyTo(memoryStream);

                        inputFileStream.SetLength(0);
                    }

                    using (FileStream outputFileStream = new FileStream(inputFile, FileMode.Open))
                    {
                        outputFileStream.Write(salt, 0, salt.Length);

                        var AES = DefaultAESWithKey(new Rfc2898DeriveBytes(passwordBytes, salt, 50000));

                        using (CryptoStream cryptoStream = new CryptoStream(outputFileStream, AES.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            int read;

                            byte[] buffer = new byte[1048576];

                            while ((read = memoryStream.Read(buffer, 0, buffer.Length)) > 0)
                                cryptoStream.Write(buffer, 0, read);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show($"Method: TryEncryptFile() failed.\n\nMessage: { e.Message }\n\nInnerException: { e.InnerException }");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to create a new encrypted file with data taken from a MemoryStream.
        /// </summary>
        /// <param name="memoryStream"></param>
        /// <param name="outputFile"></param>
        /// <param name="password"></param>
        public static bool TryCreateEncryptedFile(MemoryStream memoryStream, string outputFile, string password)
        {
            byte[] salt = GenerateRandomSalt();

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            try
            {
                using (FileStream outputFileStream = new FileStream(outputFile, FileMode.Create))
                {
                    var AES = DefaultAESWithKey(new Rfc2898DeriveBytes(passwordBytes, salt, HashRate));

                    outputFileStream.Write(salt, 0, salt.Length);

                    using (CryptoStream cryptoStream = new CryptoStream(outputFileStream, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (memoryStream)
                        {
                            int read;

                            byte[] buffer = new byte[1048576];

                            while ((read = memoryStream.Read(buffer, 0, buffer.Length)) > 0)
                                cryptoStream.Write(buffer, 0, read);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show($"Method: TryCreateEncryptedFile() failed.\n\nMessage: { e.Message }\n\nInnerException: { e.InnerException }\n\n{ e.HResult }");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a new decrypted file from an encrypted one.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <param name="password"></param>
        public static void CreateDecryptedFile(string inputFile, string outputFile, string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            byte[] salt = new byte[32];

            try
            {
                using (FileStream inputFileStream = new FileStream(inputFile, FileMode.Open))
                {
                    inputFileStream.Read(salt, 0, salt.Length);

                    var AES = DefaultAESWithKey(new Rfc2898DeriveBytes(passwordBytes, salt, 50000));

                    using (CryptoStream cryptoStream = new CryptoStream(inputFileStream, AES.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (FileStream outputFileStream = new FileStream(outputFile, FileMode.Create))
                        {
                            int read;

                            byte[] buffer = new byte[1048576];

                            while ((read = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                                outputFileStream.Write(buffer, 0, read);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show($"Method: CreateDecryptedFile() failed.\n\nMessage: { e.Message }\n\nInnerException: { e.InnerException }");
            }
        }

        /// <summary>
        /// Attempts to decrypt an encrypted file. Overriding the encrypted content of the file with it's decrypted counterpart.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool TryDecryptFile(string inputFile, string password)
        {
            // This method could be improved as the resulting file has loads of weird whitespace?
            //

            bool decryptResult = TryDecryptFile(inputFile, password, out var memoryStream);

            if (memoryStream != null)
            {
                using (memoryStream)
                {
                    if (decryptResult)
                    {
                        using (FileStream inputFileStream = new FileStream(inputFile, FileMode.Truncate))
                        {
                            memoryStream.WriteTo(inputFileStream);

                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Attempts to decrypt an encrypted file and returns the data as a MemoryStream object.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool TryDecryptFile(string inputFile, string password, out MemoryStream memoryStream)
        {
            memoryStream = null;

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            byte[] salt = new byte[32];

            try
            {
                using (FileStream inputFileStream = new FileStream(inputFile, FileMode.Open))
                {
                    inputFileStream.Read(salt, 0, salt.Length);

                    var AES = DefaultAESWithKey(new Rfc2898DeriveBytes(passwordBytes, salt, HashRate));

                    using (CryptoStream cryptoStream = new CryptoStream(inputFileStream, AES.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        memoryStream = new MemoryStream();

                        int read;

                        byte[] buffer = new byte[1048576];

                        while ((read = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                            memoryStream.Write(buffer, 0, read);

                        memoryStream.Position = 0;
                    }
                }
            }
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show($"Method: DecryptFile() failed.\n\nMessage: { e.Message }\n\nInnerException: { e.InnerException }");

                return false;
            }

            return true;
        }

        public static async Task<MemoryStream> DecryptFileAsync(string inputFile, string password)
        {
            MemoryStream memoryStream = null;

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            byte[] salt = new byte[32];

            try
            {
                using (FileStream inputFileStream = new FileStream(inputFile, FileMode.Open))
                {
                    await inputFileStream.ReadAsync(salt, 0, salt.Length);

                    var AES = DefaultAESWithKey(new Rfc2898DeriveBytes(passwordBytes, salt, HashRate));

                    using (CryptoStream cryptoStream = new CryptoStream(inputFileStream, AES.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        memoryStream = new MemoryStream();

                        int read;

                        byte[] buffer = new byte[1048576];

                        while ((read = await cryptoStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            await memoryStream.WriteAsync(buffer, 0, buffer.Length);

                        memoryStream.Position = 0;
                    }
                }
            }
            catch (Exception e)
            {
                //System.Windows.MessageBox.Show($"Method: DecryptFile() failed.\n\nMessage: { e.Message }\n\nInnerException: { e.InnerException }");

                return null;
            }

            return memoryStream;
        }

        /// <summary>
        /// As an additional security measure, call this function to remove the key from memory after use.
        /// </summary>
        /// <param name="Destination"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        [DllImport("KERNEL32.DLL", EntryPoint = "RtlZeroMemory")]
        public static extern bool ZeroMemory(IntPtr Destination, int Length);
    }
}
