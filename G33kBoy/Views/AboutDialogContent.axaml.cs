// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using DTC.Core.Extensions;

namespace G33kBoy.Views;

public partial class AboutDialogContent : UserControl
{
    public AboutDialogContent()
    {
        InitializeComponent();

        AppVersion.Text = $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";
    }

    private void OnGitHubLinkPressed(object sender, PointerPressedEventArgs e) =>
        new Uri("https://github.com/deanthecoder").Open();
}