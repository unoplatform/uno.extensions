namespace Uno.Extensions.Navigation;

public static class RouteConstants
{
    public static class RelativePath
    {
        public static string Parent(int numberOfLevels = 1) => MultipleLevels(Qualifiers.Parent, string.Empty, numberOfLevels);

        public static string Back(int numberOfLevels = 1) => MultipleLevels(Qualifiers.NavigateBack + string.Empty, string.Empty, numberOfLevels);

        private static string MultipleLevels(string pathToRepeat, string separator, int numberOfLevels)
        {
            var sb = new StringBuilder((pathToRepeat.Length * numberOfLevels) + (separator.Length * (numberOfLevels - 1)));
            sb.Append(pathToRepeat);
            for (var i = 0; i < numberOfLevels - 1; i++)
            {
                sb.Append(separator + pathToRepeat);
            }
            return sb.ToString();
        }
    }

    public const string DialogPrefix = "__dialog__";

    private const string MessageDialogUri = "__md__";
    public const string MessageDialogParameterContent = MessageDialogUri + "content";
    public const string MessageDialogParameterTitle = MessageDialogUri + "title";
    public const string MessageDialogParameterOptions = MessageDialogUri + "options";
    public const string MessageDialogParameterDefaultCommand = MessageDialogUri + "default";
    public const string MessageDialogParameterCancelCommand = MessageDialogUri + "cancel";
    public const string MessageDialogParameterCommands = MessageDialogUri + "commands";

    private const string PickerPrefix = "__picker__";
    public const string PickerItemsSource = PickerPrefix + "itemssource";
    public const string PickerItemTemplate = PickerPrefix + "itemtemplate";

    public const string PopupShow = "Show";
}
