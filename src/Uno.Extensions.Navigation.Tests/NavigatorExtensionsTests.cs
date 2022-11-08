
namespace Uno.Extensions.Navigation.Tests;

[TestClass]
public class NavigatorExtensionsTests
{
	[TestMethod]
	public async Task RedirectForNavigationDataTest()
	{
		var mockNavigator = new Mock<INavigator>();
		mockNavigator
			.Setup(n => n.NavigateAsync(It.IsAny<NavigationRequest>()))
			.Returns(Task.FromResult<NavigationResponse?>(new NavigationResponse(Success:true)));

		var resolver = new Mock<IRouteResolver>();

		var request = new NavigationRequest(this, new Route(Data: new Dictionary<string, object>() { { "", new object() } } ));


		// There's no matching routemap, so no redirect (ie returns null)
		var response = mockNavigator.Object.RedirectForNavigationData(resolver.Object, request);
		response.Should().BeNull();


		resolver
			.Setup(r => r.FindByData(typeof(object), It.IsAny<INavigator>()))
			.Returns(new RouteInfo("home"));

		// When there's a matching route, Navigate method on Navigator should be called
		response = mockNavigator.Object.RedirectForNavigationData(resolver.Object, request);
		response.Should().NotBeNull();
		var result = await response!;
		result!.Success.Should().BeTrue();
	}
}
