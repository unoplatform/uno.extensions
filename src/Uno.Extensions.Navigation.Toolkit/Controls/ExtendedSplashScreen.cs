using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Toolkit.Controls;

[TemplatePart(Name = SplashScreenPresenterPartName, Type = typeof(ContentPresenter))]
public partial class ExtendedSplashScreen : LoadingView
{
	private const string SplashScreenPresenterPartName = "SplashScreenPresenter";

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		if (GetTemplateChild(SplashScreenPresenterPartName) is ContentPresenter splashScreenPresenter)
		{
			splashScreenPresenter.Content = GetNativeSplashScreen();
		}
	}
}
