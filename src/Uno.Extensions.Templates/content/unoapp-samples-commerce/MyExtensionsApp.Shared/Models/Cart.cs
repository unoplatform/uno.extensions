//-:cnd:noEmit
using System;
using System.Linq;
using MyExtensionsApp.Services;

namespace MyExtensionsApp.Models;

public record Cart(CartItem[] Items)
{
	public string SubTotal => "$350,97";
	public string Tax => "$15,75";
	public string Total => "$405,29";
}
