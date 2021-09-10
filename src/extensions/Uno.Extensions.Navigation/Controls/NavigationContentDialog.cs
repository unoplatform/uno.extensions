﻿using System;
using System.Threading.Tasks;
using System.Threading;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls;

public class NavigationContentDialog : IDialogManager
{
    public object CloseDialog(Dialog dialog, NavigationContext context, object responseData)
    {

        if (!(responseData is ContentDialogResult))
        {
            dialog.ShowTask.Cancel();
        }

        var resultType = dialog.Context.Request.Result;

        if (resultType is not null && responseData is not null)
        {
            if (resultType == typeof(ContentDialogResult))
            {
                if (responseData is not ContentDialogResult)
                {
                    responseData = ContentDialogResult.None;
                }
            }
            else if (resultType == typeof(ContentResult))
            {
                if (responseData is ContentDialogResult result)
                {
                    responseData = new ContentResult(result);
                }
                else
                {
                    responseData = new ContentResult(ContentDialogResult.None, responseData);
                }
            }
        }

        return responseData;
    }

    public Dialog DisplayDialog(INavigationService navigation, NavigationContext context, object vm)
    {
        if (!(context.Mapping?.View?.IsSubclassOf(typeof(ContentDialog)) ?? false))
        {
            return null;
        }

        var dialog = Activator.CreateInstance(context.Mapping.View) as ContentDialog;
        if (vm is not null)
        {
            dialog.DataContext = vm;
        }
        if (dialog is INavigationAware navAware)
        {
            navAware.Navigation = navigation;
        }

        var showTask = dialog.ShowAsync();
        showTask.AsTask().ContinueWith(result =>
        {
            if (result.Status != TaskStatus.Canceled &&
            context.ResultCompletion.Task.Status != TaskStatus.Canceled &&
            context.ResultCompletion.Task.Status != TaskStatus.RanToCompletion)
            {
                navigation.Navigate(new NavigationRequest(dialog, new NavigationRoute(new Uri(NavigationConstants.PreviousViewUri, UriKind.Relative), result.Result)));
            }
        }, CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach,
                        TaskScheduler.FromCurrentSynchronizationContext());
        return new Dialog(this, showTask, context);
    }
}
