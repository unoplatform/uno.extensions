namespace Uno.Extensions.Navigation;

public static class RouteConstants
{
    public const string DialogPrefix = "xxxxdialogxxxx";

    public const string MessageDialogUri = "xxxxmessagedialogxxxx";
    public const string MessageDialogParameterContent = MessageDialogUri + "content";
    public const string MessageDialogParameterTitle = MessageDialogUri + "title";
    public const string MessageDialogParameterOptions = MessageDialogUri + "options";
    public const string MessageDialogParameterDefaultCommand = MessageDialogUri + "default";
    public const string MessageDialogParameterCancelCommand = MessageDialogUri + "cancel";
    public const string MessageDialogParameterCommands = MessageDialogUri + "commands";

    private const string PickerPrefix = "xxxxpickerxxxxx";
    public const string PickerItemsSource = PickerPrefix + "itemssource";
    public const string PickerItemTemplate = PickerPrefix + "itemtemplate";

    public const string PopupShow = "Show";
}
