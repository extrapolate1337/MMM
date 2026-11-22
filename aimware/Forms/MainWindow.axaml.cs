using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using aimware.Services;

namespace aimware;

public partial class MainWindow : Window
{
    private readonly ModPackService _modPacks;
    private readonly ModService _mods;
    private readonly ArchiveService _archive;
    private readonly GameService _game;

    private string _currentAction = "None";
    private string? GameFloder;

    public MainWindow()
    {
        _modPacks = new ModPackService();
        _mods = new ModService();
        _archive = new ArchiveService();
        _game = new GameService();

        InitializeComponent();
        InitializeApp();
        if (DragImage != null) DragImage.PointerPressed += DragImage_PointerPressed;
        if (Design.IsDesignMode)
            return;
    }

    private void DragImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    private void InitializeApp()
    {
        var modpacks = _modPacks.Initialize();
        if (modpacks.Length > 0)
        {
            AddCollection.Width = 120;
            SubCollection.IsVisible = false;
            ListBox.IsVisible = true;
            for (var i = 0; i < modpacks.Length; i++)
            {
                var item = new ListBoxItem
                {
                    Content = modpacks[i].Replace("./", ""),
                    FontSize = 10,
                    FontFamily = new FontFamily("Verdana"),
                    Padding = new Thickness(10, 6, 8, 0),
                    Height = 30
                };
                ListBox.Items.Add(item);
            }
        }
    }

    private void SetSplit(bool split)
    {
        if (split)
        {
            Grid.SetColumn(ListBoxContent, 2);
            Grid.SetColumnSpan(ListBoxContent, 1);
            ListBoxMenager.IsVisible = true;
        }
        else
        {
            Grid.SetColumn(ListBoxContent, 0);
            Grid.SetColumnSpan(ListBoxContent, 3);
            ListBoxMenager.IsVisible = false;
        }
    }

    private void UpdateModpacks()
    {
        ListBox.UnselectAll();
        FocusManager?.ClearFocus();
        ListBox.Items.Clear();
        var modpacks = _modPacks.GetModPacks();
        if (modpacks.Length > 0)
        {
            if (ListBox.SelectedIndex == -1)
            {
                AddCollection.Width = 60;
                SubCollection.IsVisible = true;
            }
            else
            {
                AddCollection.Width = 120;
                SubCollection.IsVisible = false;
            }

            ListBox.IsVisible = true;
            for (var i = 0; i < modpacks.Length; i++)
            {
                var item = new ListBoxItem
                {
                    Content = modpacks[i].Replace("./", ""),
                    FontSize = 10,
                    FontFamily = new FontFamily("Verdana"),
                    Padding = new Thickness(10, 6, 8, 0),
                    Height = 30
                };
                ListBox.Items.Add(item);
            }

            SetSplit(false);
        }

        if (modpacks.Length == 0)
        {
            ListBox.IsVisible = false;
            SubCollection.IsVisible = false;
            AddCollection.Width = 120;
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
        FocusManager?.ClearFocus();
        ListBoxContent.Items.Clear();
        var names = _mods.GetModNames(_modPacks.AbsDirectory + "modpacks/" + nameModpack);

        if (names == null)
        {
            ListBoxContent.IsVisible = false;
            return;
        }

        ListBoxContent.IsVisible = true;
        if (names.Count == 0)
        {
            return;
        }

        foreach (var name in names)
            ListBoxContent.Items.Add(new ListBoxItem
            {
                Content = name,
                FontSize = 14,
                FontFamily = new FontFamily("Verdana"),
                Padding = new Thickness(10, 6, 8, 0),
                Height = 40
            });
    }

    private string? GetSelectedItemNameCollections()
    {
        if (ListBox.SelectedIndex == -1) return null;
        var Content = new List<string>();
        foreach (var obj in ListBox.Items)
        {
            var items = obj as ListBoxItem;

            if (items != null)
            {
                Content.Add(items.Content?.ToString() ?? string.Empty);
                //Console.WriteLine(items.Content.ToString());
            }
        }

        if (ListBox.SelectedIndex < 0 || ListBox.SelectedIndex >= Content.Count) return null;
        return Content[ListBox.SelectedIndex];
    }

    private List<string>? GetSelectedListModpack()
    {
        if (ListBoxContent.SelectedItem == null) return null;
        var selectedMode = ListBoxContent.SelectedItems;
        if (selectedMode == null) return null;

        var modpacks = new List<string>();
        foreach (var item in selectedMode)
        {
            var listBoxModeItem = item as ListBoxItem;
            if (listBoxModeItem != null)
                modpacks.Add(listBoxModeItem.Content?.ToString() ?? string.Empty);
        }

        return modpacks;
    }

    private void Exit_OnClick(object? sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    private async void Run_OnClick(object? sender, RoutedEventArgs e)
    {
        var selectedModPack = GetSelectedItemNameCollections();
        if (selectedModPack == null) return;

        var gameFloder = GameFloder;
        if (gameFloder == null || !_game.CanRun(gameFloder)) return;
        if (!Directory.Exists(_modPacks.AbsDirectory + "modpacks/" + selectedModPack)) return;

        RunText.Text = "🔧 ⏳ 🔧";
        await Task.Delay(500);
        Run.IsEnabled = false;
        try
        {
            _game.Run(gameFloder, _modPacks.AbsDirectory, selectedModPack);
        }
        finally
        {
            RunText.Text = "Start";
            Run.IsEnabled = true;
        }
    }

    private void AddCollection_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_modPacks.EnsureModPacksDirectory()) return;

        _modPacks.CreateModPack();
        UpdateModpacks();
    }

    private async Task<bool> ConfirmAction()
    {
        var WarningWindow = new WarningWindow();
        var result = await WarningWindow.ShowDialog<bool>(this);
        //Console.WriteLine(result);
        return result;
    }

    private async void SubCollection_OnClick(object? sender, RoutedEventArgs e)
    {
        var nameModPack = GetSelectedItemNameCollections();
        if (nameModPack == null) return;

        var isEmpty = _modPacks.IsModPackEmpty(_modPacks.AbsDirectory + "modpacks/" + nameModPack);
        if (!isEmpty && !await ConfirmAction())
            return;

        if (!_modPacks.EnsureModPacksDirectory())
            return;

        _modPacks.DeleteModPack(nameModPack);
        UpdateModpacks();

        Directory.SetCurrentDirectory("..");
    }

    private async void AddAddon_OnClick(object? sender, RoutedEventArgs e)
    {
        await SelectArchive("Mode Archive");
    }

    private void ExtractArchive(string[] paths)
    {
        if (paths == null || paths.Length == 0) return;
        if (!_modPacks.EnsureModPacksDirectory()) return;

        var selectedModPack = GetSelectedItemNameCollections();
        var lastCreated = selectedModPack;

        foreach (var archive in paths)
        {
            if (string.IsNullOrEmpty(archive)) continue;

            var nameModPack = selectedModPack ?? Path.GetFileNameWithoutExtension(archive);
            var destination = _modPacks.AbsDirectory + "modpacks/" + nameModPack;
            Console.WriteLine("Select: Archive " + archive);
            Console.WriteLine("Extract to: " + destination);

            try
            {
                _archive.Extract(archive, destination);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            lastCreated = nameModPack;
        }

        UpdateModpacks();
        if (lastCreated == null) return;

        SelectModPack(lastCreated);
        TextBox.Focus();
    }

    private void SelectModPack(string nameModPack)
    {
        for (var i = 0; i < ListBox.ItemCount; i++)
        {
            var item = ListBox.Items[i] as ListBoxItem;
            if (item != null && item.Content?.ToString() == nameModPack)
            {
                ListBox.SelectedIndex = i;
                return;
            }
        }
    }

    private async Task SelectArchive(string title)
    {
        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true
        });

        var paths = result.Select(file => file.Path.LocalPath).ToArray();
        if (paths.Length > 0) ExtractArchive(paths);
    }

    private async void SubAddon_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ListBoxContent.SelectedItem == null)
            return;

        if (!_modPacks.EnsureModPacksDirectory())
            return;

        var selectedModPack = GetSelectedItemNameCollections();
        var selectedMods = GetSelectedListModpack();
        if (selectedModPack == null || selectedMods == null)
            return;

        var index = ListBox.SelectedIndex;

        if (!await ConfirmAction())
            return;

        _mods.DeleteMods(selectedModPack, selectedMods);

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
        var listBox = sender as ListBox;
        if (listBox == null) return;
        if (listBox.SelectedItem == null)
        {
            TextBox.IsVisible = false;
            ListBoxContent.IsVisible = false;
            return;
        }

        AddCollection.Width = 60;
        SubCollection.IsVisible = true;
        var item = listBox.SelectedItem as ListBoxItem;
        if (item == null) return;
        var content = item.Content?.ToString() ?? string.Empty;
        if (listBox.SelectedItem == item)
        {
            TextBox.IsVisible = true;
            ListBoxContent.IsVisible = true;
            TextBox.Text = content;
            TextBox.FontFamily = new FontFamily("Verdana");
            TextBox.FontSize = 12;
            var name = GetSelectedItemNameCollections();
            if (name == null) return;
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

    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_modPacks.EnsureModPacksDirectory()) return;

        //Console.WriteLine(Path.GetFullPath("./"));
        var oldName = _modPacks.GetModPackDirectoryAt(ListBox.SelectedIndex);
        var newName = TextBox.Text ?? string.Empty;
        //Console.WriteLine("Listbox current content: " + content);
        //Console.WriteLine("Old: " + oldName + " New: " + newName);
        //Console.WriteLine(ListBox.ItemCount);
        var Content = new List<string>();
        foreach (var obj in ListBox.Items)
        {
            var items = obj as ListBoxItem;

            if (items != null)
            {
                Content.Add(items.Content?.ToString() ?? string.Empty);
                //Console.WriteLine(items.Content.ToString());
            }
        }

        if (oldName != null && oldName != newName)
        {
            if (_modPacks.Rename(oldName, newName))
            {
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
                        Content.Add(items.Content?.ToString() ?? string.Empty);
                        //Console.WriteLine(items.Content.ToString());
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

    private async Task<string?> SelectFolderGame(string title)
    {
        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        if (result.Count == 0) return null;
        var path = result[0].Path.LocalPath;
        return string.IsNullOrEmpty(path) ? null : path;
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
        if (!_modPacks.EnsureModPacksDirectory()) return;

        var dirs = _modPacks.GetOtherModPacks(GetSelectedItemNameCollections());
        if (dirs.Length > 0)
        {
            for (var i = 0; i < dirs.Length; i++)
                ListBoxMenager.Items.Add(new ListBoxItem
                {
                    Content = dirs[i].Replace("./", ""),
                    FontFamily = new FontFamily("Verdana"),
                    FontSize = 12
                });

            SetSplit(true);
        }

        _currentAction = "Copy";
        if (_currentAction == "Copy")
            CopyText.Foreground = new SolidColorBrush(Color.Parse("#7bc330"));

        Directory.SetCurrentDirectory("..");
    }

    private void Move_OnClick(object? sender, RoutedEventArgs e)
    {
        ListBoxMenager.Items.Clear();
        if (!_modPacks.EnsureModPacksDirectory()) return;

        var dirs = _modPacks.GetOtherModPacks(GetSelectedItemNameCollections());
        if (dirs.Length > 0)
        {
            for (var i = 0; i < dirs.Length; i++)
                ListBoxMenager.Items.Add(new ListBoxItem
                {
                    Content = dirs[i].Replace("./", ""),
                    FontFamily = new FontFamily("Verdana"),
                    FontSize = 12
                });

            SetSplit(true);
        }

        _currentAction = "Move";
        if (_currentAction == "Move")
            MoveText.Foreground = new SolidColorBrush(Color.Parse("#7bc330"));

        Directory.SetCurrentDirectory("..");
    }

    private void ActiveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var selectedMods = GetSelectedListModpack();
        var selectedModPack = GetSelectedItemNameCollections();
        if (selectedMods == null || selectedModPack == null) return;
        if (!_modPacks.EnsureModPacksDirectory()) return;

        _mods.ToggleMods(selectedModPack, selectedMods);

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
            //Console.WriteLine("Manager: " + listBoxItem.Content);
            itemName = "./" + (listBoxItem.Content?.ToString() ?? string.Empty);
        }

        var selectedMode = ListBoxContent.SelectedItems;
        if (selectedMode == null) return;
        foreach (var item in selectedMode)
        {
            var listBoxModeItem = item as ListBoxItem;
            if (listBoxModeItem != null)
            {
                //Console.WriteLine("selected item: " + listBoxModeItem.Content);
                modeName.Add(listBoxModeItem.Content?.ToString() ?? string.Empty);
            }
        }

        if (!_modPacks.EnsureModPacksDirectory()) return;

        var modPackDirs = Directory.GetDirectories("./");
        if (modPackDirs.Length > 0 && listBoxItem != null)
        {
            if (!modPackDirs.Contains(itemName))
            {
                Directory.SetCurrentDirectory("..");
                return;
            }

            if (!await ConfirmAction())
            {
                Directory.SetCurrentDirectory("..");
                return;
            }

            var selectedModPack = GetSelectedItemNameCollections();
            var targetModPack = listBoxItem.Content?.ToString();
            if (selectedModPack != null && targetModPack != null)
                _mods.CopyOrMoveMods(selectedModPack, targetModPack, modeName,
                    _currentAction == "Copy");
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
