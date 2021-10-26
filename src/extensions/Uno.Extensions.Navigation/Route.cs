using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record Route(string Scheme, string Base, string Path, IDictionary<string, object> Data)
{
    public override string ToString()
    {
        try
        {
            return $"{Scheme}{Base}{Path}{this.Query()}";
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
