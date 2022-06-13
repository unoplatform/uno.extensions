using System;
using System.Linq;

namespace Uno.Extensions.Reactive.SampleApp;

public partial class RefreshSampleViewModel
{
	public IFeed<string> Content => Feed.Async(async ct => DateTimeOffset.Now.ToString("F"));

	//public record MyValue
	//{
	//	public string CreationDate { get; } = DateTimeOffset.Now.ToString("F");

	//	/// <inheritdoc />
	//	public override string ToString()
	//		=> CreationDate;
	//}
}
