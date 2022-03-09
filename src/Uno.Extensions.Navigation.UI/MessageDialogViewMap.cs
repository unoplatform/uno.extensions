namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record MessageDialogViewMap(
	string? Content = null,
	string? Title = null,
	bool DelayUserInput = false,
	int DefaultButtonIndex = 0,
	int CancelButtonIndex = 0,
	DialogAction[]? Buttons = null,
	Type? ViewModel = null,
	DataMap? Data = null,
	Type? ResultData = null
) : ViewMap<MessageDialog>(ViewModel, Data, ResultData)
{
}

public record MessageDialogViewMap<TViewModel>(
	string? Content = null,
	string? Title = null,
	bool DelayUserInput = false,
	int DefaultButtonIndex = 0,
	int CancelButtonIndex = 0,
	DialogAction[]? Buttons = null,
	DataMap? Data = null,
	Type? ResultData = null
) : MessageDialogViewMap(
	Content, Title, DelayUserInput,
	DefaultButtonIndex, CancelButtonIndex, Buttons,
	typeof(TViewModel), Data, ResultData)
{
}

public record DialogAction(string? Label = "", Action? Action = null, object? Id = null) { }

#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

