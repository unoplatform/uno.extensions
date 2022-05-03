namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record FlyoutAttributes(
	bool AutoDismiss = true
)
{
}


public record FlyoutViewMap<TFlyout>(
	bool AutoDismiss = true,
	Type? ViewModel = null,
	DataMap? Data = null,
	Type? ResultData = null
) : ViewMap<TFlyout>(
		ViewModel,
		Data,
		ResultData,
		new FlyoutAttributes(AutoDismiss)
		)
{
}

public record FlyoutViewMap<TFlyout,TViewModel>(
	bool AutoDismiss = true,
	DataMap? Data = null,
	Type? ResultData = null
) : FlyoutViewMap<TFlyout>(AutoDismiss,typeof(TViewModel), Data, ResultData)
{
}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

