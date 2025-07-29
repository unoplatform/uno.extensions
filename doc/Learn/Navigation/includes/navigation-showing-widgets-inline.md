---
uid: Uno.Extensions.Navigation.Data.ShowingWidgets-inline
---
<!--markdownlint-disable MD041-->

- Add a `TextBlock` to `SecondPage.xaml` that shows the name of the `Widget` supplied during navigation.

    ```xml
    <TextBlock HorizontalAlignment="Center"
               VerticalAlignment="Center">
        <Run Text="Widget Name:" />
        <Run Text="{Binding Name}" />
    </TextBlock>
    ```
