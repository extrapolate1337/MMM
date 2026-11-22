using System;
using System.Collections.Generic;
using System.IO;

namespace MMM.Services;

public class ModService
{
    internal const string DisabledPrefix = "🚫";

    public List<string>? GetModNames(string modpackPath)
    {
        if (!Directory.Exists(modpackPath))
        {
            Console.WriteLine($"Directory '{modpackPath}' not found");
            return null;
        }

        var names = new List<string>();
        foreach (var file in Directory.GetFiles(modpackPath, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (!name.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) continue;

            names.Add(Path.GetFileNameWithoutExtension(name));
        }

        return names;
    }

    public void DeleteMods(string modpackPath, List<string> selectedMods)
    {
        if (!Directory.Exists(modpackPath)) return;

        foreach (var mod in selectedMods)
            foreach (var pack in Directory.GetFiles(modpackPath, "*", SearchOption.AllDirectories))
                if (Path.GetFileName(pack).StartsWith(mod, StringComparison.Ordinal))
                    File.Delete(pack);
    }

    public void ToggleMods(string modpackPath, List<string> selectedMods)
    {
        if (!Directory.Exists(modpackPath)) return;

        var files = Directory.GetFiles(modpackPath, "*", SearchOption.AllDirectories);
        foreach (var mod in selectedMods)
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (!name.StartsWith(mod, StringComparison.Ordinal)) continue;

                var directory = Path.GetDirectoryName(file) ?? modpackPath;
                if (name.StartsWith(DisabledPrefix, StringComparison.Ordinal))
                    File.Move(file, Path.Combine(directory, name[DisabledPrefix.Length..]));
                else
                    File.Move(file, Path.Combine(directory, DisabledPrefix + name));
            }
    }

    public void CopyOrMoveMods(string sourceModPackPath, string targetModPackPath, List<string> mods, bool copy)
    {
        var source = Path.GetFullPath(sourceModPackPath);
        var target = Path.GetFullPath(targetModPackPath);
        if (!Directory.Exists(source) || !Directory.Exists(target)) return;
        if (PathsEqual(source, target)) return;

        foreach (var filePath in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(filePath);
            foreach (var modName in mods)
            {
                if (!name.StartsWith(modName, StringComparison.Ordinal)) continue;

                var targetPath = Path.Combine(target, name);
                if (copy) File.Copy(filePath, targetPath, true);
                else File.Move(filePath, targetPath, true);
                break;
            }
        }
    }

    private static bool PathsEqual(string first, string second)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(
            Path.TrimEndingDirectorySeparator(first),
            Path.TrimEndingDirectorySeparator(second),
            comparison);
    }
}
