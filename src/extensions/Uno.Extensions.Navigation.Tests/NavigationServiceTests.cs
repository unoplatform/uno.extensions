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
    protected override void InitializeServices(IServiceCollection services)
    {
       

    }

    [TestMethod]
    public void NavigationTest()
    {
       
    }
}

public class PageOne
{ }
