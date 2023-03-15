using System;
using System.Collections.Generic;
using System.Text;

namespace TestHarness.Ext.Navigation.NavigationView;

public class NavigationViewDataService : INavigationViewDataService
{
	private readonly Recipe[] recipes = new Recipe[]{
		new Recipe(Guid.NewGuid(), "Apple Crumble"),
		new Recipe(Guid.NewGuid(), "Greek Salad")
	};

	private readonly CookBook[] cookbooks = new CookBook[]
	{
		new CookBook(Guid.NewGuid(), "Favorites"),
		new CookBook(Guid.NewGuid(), "Gourmet Chef")
	};

	public Recipe[] Recipes => recipes;
	public CookBook[] CookBooks => cookbooks;
}

public interface INavigationViewDataService
{
	Recipe[] Recipes { get; }
	CookBook[] CookBooks { get; }
}

public interface IChefEntity { }

public partial record Recipe(Guid Id, string Name) : IChefEntity;

public partial record CookBook(Guid Id, string Name) : IChefEntity;

