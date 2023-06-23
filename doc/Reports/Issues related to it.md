
1. MauiBinding, MauiColor don't work properly. Looks like the MauiContext and Maui.Application is being created in another thread and that's causing issues;

2. iOS doesn't work well with the Skia, looks like we have to add the native reference in order to make it work;

3. Some controls aren't working properly, from Telerik we can report:
	> RadBusyIndicator (Didn't find the Handler for SKCanvasView) RadAutoComplete (Something about the constants file/class [didn't remember well]) RadSignaturePad (it runs, but is a blank square in the screen)

4. Issues with WindowsAppSdk on Uno projects, it shows [UNOB002](https://platform.uno/docs/articles/uno-build-error-codes.html) error, even doing a downgrade (will discuss this with Nick)
5. running on macOS isn't fun due to .net workloads nightmare (right now I'm trying to restore the workloads)


# Report June 20th

- Can't use `Maui.DataTemplate`, with that isn't possible to use `ListView` or `CollectionView`

```xml
<maui:CollectionView HeightRequest="200"
					 x:Name="cv">
	<maui:CollectionView.ItemTemplate>
		<maui:DataTemplate>
			<maui:Label Text="Hello there"/>
		</maui:DataTemplate>
	</maui:CollectionView.ItemTemplate>
</maui:CollectionView>
```
The code above will not work, due the following build errors:
> XamlCompiler error WMC0075: Missing Content Property definition for Element 'DataTemplate' to receive content 'Label'
> XamlCompiler error WMC0011: Unknown member '_UnknownContent' on element 'DataTemplate'


- When defining values in XAML everything is a `string` , but isn't converted for all properties, like for primitive types it works for the rest don't. If you use C# everything works fine

```xml
<maui:Button Text="Click me" HeightRequest="30"/>
```
The above code will not work on winUI, this happens on some properties, it works well on mobile targets. Probably because the on winUI everything is a `DependencyObject`.


- Some layout issues, if you create a `VerticalStackLayout` and add `BoxView` without `HeightRequest` it will not renderer (the Height will be zero);
	- If you create more controls and don't specify a `HeightRequest` it can be zero, even if you update the value (Label that has a Text as a binding value)
- The binding changed aren't triggered on mobile targets, and that can cause the control to not show on the screen, for example, if you have a `Label.Text` binded to a property on your ViewModel. This doesn't work even in plain C#.
- Can't use the `Grid` control, because it will not work very when trying to configure the Row/ColumnDefinitions, the `*` and `auto` value will not be correctly translated
```xml
<maui:Grid RowDefinitions="auto,*,300">
	<maui:Label Text="Row0"/>
	<maui:Label Text="Row1" maui:Grid.Row="1"/>
	<maui:Label Text="Row2" maui:Grid.Row="2"/>
</maui:Grid>
```

>  An error was found in MauiContent ---> System.Exception: Unable to convert auto,* for RowDefinitions with type Microsoft.Maui.Controls.RowDefinitionCollection

Now if you just use the plain `Grid` layout it will work:
```xml
<maui:Grid>
	<maui:Label Text="Row0"/>
</maui:Grid>
```

The same is true for other layouts that need _extra_ configuration, like `AbsoluteLayout`.

