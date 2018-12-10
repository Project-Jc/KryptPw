using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;


namespace KryptPw
{
    public class kPw
    {
        public static EncryptDecryptOperation CurrentOperation { get; set; }

        public static kPwSettings Settings;


        public static ObservableCollection<Entry> Entries;

        public object this[int index] { get => Entries[index]; set => Entries[index] = value as Entry; }


        public static bool EncryptDecryptDone { get; set; }

        public static bool UnsavedChangesMade => EntryPropertyChanged;

        public static bool EntryPropertyChanged { get; set; }

        public static bool ProgramExitRequested { get; set; }

        public static void ShowExceptionError(params string[] args)
        {
            StringBuilder stringBuilder = new StringBuilder();

            Dictionary<int, string> keyValuePairs = new Dictionary<int, string>
                {
                    { 0, "Method" },
                    { 1, "Message" },
                    { 2, "InnerException" }
                };

            for (int i = 0; i < args.Count(); i++)
                stringBuilder.AppendLine($"{ keyValuePairs[i] } { args[i] }{ Environment.NewLine }");

            MessageBox.Show(stringBuilder.ToString());
        }
    }

    public class Entry
    {
        private string _service;

        public string Service
        {
            get => _service;

            set
            {
                _service = value;
                kPw.EntryPropertyChanged = true;
            }
        }

        private string _username;

        public string Username
        {
            get => _username;

            set
            {
                _username = value;
                kPw.EntryPropertyChanged = true;
            }
        }

        private string _password;

        public string Password
        {
            get => _password;

            set
            {
                _password = value;
                kPw.EntryPropertyChanged = true;
            }
        }

        private string _pin;

        public string Pin
        {
            get => _pin;

            set
            {
                _pin = value;
                kPw.EntryPropertyChanged = true;
            }
        }

        private string _misc;

        public string Misc
        {
            get => _misc;

            set
            {
                _misc = value;
                kPw.EntryPropertyChanged = true;
            }
        }
    }

    public enum EncryptDecryptResult
    {
        EncryptSuccess,
        EncryptFailed,

        DecryptSuccess,
        DecryptFailed
    }

    public enum EncryptDecryptOperation
    {
        Encrypt,
        Decrypt
    }
}
