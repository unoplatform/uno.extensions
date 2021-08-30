using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Uno.Extensions.Navigation.Tests;

[TestClass]
public class NavigationServiceExtensionsTests : BaseNavigationTests
{
    private int navigationCounter;

    protected override void InitializeServices(IServiceCollection services)
    {
        var mockFrame = new Mock<IFrameWrapper>(); 
        mockFrame
            .Setup(foo => foo.Navigate(typeof(PageOne), null))
            .Callback(() => navigationCounter++)
            .Returns(true);
        mockFrame
            .Setup(foo => foo.GoBack())
            .Callback(() => navigationCounter--);
        services.AddSingleton<IFrameWrapper>(mockFrame.Object);
    }

    [TestMethod]
    public void NavigateToViewTest()
    {
        var result = Navigation.NavigateToView<PageOne>(this);
        result.Should().NotBeNull();
        result.Request.Route.Path.OriginalString.Should().Be(typeof(PageOne).Name);
        navigationCounter.Should().Be(1);
    }

    [TestMethod]
    public void NavigateToPreviousViewTest()
    {
        var result = Navigation.NavigateToPreviousView(this);
        result.Should().NotBeNull();
        result.Request.Route.Path.OriginalString.Should().Be("..");
        navigationCounter.Should().Be(-1);
    }
}
