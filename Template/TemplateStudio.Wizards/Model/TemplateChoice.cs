using System;

namespace TemplateStudio.Wizards.Model;

public struct TemplateChoice
{
	public TemplateChoice(string displayName, string description, string choice)
	{
		DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
		Description = description ?? throw new ArgumentNullException(nameof(description));
		Choice = choice ?? throw new ArgumentNullException(nameof(choice));
	}

	public string DisplayName { get; }

	public string Description { get; }

	public string Choice { get; }
}
