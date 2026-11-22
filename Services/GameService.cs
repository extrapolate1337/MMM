using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MMM.Services;

public class GameService
{
    private const string ExecutableName = "Marvel.exe";
    private const string DisabledPrefix = "🚫";
    private const string SteamAppId = "2767030";

    public bool CanRun(string? gameFolder)
    {
        return TryResolve(gameFolder, out _, out _);
    }

    public void Run(string? gameFolder, string modPackPath)
    {
        if (!TryResolve(gameFolder, out var gameRoot, out var binDirectory)) return;
        if (!Directory.Exists(modPackPath)) return;

        var pluginsDirectory = Path.Combine(binDirectory, "plugins");
        Directory.CreateDirectory(pluginsDirectory);

        CopyResource(
            "MMM.Resources.dsound.dll",
            Path.Combine(binDirectory, "dsound.dll"),
            "dsound.dll");
        CopyResource(
            "MMM.Resources.plugins.MarvelRivalsUTOCSignatureBypass.asi",
            Path.Combine(pluginsDirectory, "MarvelRivalsUTOCSignatureBypass.asi"),
            "MarvelBypass");

        var paksDirectory = Path.Combine(gameRoot, "Marvel", "Content", "Paks");
        if (!Directory.Exists(paksDirectory))
        {
            Console.WriteLine(paksDirectory + " not found");
            return;
        }

        var modsDirectory = Path.Combine(paksDirectory, "~mods");
        if (Directory.Exists(modsDirectory)) Directory.Delete(modsDirectory, true);
        Directory.CreateDirectory(modsDirectory);

        foreach (var file in Directory.GetFiles(modPackPath, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith(DisabledPrefix, StringComparison.Ordinal)) continue;

            File.Copy(file, Path.Combine(modsDirectory, name), true);
        }

        LaunchGame();
    }

    private static bool TryResolve(string? selectedPath, out string gameRoot, out string binDirectory)
    {
        gameRoot = string.Empty;
        binDirectory = string.Empty;

        if (string.IsNullOrWhiteSpace(selectedPath)) return false;

        if (!File.Exists(selectedPath)) return false;

        var executablePath = Path.GetFullPath(selectedPath);
        if (!string.Equals(Path.GetFileName(executablePath), ExecutableName, StringComparison.OrdinalIgnoreCase))
            return false;

        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (executableDirectory == null) return false;

        //windows
        var marvelDirectory = new DirectoryInfo(executableDirectory).Parent?.Parent;
        if (marvelDirectory != null
            && string.Equals(marvelDirectory.Name, "Marvel", StringComparison.OrdinalIgnoreCase))
        {
            var candidateRoot = marvelDirectory.Parent?.FullName;
            if (candidateRoot != null && HasGameLayout(candidateRoot, executableDirectory))
            {
                gameRoot = candidateRoot;
                binDirectory = executableDirectory;
                return true;
            }
        }

        //linux
        var candidateBinDirectory = Path.Combine(executableDirectory, "Marvel", "Binaries", "Win64");
        if (HasGameLayout(executableDirectory, candidateBinDirectory))
        {
            gameRoot = executableDirectory;
            binDirectory = candidateBinDirectory;
            return true;
        }

        return false;
    }

    private static bool HasGameLayout(string gameRoot, string binDirectory)
    {
        return Directory.Exists(binDirectory)
               && Directory.Exists(Path.Combine(gameRoot, "Marvel", "Content", "Paks"));
    }

    private static void LaunchGame()
    {
        var url = $"steam://run/{SteamAppId}";

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }

        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            else
                Process.Start(new ProcessStartInfo("xdg-open", url) { UseShellExecute = false });
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
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

        var directory = Path.GetDirectoryName(destination);
        if (directory != null) Directory.CreateDirectory(directory);

        using var file = File.Create(destination);
        stream.CopyTo(file);
    }
}
