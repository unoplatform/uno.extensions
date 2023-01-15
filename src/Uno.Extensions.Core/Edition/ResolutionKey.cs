using System;
using System.Linq;

namespace Uno.Extensions.Edition;

internal struct ResolutionKey
{
	public string Value { get; }

	public ResolutionKey(string memberName, string filePath, int fileLine)
		: this(memberName + filePath + fileLine)
	{
	}

	public ResolutionKey(string value)
	{
		Value = value;
	}
}
