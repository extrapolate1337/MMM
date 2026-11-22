using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace aimware.Services;

public class ModPackService
{
    public string AbsDirectory { get; private set; } = string.Empty;
    public string NoMainDirectory { get; private set; } = string.Empty;
    public string[] CleanAbsPath { get; private set; } = [];
    public string[] ModPacks { get; private set; } = [];
    public string[] OriginalNameDirs { get; private set; } = [];
    public string LastDirectory { get; private set; } = string.Empty;

    public string[] Initialize()
    {
        var absPath = Path.GetFullPath("./");
        NoMainDirectory = absPath;
        Console.WriteLine("Full path: " + absPath);
        CleanAbsPath = absPath.Split("/");
        LastDirectory = CleanAbsPath[CleanAbsPath.Length - 2];
        //Console.WriteLine("LastDirectory: " + LastDirectory);
        if (CleanAbsPath[CleanAbsPath.Length - 2] != "MMM")
        {
            Directory.CreateDirectory("MMM");
            Directory.SetCurrentDirectory("./MMM");
        }

        absPath = Path.GetFullPath("./");
        CleanAbsPath = absPath.Split("/");
        AbsDirectory = absPath;
        var directories = Directory.GetDirectories("./");
        if (!directories.Contains("modpacks")) Directory.CreateDirectory("modpacks");
        Directory.SetCurrentDirectory("modpacks");
        var modpacks = Directory.GetDirectories("./");
        ModPacks = modpacks;
        if (modpacks.Length > 0)
        {
            OriginalNameDirs = new string[modpacks.Length];
            for (var i = 0; i < modpacks.Length; i++)
            {
                OriginalNameDirs[i] = modpacks[i].Replace("./", "");
                //Console.WriteLine("OriginalNameDirs: " + OriginalNameDirs[i]);
            }
        }

        Directory.SetCurrentDirectory("..");
        return modpacks;
    }

    public bool IsMainPath()
    {
        var main_dirs = CleanAbsPath;
        var target_dirs = Path.GetFullPath("./").Split("/");
        var main_index = -1;
        var target_index = -1;
        if (!target_dirs.Contains("MMM")) return false;
        for (var i = 0; i < main_dirs.Length; i++)
            if (main_dirs[i] == "MMM")
            {
                main_index = i;
                break;
            }

        for (var i = 0; i < target_dirs.Length; i++)
            if (target_dirs[i] == "MMM")
            {
                target_index = i;
                break;
            }

        if (main_index == target_index) return true;
        return false;
    }

    public bool EnsureBaseDirectory()
    {
        if (!IsMainPath())
            try
            {
                Directory.SetCurrentDirectory(AbsDirectory);
                //Console.WriteLine(Path.GetFullPath("./"));
            }
            catch (Exception exception)
            {
                try
                {
                    Directory.SetCurrentDirectory(NoMainDirectory);
                }
                catch (Exception e1)
                {
                    Console.WriteLine(e1);
                    return false;
                }

                Console.WriteLine(exception);
            }

        return true;
    }

    public bool EnsureModPacksDirectory()
    {
        if (!EnsureBaseDirectory()) return false;

        var directory = Directory.GetCurrentDirectory();
        if (!directory.Contains("modpacks"))
            try
            {
                Directory.SetCurrentDirectory("./modpacks");
            }
            catch
            {
                Directory.CreateDirectory("modpacks");
                Directory.SetCurrentDirectory("./modpacks");
            }

        return true;
    }

    public string[] GetModPacks()
    {
        var modpacks = Directory.GetDirectories("./");
        ModPacks = modpacks;
        return modpacks;
    }

    public string[] GetOtherModPacks(string? current)
    {
        return GetModPacks()
            .Where(p => p.Replace("./", "") != current)
            .ToArray();
    }

    public void CreateModPack()
    {
        var Paks = Directory.GetDirectories("./");
        var Modpacks = Paks.Select(p => p.Replace("./", "")).ToList();
        if (Modpacks.Contains("New ModPack"))
        {
            var IsNew = true;
            var i = 1;
            var a = "New ModPack";
            while (IsNew)
            {
                a = "New ModPack" + i;
                if (!Modpacks.Contains(a))
                {
                    Directory.CreateDirectory(a);
                    //Console.WriteLine("Created " + a);
                    IsNew = false;
                }

                i++;
            }
        }
        else
        {
            Directory.CreateDirectory("New ModPack");
            //Console.WriteLine("Created New ModPack");
        }
    }

    public bool IsModPackEmpty(string path)
    {
        if (!Directory.Exists(path)) return false;
        return !Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any();
    }

    public void DeleteModPack(string nameModPack)
    {
        var packs = Directory.GetDirectories("./");
        for (var i = 0; i < packs.Length; i++)
            if (packs[i].Replace("./", "") == nameModPack)
            {
                Directory.Delete(packs[i], true);
                //Console.WriteLine(packs[i].Replace("./", "") + " deleted");
                break;
            }
    }

    public string? GetModPackDirectoryAt(int index)
    {
        var dirs = Directory.GetDirectories("./");
        if (index < 0 || index >= dirs.Length) return null;
        return dirs[index];
    }

    public bool Rename(string oldName, string newName)
    {
        var directory = Path.GetDirectoryName(oldName);
        var newPath = directory == null ? newName : Path.Combine(directory, newName);
        //Console.WriteLine(newPath);
        if (oldName == newName || Directory.Exists(newPath)) return false;
        Directory.Move(oldName, newPath);
        return true;
    }
}
