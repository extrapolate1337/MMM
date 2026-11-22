using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace aimware.Services;

public class GameService
{
    public bool CanRun(string? gameFloder)
    {
        if (gameFloder == null || !gameFloder.Contains("Marvel.exe") || !gameFloder.Contains("MarvelGame"))
            return false;
        var mod = gameFloder.Replace("Marvel.exe", "");
        if (!Directory.Exists(mod)) return false;
        mod += "Marvel/Binaries/Win64/";
        if (!Directory.Exists(mod)) return false;
        if (!Directory.Exists(mod.Replace("Binaries/Win64/", ""))) return false;
        return true;
    }

    public void Run(string? gameFloder, string absDirectory, string selectedModPack)
    {
        if (gameFloder == null) return;
        var mod = gameFloder.Replace("Marvel.exe", "");
        if (!Directory.Exists(mod)) return;
        mod += "Marvel/Binaries/Win64/";
        if (!Directory.Exists(mod)) return;
        //Console.WriteLine(mod);
        if (!Directory.Exists(mod.Replace("Binaries/Win64/", ""))) return;
        var dsoundDll = mod + "dsound.dll";
        var marvelBypass = mod + "plugins/MarvelRivalsUTOCSignatureBypass.asi";
        if (!File.Exists(marvelBypass.Replace("MarvelRivalsUTOCSignatureBypass.asi", "")))
            Directory.CreateDirectory(mod + "plugins");

        CopyResource("aimware.Resources.dsound.dll", dsoundDll, "dsound.dll");
        CopyResource("aimware.Resources.plugins.MarvelRivalsUTOCSignatureBypass.asi", marvelBypass, "MarvelBypass");

        mod = mod.Replace("Binaries/Win64/", "") + "Content/Paks/";
        if (!Directory.Exists(mod))
        {
            Console.WriteLine(mod + " not found");
            return;
        }

        //Console.WriteLine(absDirectory);
        var path = absDirectory + "modpacks/" + selectedModPack;
        if (!Directory.Exists(path)) return;

        var modsDirectory = mod + "~mods";
        if (Directory.Exists(modsDirectory)) Directory.Delete(modsDirectory, true);
        Directory.CreateDirectory(modsDirectory);

        var mods = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        foreach (var file in mods)
        {
            var name = Path.GetFileName(file);
            //Console.WriteLine(file);
            if (name.StartsWith("🚫")) continue;
            File.Copy(file, Path.Combine(modsDirectory, name), true);
        }

        try
        {
            Process.Start(new ProcessStartInfo("steam://run/2767030")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static void CopyResource(string resourceName, string destination, string displayName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            Console.WriteLine(displayName + " not found");
            return;
        }

        using var file = File.Create(destination);
        stream.CopyTo(file);
        //Console.WriteLine("Copy " + displayName + " Done");
    }
}
