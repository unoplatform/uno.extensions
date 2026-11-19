// The IValidator has been moved to Uno.Extensions.Core (same namespace), so it can be used without depending on this package.
// This forward keeps binaries compiled against a previous version of this package working.
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(Uno.Extensions.Validation.IValidator))]
