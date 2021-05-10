using System;
using System.Threading;
using System.Threading.Tasks;
using Chinook.DynamicMvvm;
using MessageDialogService;
using Windows.UI.Core;

namespace ApplicationTemplate.Presentation
{
	public class ExceptionsDiagnosticsViewModel : ViewModel
	{
		public IDynamicCommand TestErrorInCommand => this.GetCommand(() =>
		{
			throw new Exception("This is a test of an exception in a command. Please ignore.");
		});

		public IDynamicCommand TestErrorInTaskScheduler => this.GetCommand(() =>
		{
			// This will be handled by <see cref="TaskScheduler.UnobservedTaskException" />
			var _ = Task.Run(() => throw new Exception("This is a test of an exception in the TaskScheduler. Please ignore."));

			// Wait until the task had a chance to throw without awaiting it.
			// Unobserved tasks exceptions occur only when the tasks are collected by the GC.
			Thread.Sleep(100);
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
			GC.WaitForPendingFinalizers();
		});

		public IDynamicCommand TestErrorInCoreDispatcher => this.GetCommand(() =>
		{
			var _ = this.GetService<CoreDispatcher>().RunAsync(CoreDispatcherPriority.High, () => throw new Exception("This is a test of an exception in the CoreDispatcher. Please ignore."));
		});

		public IDynamicCommand TestErrorInThreadPool => this.GetCommandFromTask(async ct =>
		{
			var confirmation = await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
				.Title("Diagnostics")
				.Content("This should crash your application. Make sure your analytics provider receives a crash log.")
				.CancelCommand()
				.Command(MessageDialogResult.Accept, label: "Crash")
			);

			if (confirmation != MessageDialogResult.Accept)
			{
				return;
			}

			// This will be handled by <see cref="AppDomain.CurrentDomain.UnhandledException" />
			var _ = ThreadPool.QueueUserWorkItem(__ => throw new Exception("This is a test of an exception in the ThreadPool. Please ignore."));
		});

		public IDynamicCommand TestErrorInMainThread => this.GetCommandFromTask(async ct =>
		{
			//-:cnd:noEmit
			// This will not crash on Android as it can be safely handled.
#if !__ANDROID__
			//+:cnd:noEmit
			var confirmation = await this.GetService<IMessageDialogService>().ShowMessage(ct, mb => mb
				.Title("Diagnostics")
				.Content("This should crash your application. Make sure your analytics provider receives a crash log.")
				.CancelCommand()
				.Command(MessageDialogResult.Accept, label: "Crash")
			);

			if (confirmation != MessageDialogResult.Accept)
			{
				return;
			}
			//-:cnd:noEmit
#endif
			//+:cnd:noEmit

			//-:cnd:noEmit
#if __IOS__
			//+:cnd:noEmit
			/// This will be handled by <see cref="AppDomain.CurrentDomain.UnhandledException" />
			UIKit.UIApplication.SharedApplication.InvokeOnMainThread(() => throw new Exception("This is a test of an exception in the MainThread. Please ignore."));
			//-:cnd:noEmit
#elif __ANDROID__
			//+:cnd:noEmit
			/// This will be handled by <see cref="Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser" />
			var _ = new Android.OS.Handler(Android.OS.Looper.MainLooper).Post(() => throw new InvalidOperationException("This is a test of an exception in the MainLooper. Please ignore."));
			await Task.CompletedTask;
			//-:cnd:noEmit
#endif
			//+:cnd:noEmit
		});
	}
}
