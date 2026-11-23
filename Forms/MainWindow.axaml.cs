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
using MMM.Services;

namespace MMM;

public partial class MainWindow : Window
{
    private readonly ModPackService _modPacks;
    private readonly ModService _mods;
    private readonly ArchiveService _archive;
    private readonly GameService _game;

    private string _currentAction = "None";
    private string? _gamePath;
    private string SettingsFile => Path.Combine(_modPacks.AbsDirectory, "settings.txt");
    private SecondWindow? _additionallyWindow;

    public MainWindow()
    {
        _modPacks = new ModPackService();
        _mods = new ModService();
        _archive = new ArchiveService();
        _game = new GameService();

        InitializeComponent();
        InitializeApp();
        RestoreGamePath();
        Activated += (_, _) =>
        {
            if (Run.IsEnabled) UpdateGamePathControls();
        };
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
                    Content = Path.GetFileName(modpacks[i]),
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
                    Content = Path.GetFileName(modpacks[i]),
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
        var names = _mods.GetModNames(_modPacks.GetModPackPath(nameModpack));

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
        var gamePath = _gamePath;
        if (!_game.CanRun(gamePath))
        {
            UpdateGamePathControls();
            await ChooseGamePathAsync();
            return;
        }

        var selectedModPack = GetSelectedItemNameCollections();
        if (selectedModPack == null) return;

        var modPackPath = _modPacks.GetModPackPath(selectedModPack);
        if (!Directory.Exists(modPackPath)) return;

        RunText.Text = "🔧...";
        Run.IsEnabled = false;
        try
        {
            _game.Run(gamePath, modPackPath);
            await Task.Delay(500);
        }
        finally
        {
            UpdateGamePathControls();
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

        var selectedIndex = ListBox.SelectedIndex;
        var isEmpty = _modPacks.IsModPackEmpty(_modPacks.GetModPackPath(nameModPack));
        if (!isEmpty && !await ConfirmAction())
            return;

        if (!_modPacks.EnsureModPacksDirectory())
            return;

        _modPacks.DeleteModPack(nameModPack);
        UpdateModpacks();

        if (ListBox.ItemCount > 0)
            ListBox.SelectedIndex = Math.Min(selectedIndex, ListBox.ItemCount - 1);
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
            var destination = _modPacks.GetModPackPath(nameModPack);
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

        _mods.DeleteMods(_modPacks.GetModPackPath(selectedModPack), selectedMods);

        UpdateModpacks();
        ListBox.SelectedIndex = index;
    }

    private void Additionally_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_additionallyWindow is not null)
        {
            _additionallyWindow.Activate();
            return;
        }

        _additionallyWindow = new SecondWindow();
        _additionallyWindow.Closed += (_, _) => _additionallyWindow = null;
        _additionallyWindow.Show(this);
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
        var selectedMods = ListBoxContent.SelectedItems?.OfType<ListBoxItem>().ToArray();
        var hasSelection = selectedMods is { Length: > 0 };
        Copy.IsVisible = Move.IsVisible = hasSelection && ListBox.ItemCount != 1;
        ActiveButton.IsVisible = hasSelection;
        if (selectedMods is not { Length: > 0 }) return;

        var disabledCount = selectedMods.Count(item =>
            item.Content?.ToString()?.StartsWith(ModService.DisabledPrefix, StringComparison.Ordinal) == true);
        ActiveButtonText.Text = disabledCount == selectedMods.Length ? "Activate"
            : disabledCount == 0 ? "Disable"
            : "Toggle";
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
    }

    private void RestoreGamePath()
    {
        try
        {
            if (File.Exists(SettingsFile))
                _gamePath = File.ReadAllText(SettingsFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(exception.Message);
        }

        UpdateGamePathControls();
    }

    private void UpdateGamePathControls()
    {
        var hasGame = _game.CanRun(_gamePath);
        ChangeGameExecutable.IsVisible = hasGame;
        Grid.SetColumn(Run, hasGame ? 2 : 0);
        Grid.SetColumnSpan(Run, hasGame ? 1 : 3);
        RunText.Text = hasGame ? "Start" : "Select Marvel.exe";
    }

    private async Task ChooseGamePathAsync()
    {
        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Marvel.exe",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Marvel Rivals (Marvel.exe)")
                {
                    Patterns = new[] { "Marvel.exe" }
                }
            }
        });

        if (result.Count == 0 || result[0].TryGetLocalPath() is not { } gamePath) return;

        if (!_game.CanRun(gamePath))
        {
            RunText.Text = _game.CanRun(_gamePath) ? "Start" : "Invalid Marvel.exe";
            return;
        }

        _gamePath = gamePath;
        UpdateGamePathControls();

        try
        {
            File.WriteAllText(SettingsFile, gamePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(exception.Message);
            RunText.Text = "Path not saved";
        }
    }

    private async void ChangeGameExecutable_OnClick(object? sender, RoutedEventArgs e)
    {
        await ChooseGamePathAsync();
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
                    Content = Path.GetFileName(dirs[i]),
                    FontFamily = new FontFamily("Verdana"),
                    FontSize = 12
                });

            SetSplit(true);
        }

        _currentAction = "Copy";
        if (_currentAction == "Copy")
            CopyText.Foreground = new SolidColorBrush(Color.Parse("#7bc330"));
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
                    Content = Path.GetFileName(dirs[i]),
                    FontFamily = new FontFamily("Verdana"),
                    FontSize = 12
                });

            SetSplit(true);
        }

        _currentAction = "Move";
        if (_currentAction == "Move")
            MoveText.Foreground = new SolidColorBrush(Color.Parse("#7bc330"));
    }

    private void ActiveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var selectedMods = GetSelectedListModpack();
        var selectedModPack = GetSelectedItemNameCollections();
        if (selectedMods == null || selectedModPack == null) return;
        if (!_modPacks.EnsureModPacksDirectory()) return;

        _mods.ToggleMods(_modPacks.GetModPackPath(selectedModPack), selectedMods);

        var index = ListBox.SelectedIndex;
        UpdateModpacks();
        ListBox.SelectedIndex = index;
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

        var modeName = new List<string>();
        var listBoxItem = ListBoxMenager.SelectedItem as ListBoxItem;
        var targetModPack = listBoxItem?.Content?.ToString();

        var selectedMode = ListBoxContent.SelectedItems;
        if (selectedMode == null) return;
        foreach (var item in selectedMode)
        {
            var listBoxModeItem = item as ListBoxItem;
            if (listBoxModeItem != null)
                modeName.Add(listBoxModeItem.Content?.ToString() ?? string.Empty);
        }

        if (!_modPacks.EnsureModPacksDirectory()) return;

        if (targetModPack != null)
        {
            var targetModPackPath = _modPacks.GetModPackPath(targetModPack);
            if (!Directory.Exists(targetModPackPath)) return;

            if (!await ConfirmAction()) return;

            var selectedModPack = GetSelectedItemNameCollections();
            if (selectedModPack != null)
                _mods.CopyOrMoveMods(
                    _modPacks.GetModPackPath(selectedModPack),
                    targetModPackPath,
                    modeName,
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
    }
}
