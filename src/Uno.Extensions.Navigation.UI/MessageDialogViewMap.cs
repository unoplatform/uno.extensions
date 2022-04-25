namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record MessageDialogAttributes(
	string? Content = null,
	string? Title = null,
	bool DelayUserInput = false,
	int DefaultButtonIndex = 0,
	int CancelButtonIndex = 0,
	DialogAction[]? Buttons = null
)
{
}


public record MessageDialogViewMap(
	MessageDialogAttributes? attributes = null,
	Type? ViewModel = null,
	DataMap? Data = null,
	Type? ResultData = null
) : ViewMap<MessageDialog>(ViewModel, Data, ResultData, attributes)
{
}

public record MessageDialogViewMap<TViewModel>(
	MessageDialogAttributes? attributes = null,
	DataMap? Data = null,
	Type? ResultData = null
) : MessageDialogViewMap(
	attributes,
	typeof(TViewModel), Data, ResultData)
{
}


#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

