using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Equality;

// As **Attribute.cs files are linked in the Core.Generators project (to ease code gen)
// and the Core.Tests project is referencing both Core and Core.Generators projects,
// attributes are present twice.

// To resolve this, we don't set any access modifier in **Attributes.cs files (so they are internal by default),
// and then make them public only in the Core project.

public partial class ImplicitKeyEqualityAttribute
{
}

public partial class KeyAttribute
{
}
