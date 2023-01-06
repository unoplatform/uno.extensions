using System;
using System.Linq;

namespace Uno.System // Namespace that will conflicts with any system types that are fully qualified but not prefixed by 'global'
{
	public class DateTime { } // Type that conflicts the System.DateTime type when not prefixed with 'global'
}
