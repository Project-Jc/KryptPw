using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using EncryptDecrypt;

namespace KryptPw
{
    public static class kPwFile
    {
        private static string _Path;

        public static string Path
        {
            get => _Path;

            set
            {
                _Path = value;
                Name = System.IO.Path.GetFileName(_Path);
                kPw.Settings.PreviouslyLoadedFile = _Path;
                TmpPath = string.Empty;
            }
        }

        public static string Name { get; set; }

        private static string _TmpPath;

        public static string TmpPath
        {
            get => _TmpPath;

            set
            {
                _TmpPath = value;
                if (!string.IsNullOrEmpty(_TmpPath))
                    TmpName = System.IO.Path.GetFileName(_TmpPath);
            }
        }

        public static string TmpName { get; set; }


        public static string TmpPass { get; set; }

        public static string Pass { get; set; }


        public static int DecryptAttempts { get; set; }


        public static void SaveSettingsToXmlFile(string filePath)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(kPwSettings));

                using (TextWriter textWriter = new StreamWriter(filePath))
                {
                    xmlSerializer.Serialize(textWriter, kPw.Settings);
                }
            }
            catch (Exception e)
            {
                //kPw.ShowExceptionError("SaveSettings", e.Message, e.InnerException.ToString());
            }
        }

        public static void SaveDataToXmlFile(string filePath)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObservableCollection<Entry>));

            try
            {
                using (TextWriter textWriter = new StreamWriter(filePath))
                {
                    xmlSerializer.Serialize(textWriter, kPw.Entries);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"\"SaveDataToXmlFile()\" failed.\n\n{ e.Message }\n\n{ e.InnerException }");
            }
        }

        private static bool TryXmlSerializeData(out MemoryStream memoryStream)
        {
            memoryStream = new MemoryStream();

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObservableCollection<Entry>));

            try
            {
                xmlSerializer.Serialize(memoryStream, kPw.Entries);

                memoryStream.Position = 0;
            }
            catch (Exception e)
            {
                //kPw.ShowExceptionError("TryXmlSerializeData", e.Message, e.InnerException.ToString());

                return false;
            }

            return true;
        }


        private static bool TrySaveDataToEncryptedFile(string filePath, string password)
        {
            bool serializeResult = TryXmlSerializeData(out var memoryStream);

            using (memoryStream)
            {
                if (serializeResult)
                {
                    //GCHandle gch = GCHandle.Alloc(password, GCHandleType.Pinned);

                    bool encryptResult = EncDec.TryCreateEncryptedFile(memoryStream, filePath, password);

                    //gch.Free();  

                    return encryptResult;
                }
            }

            ////Security.ZeroMemory(gch.AddrOfPinnedObject(), kPw.Password.Length * 2);

            return false;
        }

        public static EncryptDecryptResult SaveDataToEncryptedFile(string filePath, string password)
        {
            return TrySaveDataToEncryptedFile(filePath, password) ? EncryptDecryptResult.EncryptSuccess : EncryptDecryptResult.EncryptFailed;
        }


        public static void LoadEntries(object obj, bool clearFirst = true)
        {
            if (clearFirst)
                kPw.Entries.Clear();

            try
            {
                foreach (var v in (obj as ObservableCollection<Entry>))
                    kPw.Entries.Add(v);
            }
            catch (Exception e)
            {
                kPw.ShowExceptionError("LoadEntries()", e.Message, e.InnerException.ToString());
            }
        }

        public static void LoadEntries(object obj, ref ObservableCollection<Entry> collectionRef, bool clearFirst = true)
        {
            if (clearFirst)
                collectionRef.Clear();

            try
            {
                foreach (var v in (obj as ObservableCollection<Entry>))
                    collectionRef.Add(v);
            }
            catch (Exception e)
            {
                kPw.ShowExceptionError("LoadEntries()", e.Message, e.InnerException.ToString());
            }
        }


        private static async Task<bool> TryLoadDataFromEncryptedFileAsync(string password)
        {
            GCHandle gch = GCHandle.Alloc(password, GCHandleType.Pinned);

            var memoryStream = await EncDec.DecryptFileAsync(TmpPath, password);

            gch.Free();

            if (memoryStream != null)
            {
                using (memoryStream)
                {
                    var entries = await XmlDeserializeAsync(memoryStream);

                    if (entries != null)
                    {
                        LoadEntries(entries, ref kPw.Entries);

                        return true;
                    }
                }
            }

            return false;
        }

        public static async Task<EncryptDecryptResult> LoadDataFromEncryptedFileAsync(string password)
        {
            //GCHandle gch = GCHandle.Alloc(password, GCHandleType.Pinned);

            var memoryStream = await EncDec.DecryptFileAsync(TmpPath, password);

            //gch.Free();

            if (memoryStream != null)
            {
                using (memoryStream)
                {
                    var entries = await XmlDeserializeAsync(memoryStream);

                    if (entries != null)
                    {
                        LoadEntries(entries, ref kPw.Entries);

                        return EncryptDecryptResult.DecryptSuccess;
                    }
                }
            }

            return EncryptDecryptResult.DecryptFailed;
        }

        private static bool TryLoadDataFromEncryptedFile(string password)
        {
            // GCHandle gch = GCHandle.Alloc(password, GCHandleType.Pinned);

            bool decryptResult = EncDec.TryDecryptFile(TmpPath, password, out var memoryStream);

            //gch.Free();

            using (memoryStream)
            {
                if (decryptResult)
                {
                    if (TryXmlDeserialize(memoryStream, out var obj))
                    {
                        LoadEntries(obj, ref kPw.Entries);

                        return true;
                    }
                }
            }

            //Security.ZeroMemory(gch.AddrOfPinnedObject(), kPw.Password.Length * 2);

            return false;
        }

        public static EncryptDecryptResult LoadDataFromEncryptedFile(string password)
        {
            return TryLoadDataFromEncryptedFile(password) ? EncryptDecryptResult.DecryptSuccess : EncryptDecryptResult.DecryptFailed;
        }


        private static bool TryXmlDeserialize(MemoryStream memoryStream, out object obj)
        {
            obj = null;

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObservableCollection<Entry>));

            try
            {
                obj = xmlSerializer.Deserialize(memoryStream);
            }
            catch (Exception e)
            {
                //kPw.ShowExceptionError("TryXmlDeserialize()", e.Message, e.InnerException.ToString());

                return false;
            }

            return true;
        }

        public static async Task<ObservableCollection<Entry>> XmlDeserializeAsync(MemoryStream memoryStream)
        {
            ObservableCollection<Entry> entries = null;

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObservableCollection<Entry>));

            try
            {
                entries = await Task.Run(() =>
                {
                    return (ObservableCollection<Entry>)xmlSerializer.Deserialize(memoryStream);
                });
            }
            catch (Exception e)
            {
                kPw.ShowExceptionError("XmlDeserializeAsync()", e.Message, e.InnerException.ToString());

                return null;
            }

            return entries;
        }

        public static void LoadDataFromXmlFile(string filePath)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObservableCollection<Entry>));

                using (TextReader textReader = new StreamReader(filePath))
                {
                    var v = xmlSerializer.Deserialize(textReader);

                    foreach (var b in v as ObservableCollection<Entry>)
                        kPw.Entries.Add(b);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"\"LoadDataFromXmlFile()\" failed.\n\n{ e.Message }\n\n{ e.InnerException }");
            }
        }

        public static void LoadSettingsFromXmlFile(string filePath)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(kPwSettings));

                using (TextReader textWriter = new StreamReader(filePath))
                {
                    kPw.Settings = xmlSerializer.Deserialize(textWriter) as kPwSettings;
                }
            }
            catch (Exception e)
            {
                //kPw.ShowExceptionError("SaveSettings", e.Message, e.InnerException.ToString());
            }
        }
    }

}
