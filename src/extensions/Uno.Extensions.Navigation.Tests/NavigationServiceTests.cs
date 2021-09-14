using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Tests;

[TestClass]
public class NavigationServiceTests : BaseNavigationTests
{
    private int navigationCounter;
    protected override void InitializeServices(IServiceCollection services)
    {
        var mockFrame = new Mock<IStackNavigation>();
        mockFrame
            .Setup(foo => foo.Navigate(typeof(PageOne), null, null))
                .Callback(() => navigationCounter++)
                .Returns(true);
        mockFrame
            .Setup(foo => foo.GoBack(null, null))
                .Callback(() => navigationCounter--);
        services.AddSingleton<IStackNavigation>(mockFrame.Object);

    }

    [TestMethod]
    public void NavigationTest()
    {
        var result = Navigation.Navigate(new NavigationRequest(this, new NavigationRoute(new Uri("PageOne", UriKind.Relative))));
        result.Should().NotBeNull();
        navigationCounter.Should().Be(1);
    }
}

public class PageOne
{ }
