using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Models;

namespace Commerce.Services;

public interface IProfileService
{
	ValueTask<Profile> GetProfile(CancellationToken ct);
}
