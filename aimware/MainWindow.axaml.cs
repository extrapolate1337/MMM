using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace aimware;

public partial class MainWindow : Window
{
    private string _currentAction = "None";
    private string absDirectory;

    private string[] CleanAbsPath;
    private string GameFloder;

    private string LastDirectory;
    private string[] ModPacks;
    private string[] Mods;
    private string noMainDirectory;
    private string[] OriginalNameDirs;

    public MainWindow()
    {
        InitializeComponent();
        InitializeApp();
        if (DragImage != null) DragImage.PointerPressed += DragImage_PointerPressed;
    }

    private void DragImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    private void InitializeApp()
    {
        var absPath = Path.GetFullPath("./");
        noMainDirectory = absPath;
        Console.WriteLine("Full path: " + absPath);
        CleanAbsPath = absPath.Split("/");
        LastDirectory = CleanAbsPath[CleanAbsPath.Length - 2];
        Console.WriteLine("LastDirectory: " + LastDirectory);
        if (CleanAbsPath[CleanAbsPath.Length - 2] != "MMM")
        {
            Directory.CreateDirectory("MMM");
            Directory.SetCurrentDirectory("./MMM");
        }

        absPath = Path.GetFullPath("./");
        CleanAbsPath = absPath.Split("/");
        absDirectory = absPath;
        var directories = Directory.GetDirectories("./");
        //if (!directories.Contains("mods")) Directory.CreateDirectory("mods");
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
                Console.WriteLine("OriginalNameDirs: " + OriginalNameDirs[i]);
            }

            var SizeHieght = modpacks.Length * 30;
            var PositionHieght = SizeHieght + 52;
            if (modpacks.Length > 7)
            {
                SizeHieght = 7 * 30;
                PositionHieght = SizeHieght + 52;
            }

            AddCollection.Margin = new Thickness(1, PositionHieght, 50, 1);
            AddCollection.Width = 120;
            SubCollection.IsVisible = false;
            ListBox.Height = SizeHieght;
            ListBox.IsVisible = true;
            for (var i = 0; i < modpacks.Length; i++)
            {
                var item = new ListBoxItem
                {
                    Content = modpacks[i].Replace("./", ""),
                    FontSize = 10,
                    FontFamily = new FontFamily("Verdana"),
                    Padding = new Thickness(10, 6, 0, 0),
                    Height = 30
                };
                ListBox.Items.Add(item);
            }
        }

        Directory.SetCurrentDirectory("..");
    }

    private bool IsMainPath()
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

    private void UpdateModpacks()
    {
        ListBox.UnselectAll();
        FocusManager.ClearFocus();
        ListBox.Items.Clear();
        var modpacks = Directory.GetDirectories("./");
        ModPacks = modpacks;
        if (modpacks.Length > 0)
        {
            var SizeHieght = modpacks.Length * 30;
            var PositionHieght = SizeHieght + 52;
            if (modpacks.Length > 7)
            {
                SizeHieght = 7 * 30;
                PositionHieght = SizeHieght + 52;
            }

            if (ListBox.SelectedIndex == null)
            {
                AddCollection.Margin = new Thickness(1, PositionHieght, 113, 1);
                AddCollection.Width = 60;
                SubCollection.Margin = new Thickness(1, PositionHieght, 47, 1);
                SubCollection.IsVisible = true;
            }
            else
            {
                AddCollection.Margin = new Thickness(1, PositionHieght, 50, 1);
                AddCollection.Width = 120;
                SubCollection.IsVisible = false;
            }

            ListBox.Height = SizeHieght;
            ListBox.IsVisible = true;
            for (var i = 0; i < modpacks.Length; i++)
            {
                var item = new ListBoxItem
                {
                    Content = modpacks[i].Replace("./", ""),
                    FontSize = 10,
                    FontFamily = new FontFamily("Verdana"),
                    Padding = new Thickness(10, 6, 0, 0),
                    Height = 30
                };
                ListBox.Items.Add(item);
            }

            ListBoxContent.Width = 530;
            ListBoxContent.Margin = new Thickness(1, 46, 201, 1);
            ListBoxMenager.IsVisible = false;
        }

        if (modpacks.Length == 0)
        {
            ListBox.IsVisible = false;
            SubCollection.IsVisible = false;
            AddCollection.Width = 120;
            AddCollection.Margin = new Thickness(1, 50, 50, 1);
        }

        _currentAction = "None";
        if (_currentAction == "None")
        {
            MoveText.Foreground = new SolidColorBrush(Color.Parse("#c0c1c1"));
            CopyText.Foreground = new SolidColorBrush(Color.Parse("#c0c1c1"));
        }
    }

    private void UpdateModeList(string nameModpack)
    {
        ListBoxContent.UnselectAll();
        FocusManager.ClearFocus();
        ListBoxContent.Items.Clear();
        var modpackPath = Path.Combine(Directory.GetCurrentDirectory(), nameModpack);
        Console.WriteLine($"[UpdateModeList] Проверяем директорию: {modpackPath}");

        if (Directory.Exists(modpackPath))
        {
            var files = Directory.GetFiles(modpackPath);
            Console.WriteLine($"[UpdateModeList] Найдено файлов: {files.Length}");
            if (files.Length > 0)
            {
                for (var i = 0; i < files.Length; i++)
                {
                    var name = Path.GetFileName(files[i]);
                    if (!name.EndsWith(".pak")) continue;
                    ListBoxContent.Items.Add(new ListBoxItem
                    {
                        Content = name.Split('.')[0],
                        FontSize = 14,
                        FontFamily = new FontFamily("Verdana"),
                        Padding = new Thickness(10, 6, 0, 0),
                        Height = 40
                    });
                    Console.WriteLine("Добавлен файл: " + Path.GetFileName(files[i]));
                }
            }
            else
            {
                ListBoxContent.IsVisible = true;
                Console.WriteLine("[UpdateModeList] В указанной папке нет файлов.");
            }
        }
        else
        {
            ListBoxContent.IsVisible = false;
            Console.WriteLine($"[UpdateModeList] ОШИБКА: Директория '{modpackPath}' не найдена.");
        }
    }

    private string GetSelectedItemNameCollections()
    {
        if (ListBox.SelectedIndex == -1) return null;
        var Content = new List<string>();
        foreach (var obj in ListBox.Items)
        {
            var items = obj as ListBoxItem;

            if (items != null)
            {
                Content.Add(items.Content.ToString());
                Console.WriteLine(items.Content.ToString());
            }
        }

        return Content[ListBox.SelectedIndex];
    }

    private List<string> GetSelectedListModpack()
    {
        if (ListBoxContent.SelectedItem == null) return null;
        var selectedMode = ListBoxContent.SelectedItems;
        var modpacks = new List<string>();
        foreach (var item in selectedMode)
        {
            var listBoxModeItem = item as ListBoxItem;
            if (listBoxModeItem != null)
                //Console.WriteLine("sDDDelected item: " + listBoxModeItem.Content);
                modpacks.Add(listBoxModeItem.Content.ToString());
        }

        return modpacks;
    }

    private void Exit_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Exit");
        Environment.Exit(0);
    }

    private async void Run_OnClick(object? sender, RoutedEventArgs e)
    {
        if (GetSelectedItemNameCollections() == null) return;
        if (GameFloder == null || !GameFloder.Contains("Marvel.exe") || !GameFloder.Contains("MarvelGame")) return;
        var mod = GameFloder.Replace("Marvel.exe", "");
        if (!Directory.Exists(mod)) return;
        mod += "Marvel/Binaries/Win64/";
        if (!Directory.Exists(mod)) return;
        Console.WriteLine(mod);
        if (!Directory.Exists(mod.Replace("Binaries/Win64/", ""))) return;
        var dsoundDll = mod + "dsound.dll";
        var marvelBypass = mod + "plugins/MarvelRivalsUTOCSignatureBypass.asi";
        if (!File.Exists(marvelBypass.Replace("MarvelRivalsUTOCSignatureBypass.asi", "")))
            Directory.CreateDirectory(mod + "plugins");
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("aimware.Resources.dsound.dll"))
        {
            if (stream == null)
            {
                Console.WriteLine("dsound not found");
            }
            else
            {
                using (var fs = new FileStream(dsoundDll, FileMode.Create))
                {
                    stream.CopyTo(fs);
                }

                Console.WriteLine("Copy dsound.dll Done");
            }
        }

        using (var stream =
               assembly.GetManifestResourceStream("aimware.Resources.plugins.MarvelRivalsUTOCSignatureBypass.asi"))
        {
            if (stream == null)
            {
                Console.WriteLine("marvelBypass not found");
            }
            else
            {
                using (var fs = new FileStream(marvelBypass, FileMode.Create))
                {
                    stream.CopyTo(fs);
                }

                Console.WriteLine("Copy MarvelBypass Done");
            }
        }

        mod = mod.Replace("Binaries/Win64/", "") + "Content/Paks/";
        if (!Directory.Exists(mod))
        {
            Console.WriteLine(mod + " not found");
            return;
        }

        if (Directory.Exists(mod + "~mods")) Directory.Delete(mod + "~mods", true);
        Directory.CreateDirectory(mod + "~mods");
        Console.WriteLine(absDirectory);
        if (!Directory.Exists(absDirectory + "modpacks/") ||
            !Directory.Exists(absDirectory + "modpacks/" + GetSelectedItemNameCollections())) return;
        RunText.Text = "🔧 ⏳ 🔧";
        await Task.Delay(500);
        Run.IsEnabled = false;
        var path = absDirectory + "modpacks/" + GetSelectedItemNameCollections();
        Directory.SetCurrentDirectory(absDirectory + "modpacks/" + GetSelectedItemNameCollections());
        var mods = Directory.GetFiles("./");
        Directory.SetCurrentDirectory(mod + "~mods");
        foreach (var file in mods)
        {
            Console.WriteLine(file);
            if (file.Replace("./", "").StartsWith("🚫")) continue;
            File.Copy(Path.Combine(path, file), file.Replace("./", ""), true);
        }

        Directory.SetCurrentDirectory(absDirectory);
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

        RunText.Text = "Start";
        Run.IsEnabled = true;
    }

    private void AddCollection_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!IsMainPath())
            try
            {
                Directory.SetCurrentDirectory(absDirectory);
                Console.WriteLine(Path.GetFullPath("./"));
            }
            catch (Exception exception)
            {
                try
                {
                    Directory.SetCurrentDirectory(noMainDirectory);
                }
                catch (Exception e1)
                {
                    Console.WriteLine(e1);
                    return;
                }

                Console.WriteLine(exception);
            }

        var directory = Directory.GetCurrentDirectory();
        if (!directory.Contains("modpacks")) Directory.CreateDirectory("modpacks");
        Directory.SetCurrentDirectory("./modpacks");
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
                    UpdateModpacks();
                    Console.WriteLine("Создан " + a);
                    IsNew = false;
                }

                i++;
            }
        }
        else
        {
            Directory.CreateDirectory("New ModPack");
            Console.WriteLine("Создан New ModPack");
            UpdateModpacks();
        }
    }

    private async Task<bool> IsDialog()
    {
        var WarningWindow = new WarningWindow();
        var result = await WarningWindow.ShowDialog<bool>(this);
        Console.WriteLine(result);
        return result;
    }

    private async void SubCollection_OnClick(object? sender, RoutedEventArgs e)
    {
        if (await IsDialog())
        {
            if (!IsMainPath())
                try
                {
                    Directory.SetCurrentDirectory(absDirectory);
                }
                catch (Exception exception)
                {
                    try
                    {
                        Directory.SetCurrentDirectory(noMainDirectory);
                    }
                    catch (Exception e1)
                    {
                        Console.WriteLine(e1);
                        return;
                    }

                    Console.WriteLine(exception);
                }

            var directory = Directory.GetCurrentDirectory();
            if (!directory.Contains("modpacks")) Directory.CreateDirectory("modpacks");
            Directory.SetCurrentDirectory("./modpacks");
            var packs = Directory.GetDirectories("./");
            var nameModPack = GetSelectedItemNameCollections();
            for (var i = 0; i < packs.Length; i++)
                if (packs[i].Replace("./", "") == nameModPack)
                {
                    Directory.Delete(packs[i], true);
                    Console.WriteLine(packs[i].Replace("./", "") + " deleted");
                    break;
                }

            UpdateModpacks();
            Directory.SetCurrentDirectory("..");
        }
    }

    private async void AddAddon_OnClick(object? sender, RoutedEventArgs e)
    {
        SelectArchive("Mode Archive");
    }

    private async void ExtractArchive(string[] path)
    {
        var currentFloder = path;
        if (path == null || path.Length == 0)
        {
            Console.WriteLine("LOL");
            return;
        }

        var archive = currentFloder.Last();
        Console.WriteLine("Select: Archive" + archive);
        if (!IsMainPath())
            try
            {
                Directory.SetCurrentDirectory(absDirectory);
            }
            catch (Exception exception)
            {
                try
                {
                    Directory.SetCurrentDirectory(noMainDirectory);
                }
                catch (Exception e1)
                {
                    Console.WriteLine(e1);
                    return;
                }

                Console.WriteLine(exception);
            }

        if (string.IsNullOrEmpty(archive)) return;
        var nameArchive = archive.Split('/').Last();
        var NameArchive = nameArchive.Split('.');
        var cleanNameArchive = NameArchive[0];
        if (ListBox.Items.Count == 0 || ListBox.SelectedItem == null)
        {
            var directory = Directory.GetCurrentDirectory();
            if (!directory.Contains("modpacks")) Directory.CreateDirectory("modpacks");
            Directory.SetCurrentDirectory("./modpacks");
            Directory.CreateDirectory(cleanNameArchive);
            Directory.SetCurrentDirectory(cleanNameArchive);
            Directory.SetCurrentDirectory("..");
            UpdateModpacks();
            if (ListBox.Items.Count == 1)
            {
                ListBox.SelectedIndex = 0;
                TextBox.Focus();
            }

            if (ListBox.Items.Count > 1)
            {
                ListBox.SelectedIndex = ListBox.Items.Count - 1;
                TextBox.Focus();
            }

            Directory.SetCurrentDirectory(cleanNameArchive);
            try
            {
                using (var mode = ArchiveFactory.Open(archive))
                {
                    foreach (var entry in mode.Entries)
                        if (!entry.IsDirectory)
                            entry.WriteToDirectory("./", new ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            Directory.SetCurrentDirectory("../..");
        }

        if (ListBox.Items.Count > 0 && ListBox.SelectedItem != null)
        {
            var lastX = ListBox.SelectedIndex;
            var dermo = "./" + GetSelectedItemNameCollections();
            var directory = Directory.GetCurrentDirectory();
            if (!directory.Contains("modpacks")) Directory.CreateDirectory("modpacks");
            Directory.SetCurrentDirectory("./modpacks");
            Directory.CreateDirectory(dermo);
            Directory.SetCurrentDirectory(dermo);
            Directory.SetCurrentDirectory("..");
            UpdateModpacks();
            ListBox.SelectedIndex = lastX;
            TextBox.Focus();
            Directory.SetCurrentDirectory(dermo);
            try
            {
                using (var mode = ArchiveFactory.Open(archive))
                {
                    foreach (var entry in mode.Entries)
                        if (!entry.IsDirectory)
                            entry.WriteToDirectory("./", new ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            Directory.SetCurrentDirectory("../..");
        }
    }

    private async void SelectArchive(string title)
    {
        var fileDialog = new OpenFileDialog
        {
            Title = title,
            Directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            AllowMultiple = false
        };

        var result = await fileDialog.ShowAsync(this);

        if (result.Length > 0 && !string.IsNullOrEmpty(result[0])) ExtractArchive(result);
        Console.WriteLine(result);
    }

    private async void SubAddon_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ListBoxContent.SelectedItem == null) return;
        var dir = Directory.GetCurrentDirectory();
        if (!dir.Contains("modpacks"))
            try
            {
                Directory.SetCurrentDirectory("modpacks");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Directory.CreateDirectory("modpacks");
                Directory.SetCurrentDirectory("modpacks");
            }

        var selectedModPack = GetSelectedItemNameCollections();
        var selectedMods = GetSelectedListModpack();
        if (selectedMods == null) return;
        var index = ListBox.SelectedIndex;

        if (!await IsDialog()) return;
        foreach (var mod in selectedMods)
        {
            Directory.SetCurrentDirectory(selectedModPack);
            var packs = Directory.GetFiles("./");
            foreach (var pack in packs)
                if (pack.Replace("./", "").StartsWith(mod))
                {
                    Console.WriteLine("CWD " + pack);
                    File.Delete(pack);
                }

            Directory.SetCurrentDirectory("..");
        }

        UpdateModpacks();
        ListBox.SelectedIndex = index;
        Directory.SetCurrentDirectory("..");
    }

    private void Additionally_OnClick(object? sender, RoutedEventArgs e)
    {
        var additionallyWindow = new SecondWindow();
        additionallyWindow.Show();
    }

    private void ListBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        //Console.WriteLine("SelectionChanged: "+ Directory.GetCurrentDirectory());
        var listBox = sender as ListBox;
        if (listBox == null) return;
        if (listBox.SelectedItem == null)
        {
            TextBox.IsVisible = false;
            ListBoxContent.IsVisible = false;
            return;
        }

        var SizeHieght = ModPacks.Length * 30;
        var PositionHieght = SizeHieght + 52;
        if (ModPacks.Length > 7)
        {
            SizeHieght = 7 * 30;
            PositionHieght = SizeHieght + 52;
        }

        AddCollection.Margin = new Thickness(1, PositionHieght, 113, 1);
        AddCollection.Width = 60;
        SubCollection.Margin = new Thickness(1, PositionHieght, 47, 1);
        SubCollection.Width = 60;
        SubCollection.IsVisible = true;
        var item = listBox.SelectedItem as ListBoxItem;
        var content = item.Content.ToString();
        if (listBox.SelectedItem == item)
        {
            TextBox.IsVisible = true;
            ListBoxContent.IsVisible = true;
            TextBox.Text = content;
            TextBox.FontFamily = new FontFamily("Verdana");
            TextBox.FontSize = 12;
            var name = GetSelectedItemNameCollections();
            UpdateModeList(name);
        }
    }

    private void ListBoxContent_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ListBoxContent.SelectedItem == null)
        {
            Copy.IsVisible = false;
            Move.IsVisible = false;
            ActiveButton.IsVisible = false;
            return;
        }


        var item = ListBoxContent.SelectedItem;
        if (ListBoxContent.SelectedItem == item)
        {
            if (ListBox.ItemCount == 1)
            {
                Copy.IsVisible = false;
                Move.IsVisible = false;
            }
            else
            {
                Copy.IsVisible = true;
                Move.IsVisible = true;
            }

            ActiveButton.IsVisible = true;
        }

        Console.WriteLine("ListBoxContent_OnSelectionChanged");
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!IsMainPath())
            try
            {
                Directory.SetCurrentDirectory(absDirectory);
            }
            catch (Exception exception)
            {
                try
                {
                    Directory.SetCurrentDirectory(noMainDirectory);
                }
                catch (Exception e1)
                {
                    Console.WriteLine(e1);
                    return;
                }

                Console.WriteLine(exception);
            }

        var directory = Directory.GetCurrentDirectory();
        if (!directory.Contains("modpacks"))
        {
            Directory.CreateDirectory("modpacks");
            Directory.SetCurrentDirectory("modpacks");
        }

        Console.WriteLine(Path.GetFullPath("./"));
        var dirs = Directory.GetDirectories("./");
        var oldName = dirs[ListBox.SelectedIndex];
        var newName = TextBox.Text;
        var item = ListBox.SelectedItem as ListBoxItem;
        var content = item.Content.ToString();
        Console.WriteLine("Listbox current content: " + content);
        Console.WriteLine("Old: " + oldName + " New: " + newName);
        Console.WriteLine(ListBox.ItemCount);
        var Content = new List<string>();
        foreach (var obj in ListBox.Items)
        {
            var items = obj as ListBoxItem;

            if (items != null)
            {
                Content.Add(items.Content.ToString());
                Console.WriteLine(items.Content.ToString());
            }
        }

        if (oldName != newName)
        {
            var newPath = Path.Combine(Path.GetDirectoryName(oldName), newName);
            Console.WriteLine(newPath);
            if (!Directory.Exists(newPath))
            {
                Directory.Move(oldName, newPath);
                var lastIndex = 0;
                for (var i = 0; i < Content.Count; i++)
                    if (Content[i] == newName)
                        lastIndex = i;

                UpdateModpacks();
                ListBox.SelectedIndex = lastIndex;
                TextBox.Focus();
                Content.Clear();
                foreach (var obj in ListBox.Items)
                {
                    var items = obj as ListBoxItem;

                    if (items != null)
                    {
                        Content.Add(items.Content.ToString());
                        Console.WriteLine(items.Content.ToString());
                    }
                }

                lastIndex = 0;
                for (var i = 0; i < Content.Count; i++)
                    if (Content[i] == newName)
                        lastIndex = i;

                Content.Clear();
                ListBox.SelectedIndex = lastIndex;
                TextBox.Focus();
            }
        }

        if (!TextBox.IsFocused)
        {
            var lastIndex = ListBox.SelectedIndex;
            UpdateModpacks();
            ListBox.SelectedIndex = lastIndex;
        }

        _currentAction = "None";
        Directory.SetCurrentDirectory("..");
    }

    private async Task<string> SelectFolderGame(string title)
    {
        var gameDialog = new OpenFileDialog
        {
            Title = title,
            Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        var result = await gameDialog.ShowAsync(this);

        if (result.Length > 0 && !string.IsNullOrEmpty(result[0])) return result[0];
        return null;
    }

    private async void Floder_OnClick(object? sender, RoutedEventArgs e)
    {
        var currentFloder = await SelectFolderGame("Select: Marvel.exe");
        GameFloder = currentFloder;
        Console.WriteLine("Target PATH: " + currentFloder);
    }

    private void CopyMode_OnClick(object? sender, RoutedEventArgs e)
    {
        ListBoxMenager.Items.Clear();
        var dir = Directory.GetCurrentDirectory();
        if (!dir.Contains("modpacks"))
            try
            {
                Directory.SetCurrentDirectory("modpacks");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Directory.CreateDirectory("modpacks");
                Directory.SetCurrentDirectory("modpacks");
            }

        var dirs = Directory.GetDirectories("./");
        if (dirs.Length > 0)
        {
            for (var i = 0; i < dirs.Length; i++)
                if (dirs[i].Replace("./", "") == GetSelectedItemNameCollections()) continue;
                else
                    ListBoxMenager.Items.Add(new ListBoxItem
                    {
                        Content = dirs[i].Replace("./", ""),
                        FontFamily = new FontFamily("Verdana"),
                        FontSize = 12
                    });

            ListBoxContent.Width = 263;
            ListBoxContent.Margin = new Thickness(67, 46, 1, 1);
            ListBoxMenager.IsVisible = true;
        }

        _currentAction = "Copy";
        if (_currentAction == "Copy") CopyText.Foreground = new SolidColorBrush(Color.Parse("#7bc330"));
        Directory.SetCurrentDirectory("..");
    }

    private void Move_OnClick(object? sender, RoutedEventArgs e)
    {
        ListBoxMenager.Items.Clear();
        var dir = Directory.GetCurrentDirectory();
        if (!dir.Contains("modpacks"))
            try
            {
                Directory.SetCurrentDirectory("modpacks");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Directory.CreateDirectory("modpacks");
                Directory.SetCurrentDirectory("modpacks");
            }

        var dirs = Directory.GetDirectories("./");
        if (dirs.Length > 0)
        {
            for (var i = 0; i < dirs.Length; i++)
                if (dirs[i].Replace("./", "") == GetSelectedItemNameCollections()) continue;
                else
                    ListBoxMenager.Items.Add(new ListBoxItem
                    {
                        Content = dirs[i].Replace("./", ""),
                        FontFamily = new FontFamily("Verdana"),
                        FontSize = 12
                    });


            ListBoxContent.Width = 263;
            ListBoxContent.Margin = new Thickness(67, 46, 1, 1);
            ListBoxMenager.IsVisible = true;
        }

        _currentAction = "Move";
        if (_currentAction == "Move") MoveText.Foreground = new SolidColorBrush(Color.Parse("#7bc330"));
        Directory.SetCurrentDirectory("..");
    }

    private void ActiveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var niger = GetSelectedListModpack();
        Console.WriteLine(Directory.GetCurrentDirectory());
        if (!Directory.GetCurrentDirectory().EndsWith("modpacks"))
            try
            {
                Directory.SetCurrentDirectory("./modpacks");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return;
            }

        Directory.SetCurrentDirectory(GetSelectedItemNameCollections());
        var files = Directory.GetFiles("./");
        for (var j = 0; j < GetSelectedListModpack().Count; j++)
        {
            var iname = GetSelectedListModpack()[j];
            for (var i = 0; i < files.Length; i++)
            {
                var name = files[i].Replace("./", "");
                if (name.StartsWith(iname))
                {
                    if (name.StartsWith("🚫"))
                    {
                        Console.WriteLine("YES " + name);
                        File.Move(name, name.Replace("🚫", ""));
                    }
                    else
                    {
                        Console.WriteLine("NOOO " + name);
                        File.Move(name, "🚫" + name);
                    }
                }
            }
        }

        Directory.SetCurrentDirectory("..");
        var index = ListBox.SelectedIndex;
        UpdateModpacks();
        ListBox.SelectedIndex = index;
        Directory.SetCurrentDirectory("..");
    }

    private async void ListBoxMenager_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ListBoxMenager.SelectedItem == null) return;
        if (_currentAction == "None")
        {
            MoveText.Foreground = new SolidColorBrush(Color.Parse("#7bc330"));
            CopyText.Foreground = new SolidColorBrush(Color.Parse("#c0c1c1"));
            return;
        }

        var itemName = "";
        var modeName = new List<string>();
        var selectedItemObject = ListBoxMenager.SelectedItem;
        var listBoxItem = selectedItemObject as ListBoxItem;
        if (listBoxItem != null)
        {
            Console.WriteLine("Menager: " + listBoxItem.Content);
            itemName = "./" + listBoxItem.Content;
        }

        var selectedMode = ListBoxContent.SelectedItems;
        foreach (var item in selectedMode)
        {
            var listBoxModeItem = item as ListBoxItem;
            if (listBoxModeItem != null)
            {
                Console.WriteLine("selected item: " + listBoxModeItem.Content);
                modeName.Add(listBoxModeItem.Content.ToString());
            }
        }

        var dir = Directory.GetCurrentDirectory();
        if (!dir.Contains("modpacks"))
            try
            {
                Directory.SetCurrentDirectory("modpacks");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Directory.CreateDirectory("modpacks");
                Directory.SetCurrentDirectory("modpacks");
            }


        var modPackDirs = Directory.GetDirectories("./");
        //Console.WriteLine($"mode: {modeName}, itemName: {itemName}");
        if (modPackDirs.Length > 0)
        {
            dir = Path.GetFullPath(listBoxItem.Content.ToString());
            Directory.SetCurrentDirectory(GetSelectedItemNameCollections());
            var dirs = Directory.GetFiles("./");
            if (dir == Directory.GetCurrentDirectory()) return;
            if (modPackDirs.Contains(itemName))
            {
                if (!await IsDialog())
                {
                    Directory.SetCurrentDirectory("../..");
                    return;
                }

                foreach (var filePath in dirs)
                foreach (var singleModeName in modeName)
                    if (filePath.Replace("./", "").StartsWith(singleModeName))
                    {
                        if (_currentAction == "Copy") File.Copy(filePath, dir + filePath.Replace("./", "/"), true);
                        else if (_currentAction == "Move") File.Move(filePath, dir + filePath.Replace("./", "/"), true);
                        break;
                    }

                //Console.WriteLine("Copied: " + listBoxModeItem.Content + " in: " + dir);
            }

            Directory.SetCurrentDirectory("..");
        }

        _currentAction = "None";
        if (_currentAction == "None")
        {
            MoveText.Foreground = new SolidColorBrush(Color.Parse("#c0c1c1"));
            CopyText.Foreground = new SolidColorBrush(Color.Parse("#c0c1c1"));
        }

        var index = ListBox.SelectedIndex;
        UpdateModpacks();
        ListBox.SelectedIndex = index;

        Directory.SetCurrentDirectory("..");
    }
}