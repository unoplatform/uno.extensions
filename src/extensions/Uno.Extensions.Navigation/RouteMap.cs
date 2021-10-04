using System;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap(
    string Path,
    Type View = null,
    Type ViewModel = null,
    Type Data = null,
    Type ResultData = null )
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public string FullPath(string relativePath) => CombinePathWithRelativePath(Path, relativePath);
    public static string CombinePathWithRelativePath(string path, string relativePath) =>
        string.IsNullOrWhiteSpace(relativePath) ?
            path :
            (relativePath.EndsWith("/") ?
                $"{relativePath}{path}" :
                $"{relativePath}/{path}");
}
