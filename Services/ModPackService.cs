using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MMM.Services;

public class ModPackService
{
    public string AbsDirectory { get; private set; } = string.Empty;

    public string ModPacksDirectory => Path.Combine(AbsDirectory, "modpacks");

    public string LastDirectory { get; private set; } = string.Empty;

    public string[] Initialize()
    {
        var startDirectory = Path.GetFullPath(".");
        LastDirectory = new DirectoryInfo(startDirectory).Name;
        AbsDirectory = IsDeploymentDirectory(startDirectory)
            ? startDirectory
            : Path.Combine(startDirectory, "MMM");

        Directory.CreateDirectory(ModPacksDirectory);
        Directory.SetCurrentDirectory(AbsDirectory);

        return Directory.GetDirectories(ModPacksDirectory);
    }

    public bool IsMainPath()
    {
        return PathsEqual(Directory.GetCurrentDirectory(), AbsDirectory);
    }

    public bool EnsureBaseDirectory()
    {
        try
        {
            Directory.SetCurrentDirectory(AbsDirectory);
            return true;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return false;
        }
    }

    public bool EnsureModPacksDirectory()
    {
        try
        {
            Directory.CreateDirectory(ModPacksDirectory);
            return true;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return false;
        }
    }

    public string GetModPackPath(string name)
    {
        return Path.Combine(ModPacksDirectory, name);
    }

    public string[] GetModPacks()
    {
        if (!Directory.Exists(ModPacksDirectory)) return [];

        return Directory.GetDirectories(ModPacksDirectory);
    }

    public string[] GetOtherModPacks(string? current)
    {
        return GetModPacks()
            .Where(path => !string.Equals(Path.GetFileName(path), current, StringComparison.Ordinal))
            .ToArray();
    }

    public void CreateModPack()
    {
        var names = GetModPacks()
            .Select(path => Path.GetFileName(path) ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var name = "New ModPack";
        if (names.Contains(name))
        {
            var index = 1;
            while (names.Contains(name = "New ModPack" + index))
                index++;
        }

        Directory.CreateDirectory(Path.Combine(ModPacksDirectory, name));
    }

    public bool IsModPackEmpty(string path)
    {
        if (!Directory.Exists(path)) return false;

        return !Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any();
    }

    public void DeleteModPack(string nameModPack)
    {
        var path = GetModPackPath(nameModPack);
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    public string? GetModPackDirectoryAt(int index)
    {
        var directories = GetModPacks();
        if (index < 0 || index >= directories.Length) return null;

        return directories[index];
    }

    public bool Rename(string oldPath, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return false;
        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;

        var directory = Path.GetDirectoryName(oldPath);
        if (directory == null) return false;

        var newPath = Path.Combine(directory, newName);
        if (PathsEqual(oldPath, newPath) || Directory.Exists(newPath)) return false;

        Directory.Move(oldPath, newPath);
        return true;
    }

    private static bool IsDeploymentDirectory(string path)
    {
        if (!string.Equals(new DirectoryInfo(path).Name, "MMM", StringComparison.OrdinalIgnoreCase))
            return false;

        return File.Exists(Path.Combine(path, "MMM.exe"))
               || File.Exists(Path.Combine(path, "MMM"))
               || File.Exists(Path.Combine(path, "MMM.dll"));
    }

    private static bool PathsEqual(string first, string second)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(first)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(second)),
            comparison);
    }
}
