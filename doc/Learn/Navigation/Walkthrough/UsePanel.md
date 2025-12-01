---
uid: Uno.Extensions.Navigation.Walkthrough.Advanced.UsePanel
title: Switch Between Views using Panel with Visibility-Based Region Navigation
tags: [uno, uno-platform, uno-extensions, navigation, Panel, Grid, StackPanel, regions, Region.Attached, Region.Name, Region.Navigator, Visibility, nested-navigation, view-switching, visibility-based-navigation, performance, lightweight-navigation, no-frame-overhead, Button-navigation, Navigation.Request, RouteMap, ViewMap, IsDefault, collapsed-visibility, visibility-toggle]
---

# Switch Between Views using Panel with Visibility-Based Region Navigation

> **UnoFeature:** Navigation

* Import Navigation namespace in XAML:

    ```xml
    <Page xmlns:uen="using:Uno.Extensions.Navigation.UI">
    ```

## Define Layout

Create structure with control buttons and content areas.

* Add row definitions:

    ```xml
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
        <RowDefinition />
    </Grid.RowDefinitions>
    ```

* Add switching controls:

    ```xml
    <StackPanel Grid.Row="1"
                HorizontalAlignment="Center"
                Orientation="Horizontal">
        <Button Content="One" />
        <Button Content="Two" />
        <Button Content="Three" />
    </StackPanel>
    ```

* Add content regions:

    ```xml
    <Grid Grid.Row="2">
        <Grid>
            <TextBlock Text="One"
                       FontSize="24"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center"/>
        </Grid>
        <Grid>
            <TextBlock Text="Two"
                       FontSize="24"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center" />
        </Grid>
        <Grid>
            <TextBlock Text="Three"
                       FontSize="24"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center" />
        </Grid>
    </Grid>
    ```

## Set Up Navigation

Enable region navigation with visibility toggling.

* Add `Region.Attached="True"` to content container:

    ```xml
    <Grid Grid.Row="2"
          uen:Region.Attached="True">
    ```

* Add `Region.Name` to each content area:

    ```xml
    <Grid uen:Region.Attached="True" Grid.Row="2">
        <Grid uen:Region.Name="One">
            <TextBlock Text="One" />
        </Grid>
        <Grid uen:Region.Name="Two">
            <TextBlock Text="Two" />
        </Grid>
        <Grid uen:Region.Name="Three">
            <TextBlock Text="Three" />
        </Grid>
    </Grid>
    ```

* Visual tree structure:
  * **Parent Grid** — Region container
  * **Child Grids** — Individual regions
  * **TextBlocks** — Region content

* Add `Navigation.Request` to buttons with `./` prefix:

    ```xml
    <StackPanel Grid.Row="1"
                HorizontalAlignment="Center"
                Orientation="Horizontal">
        <Button Content="One"
                uen:Navigation.Request="./One" />
        <Button Content="Two"
                uen:Navigation.Request="./Two" />
        <Button Content="Three"
                uen:Navigation.Request="./Three" />
    </StackPanel>
    ```

* **Important**: Use `./` prefix to indicate nested region navigation.

* Set `Region.Navigator="Visibility"`:

    ```xml
    <Grid Grid.Row="2"
          uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
    ```

* Set initial visibility to `Collapsed`:

    ```xml
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility"
          Grid.Row="2">
        <Grid uen:Region.Name="One" Visibility="Collapsed">
            <TextBlock Text="One" />
        </Grid>
        <Grid uen:Region.Name="Two" Visibility="Collapsed">
            <TextBlock Text="Two" />
        </Grid>
        <Grid uen:Region.Name="Three" Visibility="Collapsed">
            <TextBlock Text="Three" />
        </Grid>
    </Grid>
    ```

## Complete Example

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI"
      Background="{ThemeResource MaterialBackgroundBrush}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>

        <StackPanel Grid.Row="1"
                    HorizontalAlignment="Center"
                    Orientation="Horizontal">
            <Button Content="One"
                    uen:Navigation.Request="./One" />
            <Button Content="Two"
                    uen:Navigation.Request="./Two" />
            <Button Content="Three"
                    uen:Navigation.Request="./Three" />
        </StackPanel>

        <Grid uen:Region.Attached="True"
              uen:Region.Navigator="Visibility"
              Grid.Row="2">
            <Grid uen:Region.Name="One" Visibility="Collapsed">
                <TextBlock Text="One" FontSize="24" />
            </Grid>
            <Grid uen:Region.Name="Two" Visibility="Collapsed">
                <TextBlock Text="Two" FontSize="24" />
            </Grid>
            <Grid uen:Region.Name="Three" Visibility="Collapsed">
                <TextBlock Text="Three" FontSize="24" />
            </Grid>
        </Grid>
    </Grid>
</Page>
```
