// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DTC.Core;
using G33kBoy.ViewModels;

namespace G33kBoy.Views;

public partial class MainWindow : Window
{
    private bool m_isLoaded;

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnAboutDialogClicked(object sender, PointerPressedEventArgs e) =>
        Host.CloseDialogCommand.Execute(sender);

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (m_isLoaded)
            return;
        m_isLoaded = true;

        Logger.Instance.Info("Window loaded.");

        // Kick the UI to update the screen when the emulator updates it.
        var action = new Action(() =>
        {
            if (AmbientDisplay.IsVisible)
                AmbientDisplay.InvalidateVisual();
            MainDisplay.InvalidateVisual();
        });
        ViewModel.GameBoy.DisplayUpdated += (_, _) =>
        {
            try
            {
                Dispatcher.UIThread.InvokeAsync(action);
            }
            catch (TaskCanceledException)
            {
            }
        };
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.DragEffects = TryGetRomFile(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (TryGetRomFile(e, out var romFile))
            ViewModel.LoadRomFile(romFile);
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsDirectionalKey(e.Key))
            return;

        if (e.Source is MenuItem)
            return;

        e.Handled = true;
    }

    private static bool TryGetRomFile(DragEventArgs e, out FileInfo romFile)
    {
        romFile = null;

        var files = e.Data.GetFiles();
        if (files == null)
            return false;

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path) || !IsSupportedRom(path))
                continue;
            romFile = new FileInfo(path);
            return true;
        }

        return false;
    }

    private static bool IsSupportedRom(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".gb", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDirectionalKey(Key key) =>
        key is Key.Left or Key.Right or Key.Up or Key.Down;
}
