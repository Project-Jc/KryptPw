using System.Windows.Media;

namespace KryptPw
{
    public static class Colour
    {
        public const string Comment = "#FF278B27";
        public const string Error = "#FFFF0000";
        public const string FunctionTemplate = "#FFC8C8C8";
        public const string Macros = "#FFBD63C5";
        public const string NewAndDelete = "#FF569CD6";
        public const string UserTypes = "#FF4EC9B0";
        public const string StringLiterals = "#FFD69D85";
        public const string Enum = "#FFB8D7A3";

        public static SolidColorBrush SolidColorBrush(string hex)
        {
            return new BrushConverter().ConvertFromString(hex) as SolidColorBrush;
        }

        public static SolidColorBrush SolidColorBrush(EncryptDecryptOperation encryptDecryptOperation)
        {
            if (encryptDecryptOperation == EncryptDecryptOperation.Encrypt)
                return SolidColorBrush(Comment);

            return SolidColorBrush(StringLiterals);
        }
    }
}
