using System;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Business;
using ApplicationTemplate.Client;
using Chinook.DynamicMvvm;
using Chinook.SectionsNavigation;
using Chinook.StackNavigation;
using MessageDialogService;
using Microsoft.Extensions.Localization;

namespace ApplicationTemplate.Presentation
{
	public class EditPostPageViewModel : ViewModel
	{
		public EditPostPageViewModel(PostData post = null)
		{
			IsNewPost = post == null;
			Title = post == null ? this.GetService<IStringLocalizer>()["EditPost_NewPost"] : post.Title;
			Form = this.AttachChild(new PostFormViewModel(post ?? PostData.Default));

			this.RegisterBackHandler(OnBackRequested);
		}

		public string Title { get; }

		public bool IsNewPost { get; }

		public PostFormViewModel Form { get; }

		public IDynamicCommand Save => this.GetCommandFromTask(async ct =>
		{
			var validationResult = await Form.Validate(ct);

			if (validationResult.IsValid)
			{
				var post = Form.GetPost();

				if (post.Exists)
				{
					await this.GetService<IPostService>().Update(ct, post.Id, post);
				}
				else
				{
					await this.GetService<IPostService>().Create(ct, post);
				}

				await this.GetService<IStackNavigator>().NavigateBack(ct);
			}
		});

		public IDynamicCommand Delete => this.GetCommandFromTask(async ct =>
		{
			var post = Form.GetPost();

			if (post.Exists)
			{
				await this.GetService<IPostService>().Delete(ct, post.Id);

				await this.GetService<IStackNavigator>().NavigateBack(ct);
			}
		});

		private async Task OnBackRequested(CancellationToken ct)
		{
			var result = await this.GetService<IMessageDialogService>()
				.ShowMessage(ct, mdb => mdb
					.Title("Warning")
					.Content("Are you sure you want to leave this page?")
					.OkCommand()
					.CancelCommand()
				);

			if (result == MessageDialogResult.Ok)
			{
				await this.GetService<ISectionsNavigator>().NavigateBackOrCloseModal(ct);
			}
		}
	}
}
