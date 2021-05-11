using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace ApplicationTemplate
{
	/// <summary>
	/// Invokes calls to an IBrowser on the dispatcher
	/// </summary>
	public class DispatcherBrowserDecorator : IBrowser
	{
		private readonly IBrowser _innerBrowser;
		// private readonly CoreDispatcher _dispatcher;

		public DispatcherBrowserDecorator(IBrowser browser)//, CoreDispatcher dispatcher)
		{
			_innerBrowser = browser;
			//_dispatcher = dispatcher;
		}

		public async Task OpenAsync(string uri)
		{
			//await DispatcherRunTaskAsync(CoreDispatcherPriority.Normal, () => _innerBrowser.OpenAsync(new Uri(uri)));
		}

		public async Task OpenAsync(string uri, BrowserLaunchMode launchMode)
		{
			//await DispatcherRunTaskAsync(CoreDispatcherPriority.Normal, () => _innerBrowser.OpenAsync(new Uri(uri), launchMode));
		}

		public async Task OpenAsync(string uri, BrowserLaunchOptions options)
		{
			//await DispatcherRunTaskAsync(CoreDispatcherPriority.Normal, () => _innerBrowser.OpenAsync(new Uri(uri), options));
		}

		public async Task OpenAsync(Uri uri)
		{
			//await DispatcherRunTaskAsync(CoreDispatcherPriority.Normal, () => _innerBrowser.OpenAsync(uri));
		}

		public async Task OpenAsync(Uri uri, BrowserLaunchMode launchMode)
		{
			//await DispatcherRunTaskAsync(CoreDispatcherPriority.Normal, () => _innerBrowser.OpenAsync(uri, launchMode));
		}

		public async Task<bool> OpenAsync(Uri uri, BrowserLaunchOptions options)
		{
            return true; 
			//return await DispatcherRunTaskAsync(CoreDispatcherPriority.Normal, async () => await _innerBrowser.OpenAsync(uri, options));
		}

		///// <summary>
		///// This method allows for executing an async Task with result on the CoreDispatcher.
		///// </summary>
		//private async Task<TResult> DispatcherRunTaskAsync<TResult>(CoreDispatcherPriority priority, Func<Task<TResult>> asyncFunc)
		//{
		//	var completion = new TaskCompletionSource<TResult>();
		//	await _dispatcher.RunAsync(priority, RunActionUI);
		//	return await completion.Task;

		//	async void RunActionUI()
		//	{
		//		try
		//		{
		//			var result = await asyncFunc();
		//			completion.SetResult(result);
		//		}
		//		catch (Exception exception)
		//		{
		//			completion.SetException(exception);
		//		}
		//	}
		//}

		///// <summary>
		///// This method allows for executing an async Task without result on the CoreDispatcher.
		///// </summary>
		//private async Task DispatcherRunTaskAsync(CoreDispatcherPriority priority, Func<Task> asyncFunc)
		//{
		//	var completion = new TaskCompletionSource<Unit>();
		//	await _dispatcher.RunAsync(priority, RunActionUI);
		//	await completion.Task;

		//	async void RunActionUI()
		//	{
		//		try
		//		{
		//			await asyncFunc();
		//			completion.SetResult(Unit.Default);
		//		}
		//		catch (Exception exception)
		//		{
		//			completion.SetException(exception);
		//		}
		//	}
		//}
	}
}
