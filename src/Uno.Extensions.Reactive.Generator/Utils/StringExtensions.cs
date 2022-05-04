using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.Extensions.Reactive.Generator;

internal static class StringExtensions
{
	public static string JoinBy(this IEnumerable<string?> items, string separator)
		=> string.Join(separator, items.Where(item => !string.IsNullOrWhiteSpace(item)));

	public static string Align(this IEnumerable<string?> items, int indent)
		=> Align(items, new string('\t', indent));

	public static string Align(this IEnumerable<string?> items, string indent)
		=> string.Join("\r\n" + indent, items.Where(item => !string.IsNullOrWhiteSpace(item)).Select((input, i) => Align(input!, indent)));

	public static string Align(this string text, int indent)
		=> Align(text, new string('\t', indent));

	// Note: This WILL NOT align the first (non white) line as it's expected to be used in string interpolation where we already have indentation
	private static string Align(string text, string indent)
	{
		var lines = text
			.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
			.Select(line => (value: line, trimmableLength: GetWhiteStartLength(line)))
			.ToList();

		switch (lines.Count)
		{
			case 0:
				return text;
			case 1:
				return text.TrimStart();
		}



		var trimLength = lines
			.Where(line => line.trimmableLength is not null)
			.SkipWhile(line => line.trimmableLength is 0)
			.Aggregate(int.MaxValue, (length, line) => Math.Min(length, line.trimmableLength!.Value));

		if (trimLength is int.MaxValue)
		{
			trimLength = 0;
		}
		else if (trimLength > 0
			&& lines.First().trimmableLength is 0
			&& lines.SkipWhile(line => line.trimmableLength is null).Skip(1).All(line => line.trimmableLength == trimLength))
		{
			// If we have only the first line which is at 0 and all the content is already indented, we make sure to keep one indent.
			// This is to support declaration like:
			// => bla
			//		.Bla();
			trimLength--;
		}

		var isFirstNonWhiteLine = true;
		var sb = new StringBuilder();
		for (var i = 0; i < lines.Count; i++)
		{
			var (line, trimmableLength) = lines[i];
			if (i > 0)
			{
				sb.AppendLine();
			}

			if (isFirstNonWhiteLine && trimmableLength is not null and 0)
			{
				isFirstNonWhiteLine = false;

				sb.Append(line);
			}
			else if (trimmableLength is not null)
			{
				isFirstNonWhiteLine = false;

				sb.Append(indent);
				sb.Append(line, trimLength, line.Length - trimLength);
			}
		}

		return sb.ToString();

		int? GetWhiteStartLength(string line)
		{
			for (var i = 0; i < line.Length; i++)
			{
				if (!char.IsWhiteSpace(line[i]))
				{
					return i;
				}
			}

			return null;
		}
	}

	public static string Replace(this string text, char[] chars, char replacement)
	{
		var result = new StringBuilder(text.Length);

		foreach (var @char in text)
		{
			result.Append(chars.Contains(@char) ? replacement : @char);
		}

		return result.ToString();
	}
}
