using System;
using System.Threading.Tasks;

namespace Uno.Extensions.Hosting;

public interface IHasAddressBar
{
	Task UpdateAddressBar(Uri applicationUri);
}
