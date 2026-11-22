using System;
using System.Collections.Generic;
using System.IO;

namespace aimware.Services;

public class ModService
{
    public List<string>? GetModNames(string modpackPath)
    {
        var names = new List<string>();

        if (Directory.Exists(modpackPath))
        {
            var files = Directory.GetFiles(modpackPath, "*", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                for (var i = 0; i < files.Length; i++)
                {
                    var name = Path.GetFileName(files[i]);
                    if (!name.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) continue;
                    names.Add(Path.GetFileNameWithoutExtension(name));
                }
            }

            return names;
        }

        Console.WriteLine($"Directory '{modpackPath}' not found");
        return null;
    }

    public void DeleteMods(string selectedModPack, List<string> selectedMods)
    {
        foreach (var mod in selectedMods)
        {
            Directory.SetCurrentDirectory(selectedModPack);
            var packs = Directory.GetFiles("./", "*", SearchOption.AllDirectories);
            foreach (var pack in packs)
                if (Path.GetFileName(pack).StartsWith(mod))
                {
                    //Console.WriteLine("CWD " + pack);
                    File.Delete(pack);
                }

            Directory.SetCurrentDirectory("..");
        }
    }

    public void ToggleMods(string selectedModPack, List<string> selectedMods)
    {
        Directory.SetCurrentDirectory(selectedModPack);
        var files = Directory.GetFiles("./", "*", SearchOption.AllDirectories);
        for (var j = 0; j < selectedMods.Count; j++)
        {
            var iname = selectedMods[j];
            for (var i = 0; i < files.Length; i++)
            {
                var name = Path.GetFileName(files[i]);
                if (name.StartsWith(iname))
                {
                    var dir = Path.GetDirectoryName(files[i]) ?? ".";
                    if (name.StartsWith("🚫"))
                    {
                        File.Move(files[i], Path.Combine(dir, name.Replace("🚫", "")));
                    }
                    else
                    {
                        File.Move(files[i], Path.Combine(dir, "🚫" + name));
                    }
                }
            }
        }

        Directory.SetCurrentDirectory("..");
    }

    public void CopyOrMoveMods(string selectedModPack, string targetModPack, List<string> mods, bool copy)
    {
        var dir = Path.GetFullPath(targetModPack);
        Directory.SetCurrentDirectory(selectedModPack);
        var dirs = Directory.GetFiles("./", "*", SearchOption.AllDirectories);
        if (dir == Directory.GetCurrentDirectory())
        {
            Directory.SetCurrentDirectory("..");
            return;
        }

        foreach (var filePath in dirs)
        {
            var name = Path.GetFileName(filePath);
            foreach (var singleModeName in mods)
                if (name.StartsWith(singleModeName))
                {
                    var targetPath = Path.Combine(dir, name);
                    if (copy) File.Copy(filePath, targetPath, true);
                    else File.Move(filePath, targetPath, true);
                    break;
                }
        }

        Directory.SetCurrentDirectory("..");
    }
}
