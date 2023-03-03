using System.Threading;
using System.Threading.Tasks;
using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadWriteClassViewModel
{
	public IState<CompanyClass> Company => State.Value(this, () => new CompanyClass());

	public IFeed<CompanyClass> CurrentCompany => State.FromFeed(this, Company);

	public IState<string> Message => State.Value(this, () => string.Empty);

	public async ValueTask DisplayCurrentCompanyName(CompanyClass company, CancellationToken ct)
	{
		var current = await CurrentCompany;

		await Message.Update(current => company.CompanyName, ct);
	}
}
