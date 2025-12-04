using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
internal record MessageDialogAttributes(
	Func<IStringLocalizer?, string?>? ContentProvider = default,
	Func<IStringLocalizer?, string?>? TitleProvider = default,
	bool DelayUserInput = false,
	int DefaultButtonIndex = 0,
	int CancelButtonIndex = 0,
	LocalizableDialogAction[]? Buttons = default
)
{
}


public record MessageDialogViewMap(
	string? Content = default,
	string? Title = default,
	bool DelayUserInput = false,
	int DefaultButtonIndex = 0,
	int CancelButtonIndex = 0,
	DialogAction[]? Buttons = default,
	[param:   DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicConstructors)]
	Type? ViewModel = default,
	DataMap? Data = default,
	Type? ResultData = default
) : ViewMap(
		View: typeof(MessageDialog),
		ViewSelector: null,
		ViewModel: ViewModel,
		Data: Data,
		ResultData: ResultData,
		ViewAttributes: new MessageDialogAttributes(
			ContentProvider: _ => Content,
			TitleProvider: _ => Title,
			DelayUserInput,
			DefaultButtonIndex,
			CancelButtonIndex,
			Buttons)
		)
{
}

public record LocalizableMessageDialogViewMap(
	Func<IStringLocalizer?, string?>? Content = default,
	Func<IStringLocalizer?, string?>? Title = default,
	bool DelayUserInput = false,
	int DefaultButtonIndex = 0,
	int CancelButtonIndex = 0,
	LocalizableDialogAction[]? Buttons = default,
	[param:   DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicConstructors)]
	Type? ViewModel = default,
	DataMap? Data = default,
	Type? ResultData = default
) : ViewMap(
		View: typeof(MessageDialog),
		ViewSelector: null,
		ViewModel: ViewModel,
		Data: Data,
		ResultData: ResultData,
		ViewAttributes: new MessageDialogAttributes(
			ContentProvider: Content,
			TitleProvider: Title,
			DelayUserInput,
			DefaultButtonIndex,
			CancelButtonIndex,
			Buttons)
		)
{
}


#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

