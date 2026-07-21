using System.Reflection;
using System.Text.Json;
using FIG.Studies;

namespace FIGSignalExSvc.Plugins;

public sealed class StudyPluginCatalog : IStudyPluginCatalog
{
    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, StudyPluginRegistration> registrations =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<StudyPluginLoadContext> loadContexts = [];
    private readonly ILogger<StudyPluginCatalog> logger;

    public StudyPluginCatalog(
        IConfiguration configuration,
        ILogger<StudyPluginCatalog> logger)
    {
        this.logger = logger;

        // Force the shared API and common-study assemblies into the default context.
        _ = typeof(IStudyCollectionFactory).Assembly;
        _ = typeof(StudyMA).Assembly;

        var configuredPath = configuration["StudyPlugins:Path"] ?? "Plugins";
        var pluginRoot = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);

        LoadPlugins(Path.GetFullPath(pluginRoot));
    }

    public IReadOnlyCollection<StudyPluginRegistration> Registrations
        => registrations.Values.ToArray();

    public StudyColBase Create(string colType, StudyCollectionContext context)
    {
        if (!registrations.TryGetValue(colType, out var registration))
        {
            var available = registrations.Count == 0
                ? "none"
                : string.Join(", ", registrations.Keys.OrderBy(key => key));

            throw new InvalidOperationException(
                $"No study collection plugin is registered for ColType '{colType}'. " +
                $"Available ColTypes: {available}.");
        }

        return registration.Factory.Create(context);
    }

    private void LoadPlugins(string pluginRoot)
    {
        if (!Directory.Exists(pluginRoot))
        {
            logger.LogWarning("Study plugin directory does not exist: {PluginRoot}", pluginRoot);
            return;
        }

        var manifests = Directory
            .EnumerateFiles(pluginRoot, "plugin.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var manifestPath in manifests)
        {
            try
            {
                LoadPlugin(manifestPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not load study plugin manifest {ManifestPath}", manifestPath);
            }
        }

        logger.LogInformation(
            "Loaded {PluginCount} study collection plugin(s): {ColTypes}",
            registrations.Count,
            registrations.Count == 0
                ? "none"
                : string.Join(", ", registrations.Keys.OrderBy(key => key)));
    }

    private void LoadPlugin(string manifestPath)
    {
        var manifest = JsonSerializer.Deserialize<StudyPluginManifest>(
                           File.ReadAllText(manifestPath), ManifestOptions)
                       ?? throw new InvalidDataException("Plugin manifest is empty.");

        if (string.IsNullOrWhiteSpace(manifest.ColType) ||
            string.IsNullOrWhiteSpace(manifest.Assembly))
        {
            throw new InvalidDataException("Plugin manifest requires colType and assembly.");
        }

        if (manifest.ApiVersion != StudyPluginApi.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Plugin '{manifest.ColType}' API version {manifest.ApiVersion} is incompatible " +
                $"with host API version {StudyPluginApi.CurrentVersion}.");
        }

        var pluginDirectory = Path.GetFullPath(
            Path.GetDirectoryName(manifestPath)
            ?? throw new InvalidDataException("Plugin manifest has no parent directory."));
        var assemblyPath = Path.GetFullPath(Path.Combine(pluginDirectory, manifest.Assembly));
        var pluginPrefix = pluginDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!assemblyPath.StartsWith(pluginPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Plugin assembly must be inside its plugin directory.");
        }

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException("Plugin assembly was not found.", assemblyPath);
        }

        var sharedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            typeof(IStudyCollectionFactory).Assembly.GetName().Name!,
            typeof(StudyMA).Assembly.GetName().Name!,
            typeof(FIGCommon.Models.StudyColRS).Assembly.GetName().Name!,
            "Microsoft.Extensions.Logging.Abstractions"
        };

        var loadContext = new StudyPluginLoadContext(assemblyPath, sharedAssemblyNames);
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        var factoryTypes = GetLoadableTypes(assembly)
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                typeof(IStudyCollectionFactory).IsAssignableFrom(type))
            .ToArray();

        if (factoryTypes.Length != 1)
        {
            throw new InvalidDataException(
                $"Plugin '{manifest.ColType}' must contain exactly one {nameof(IStudyCollectionFactory)}; " +
                $"found {factoryTypes.Length}.");
        }

        var factory = (IStudyCollectionFactory?)Activator.CreateInstance(factoryTypes[0])
                      ?? throw new InvalidOperationException("Plugin factory could not be created.");

        if (!string.Equals(factory.ColType, manifest.ColType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Manifest ColType '{manifest.ColType}' does not match factory ColType '{factory.ColType}'.");
        }

        if (factory.ApiVersion != StudyPluginApi.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Factory '{factory.GetType().FullName}' has incompatible API version {factory.ApiVersion}.");
        }

        if (!registrations.TryAdd(
                factory.ColType,
                new StudyPluginRegistration(factory.ColType, factory.Version, assemblyPath, factory)))
        {
            throw new InvalidDataException(
                $"A plugin for ColType '{factory.ColType}' is already registered.");
        }

        loadContexts.Add(loadContext);
        logger.LogInformation(
            "Registered study plugin {ColType} v{Version} from {AssemblyPath}",
            factory.ColType,
            factory.Version,
            assemblyPath);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var loaderMessages = string.Join(
                Environment.NewLine,
                ex.LoaderExceptions.Where(item => item is not null).Select(item => item!.Message));
            throw new InvalidOperationException(
                $"One or more plugin types could not be loaded:{Environment.NewLine}{loaderMessages}", ex);
        }
    }
}
