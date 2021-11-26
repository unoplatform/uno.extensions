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
		=> string.Join("\r\n" + indent, items.Where(item => !string.IsNullOrWhiteSpace(item)).Select(input => Align(input!, indent)));

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
				sb.Append(line.Substring(trimLength));
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
}
