namespace FIG.IO;

public static class FixedOutputDirectory
{
    public static void Clear(string path, params string[] inputs)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A fixed output directory is required.");
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        bool Contains(string value) => value.Equals(root, StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        if (Path.GetPathRoot(root) == root || Contains(Path.GetFullPath(Environment.CurrentDirectory)) ||
            Contains(Path.GetFullPath(AppContext.BaseDirectory)) ||
            inputs.Where(p => !string.IsNullOrWhiteSpace(p)).Any(p => Contains(Path.GetFullPath(p))))
            throw new ArgumentException("Fixed output directory must not contain the working directory, application, or input files.");
        for (var parent = new DirectoryInfo(root); parent != null; parent = parent.Parent)
            if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Fixed output directory must not traverse a directory link.");
        if (File.Exists(root)) throw new IOException("Output directory is a file.");
        if (!Directory.Exists(root)) return;
        var lockPath = Path.Combine(root, "execution.lock");
        if (File.Exists(lockPath))
        {
            using var executionLock = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        // Reject links before deleting anything; never follow a link outside the chosen directory.
        void Inspect(string directory)
        {
            foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Fixed output directory contains a link: " + entry.FullName);
                if (entry is DirectoryInfo child) Inspect(child.FullName);
            }
        }
        Inspect(root);
        foreach (var entry in new DirectoryInfo(root).EnumerateFileSystemInfos())
            if (entry is DirectoryInfo directory) directory.Delete(true); else entry.Delete();
    }
}
