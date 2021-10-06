using System.Text;

namespace Uno.Extensions.Navigation;

public static class RouteConstants
{
    public static class Schemes
    {
        public const string Current = "";
        public const string Nested = ".";
        public const string Parent = "..";
        public static string[] All = new string[] { Parent, Current, Nested };
    }

    public static class RelativePath
    {
        public const char GoBack = '<';
        public const char GoForward = '>';
        public const char Root = '/';
        public const string DialogPrefix = "__dialog__";

        public static string Parent(int numberOfLevels = 1) => MultipleLevels(RouteConstants.Schemes.Parent + "/", string.Empty, numberOfLevels);

        public static string Back(int numberOfLevels = 1) => MultipleLevels(GoBack + string.Empty, string.Empty, numberOfLevels);

        private static string MultipleLevels(string pathToRepeat, string separator, int numberOfLevels)
        {
            var sb = new StringBuilder((pathToRepeat.Length * numberOfLevels) + (separator.Length * (numberOfLevels - 1)));
            sb.Append(pathToRepeat);
            for (int i = 0; i < numberOfLevels - 1; i++)
            {
                sb.Append(separator + pathToRepeat);
            }
            return sb.ToString();
        }
    }

    public const string MessageDialogUri = "__md__";
    public const string MessageDialogParameterContent = MessageDialogUri + "content";
    public const string MessageDialogParameterTitle = MessageDialogUri + "title";
    public const string MessageDialogParameterOptions = MessageDialogUri + "options";
    public const string MessageDialogParameterDefaultCommand = MessageDialogUri + "default";
    public const string MessageDialogParameterCancelCommand = MessageDialogUri + "cancel";
    public const string MessageDialogParameterCommands = MessageDialogUri + "commands";
}
