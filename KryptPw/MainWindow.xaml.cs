using System;
using System.Windows;
using System.IO;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Windows.Input;
using System.Xml.Serialization;

namespace KryptPw
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            PasswordPanel.Visibility = Visibility.Hidden;

            kPw.Settings = new kPwSettings();

            kPw.Entries = new ObservableCollection<Entry>();

            MainDataGrid.ItemsSource = kPw.Entries;

            kPwFile.LoadSettingsFromXmlFile($@"{ Environment.CurrentDirectory }\Settings.xml");           
        }

        private void ShowPasswordPanel(EncryptDecryptOperation encryptDecryptOperation, string filePath)
        {
            kPw.EncryptDecryptDone = false;

            PasswordPanelOperation.Content = encryptDecryptOperation.ToString();

            PasswordPanelOperation.Foreground = Colour.SolidColorBrush(encryptDecryptOperation);

            PasswordPanelFileName.Content = kPwFile.TmpName;

            PasswordPanel.Visibility = Visibility.Visible;

            PasswordPanelEntry.Focus();
        }

        private void HidePasswordPanel()
        {
            kPw.EncryptDecryptDone = true;
            kPwFile.DecryptAttempts = 0;

            PasswordResultPanelText.Content = string.Empty;
            PasswordResultPanelMiscInfo.Content = string.Empty;

            PasswordPanel.Visibility = Visibility.Hidden;
        }

        private void ProcessEncryptDecryptResult(EncryptDecryptResult encryptDecryptResult)
        {
            switch (encryptDecryptResult)
            {
                case EncryptDecryptResult.DecryptFailed:
                case EncryptDecryptResult.EncryptFailed: {

                        if (encryptDecryptResult == EncryptDecryptResult.DecryptFailed)
                        {
                            if (++kPwFile.DecryptAttempts >= 10)
                                Environment.Exit(0);

                            PasswordResultPanelMiscInfo.Content = $"Attempts: { kPwFile.DecryptAttempts }";
                        }

                        PasswordResultPanelText.Content = $"{ kPw.CurrentOperation } failed";
                    }
                    break;


                case EncryptDecryptResult.DecryptSuccess: 
                case EncryptDecryptResult.EncryptSuccess: {

                        if (kPw.ProgramExitRequested)
                            Environment.Exit(0);

                        kPw.EntryPropertyChanged = false;

                        kPw.EncryptDecryptDone = true;

                        kPwFile.Path = kPwFile.TmpPath;
                        kPwFile.TmpPath = string.Empty;

                        kPwFile.Pass = kPwFile.TmpPass;
                        kPwFile.TmpPass = string.Empty;

                        Title = $"KryptPw | { kPwFile.Path }";

                        PasswordResultPanelText.Content = $"{ kPw.CurrentOperation } succeeded!";
                        PasswordResultPanelText.Foreground = Colour.SolidColorBrush(Colour.Comment);

                        if (encryptDecryptResult == EncryptDecryptResult.EncryptSuccess)                         
                            PasswordResultPanelMiscInfo.Content = kPwFile.Path;
                    }
                    break;
            }                             
        }

        #region Buttons

        private void EncryptData_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(kPwFile.Path))
                if (kPwFile.SaveDataToEncryptedFile(kPwFile.Path, kPwFile.Pass) == EncryptDecryptResult.EncryptSuccess)
                    kPw.EntryPropertyChanged = false;
            else if (kPw.Entries.Count > 0)
                EncryptDataAs_Button_Click(EncryptDataAs, new RoutedEventArgs());
            //else
            //    MessageBox.Show("There is no loaded file to overwrite and there is also no entries to save.");
        }

        private void EncryptDataAs_Button_Click(object sender, RoutedEventArgs e)
        {
            if (kPw.Entries.Count > 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = "Data",
                    DefaultExt = ".kPw",
                    Filter = "Data Files(*.kPw)|*.kPw|All(*.*)|*",
                    AddExtension = true
                };

                if (saveFileDialog.ShowDialog() == true)
                    ShowPasswordPanel(kPw.CurrentOperation = EncryptDecryptOperation.Encrypt, kPwFile.TmpPath = saveFileDialog.FileName);
                else
                    kPw.ProgramExitRequested = false;
            }
        }

        private void DecryptData_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
                ShowPasswordPanel(kPw.CurrentOperation = EncryptDecryptOperation.Decrypt, kPwFile.TmpPath = openFileDialog.FileName);
        }

        private void Settings_Button_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion


        #region Window Open/Close

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            kPwFile.SaveSettingsToXmlFile($@"{ Environment.CurrentDirectory }\Settings.xml");

            if (kPw.UnsavedChangesMade)
            {
                var msgBoxResult = MessageBox.Show("Unsaved changes have been made. Do you wish to save?", "KryptPw", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                switch (msgBoxResult)
                {
                    case MessageBoxResult.Yes:
                        EncryptDataAs_Button_Click(EncryptDataAs, new RoutedEventArgs());
                        kPw.ProgramExitRequested = true;
                        goto case MessageBoxResult.Cancel;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainDataGrid.Focus();

            if (!string.IsNullOrEmpty(kPw.Settings.PreviouslyLoadedFile) && File.Exists(kPw.Settings.PreviouslyLoadedFile))
                ShowPasswordPanel(kPw.CurrentOperation = EncryptDecryptOperation.Decrypt, kPwFile.TmpPath = kPw.Settings.PreviouslyLoadedFile);          
        }

        #endregion


        private async void PasswordPanelEntry_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || (e.Key == Key.Return && kPw.EncryptDecryptDone))
            {
                HidePasswordPanel();

                return;
            }

            if (!kPw.EncryptDecryptDone)
            {
                if (e.Key != Key.Return || string.IsNullOrEmpty(PasswordPanelEntry.Text) || string.IsNullOrWhiteSpace(PasswordPanelEntry.Text))
                    return;

                kPwFile.TmpPass = PasswordPanelEntry.Text;

                ProcessEncryptDecryptResult(
                    kPw.CurrentOperation == EncryptDecryptOperation.Encrypt ? 
                    kPwFile.SaveDataToEncryptedFile(kPwFile.TmpPath, kPwFile.TmpPass) :
                    kPwFile.LoadDataFromEncryptedFile((kPwFile.TmpPass)));
            }
        }

        private void PasswordPanelVeil_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HidePasswordPanel();
        }

        private void MainDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //MessageBox.Show("Copy paste!");

                if (Clipboard.ContainsText(TextDataFormat.Text))
                {
                    //MessageBox.Show("Contains CommaSeparatedValue text!");

                    var dataObj = Clipboard.GetDataObject();

                    var array = dataObj.GetData(DataFormats.CommaSeparatedValue);

                    if (array != null)
                    {
                        MessageBox.Show(array.ToString());
                    }
                }
            }
        }

        private void PasswordPanelEntry_GotFocus(object sender, RoutedEventArgs e)
        {
            if (PasswordPanelEntry.Text.Equals("Enter your private key here"))
                PasswordPanelEntry.SelectAll();
        }
    }


}
