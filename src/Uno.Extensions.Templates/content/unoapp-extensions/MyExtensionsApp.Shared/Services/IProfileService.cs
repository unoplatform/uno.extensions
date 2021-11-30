using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyExtensionsApp.Models;

namespace MyExtensionsApp.Services;

public interface IProfileService
{
	ValueTask<Profile> GetProfile(CancellationToken ct);
}
