using System.Reflection;
using System.Runtime.Loader;

namespace FIGSignalExSvc.Plugins;

internal sealed class StudyPluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;
    private readonly IReadOnlySet<string> sharedAssemblyNames;

    public StudyPluginLoadContext(string pluginAssemblyPath, IReadOnlySet<string> sharedAssemblyNames)
        : base($"StudyPlugin:{Path.GetFileNameWithoutExtension(pluginAssemblyPath)}", isCollectible: false)
    {
        resolver = new AssemblyDependencyResolver(pluginAssemblyPath);
        this.sharedAssemblyNames = sharedAssemblyNames;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is not null && sharedAssemblyNames.Contains(assemblyName.Name))
        {
            return AssemblyLoadContext.Default.Assemblies.FirstOrDefault(
                       assembly => AssemblyName.ReferenceMatchesDefinition(
                           assembly.GetName(), assemblyName))
                   ?? AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
        }

        var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath is null ? nint.Zero : LoadUnmanagedDllFromPath(libraryPath);
    }
}
