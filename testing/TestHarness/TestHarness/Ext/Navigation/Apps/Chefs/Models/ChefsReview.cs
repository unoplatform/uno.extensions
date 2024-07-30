using System.Collections.Immutable;

namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public record ChefsReviewParameter(Guid recipeId, IImmutableList<ChefsReview> reviews);
public partial record ChefsReview
{
	//public ChefsReview(ReviewData reviewData)
	//{
	//	Id = reviewData.Id;
	//	RecipeId = reviewData.RecipeId;
	//	CreatedBy = reviewData.CreatedBy;
	//	PublisherName = reviewData.PublisherName;
	//	Date = reviewData.Date;
	//	Likes = reviewData.Likes?.ToImmutableList()
	//		?? ImmutableList<Guid>.Empty;
	//	Dislikes = reviewData.Dislikes?.ToImmutableList()
	//		?? ImmutableList<Guid>.Empty;
	//	Description = reviewData.Description;
	//	UrlAuthorImage = reviewData.UrlAuthorImage;
	//	UserLike = reviewData.UserLike;
	//}

	public ChefsReview(Guid recipeId, string text)
	{
		Id = Guid.NewGuid();
		RecipeId = recipeId;
		Description = text;
	}

	public Guid Id { get; init; }
	public Guid RecipeId { get; init; }
	public string? UrlAuthorImage { get; init; }
	public Guid CreatedBy { get; init; }
	public string? PublisherName { get; init; }
	public DateTime Date { get; init; }
	public string? Description { get; init; }
	public ImmutableList<Guid>? Likes { get; init; }
	public ImmutableList<Guid>? Dislikes { get; init; }
	public bool? UserLike { get; init; }

	//internal ReviewData ToData() => new()
	//{
	//	Id = Id,
	//	RecipeId = RecipeId,
	//	CreatedBy = CreatedBy,
	//	PublisherName = PublisherName,
	//	Date = Date,
	//	Likes = Likes?.ToList(),
	//	Dislikes = Dislikes?.ToList(),
	//	Description = Description,
	//	UrlAuthorImage = UrlAuthorImage
	//};
}

