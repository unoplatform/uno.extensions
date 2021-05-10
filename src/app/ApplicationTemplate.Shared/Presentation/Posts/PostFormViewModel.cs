using System;
using ApplicationTemplate.Client;
using Chinook.DynamicMvvm;
using FluentValidation;

namespace ApplicationTemplate.Presentation
{
	public class PostFormViewModel : ViewModel
	{
		private readonly PostData _post;

		public PostFormViewModel(PostData post)
		{
			_post = post;

			this.AddValidation(this.GetProperty(x => x.Title));
			this.AddValidation(this.GetProperty(x => x.Body));
		}

		public string Title
		{
			get => this.Get(_post.Title);
			set => this.Set(value);
		}

		public string Body
		{
			get => this.Get(_post.Body);
			set => this.Set(value);
		}

		public PostData GetPost()
		{
			return _post
				.WithTitle(Title)
				.WithBody(Body);
		}
	}

	public class PostFormValidator : AbstractValidator<PostFormViewModel>
	{
		public PostFormValidator()
		{
			RuleFor(x => x.Title).NotEmpty();
			RuleFor(x => x.Body).NotEmpty();
		}
	}
}
