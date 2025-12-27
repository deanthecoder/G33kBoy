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
using System.ComponentModel;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DTC.SM83.Snapshot;

namespace G33kBoy.Views;

/// <summary>
/// Dialog content for previewing and restoring recent snapshots.
/// </summary>
public partial class RollbackDialog : UserControl
{
    private IDisposable m_cpuPauser;
    private SnapshotHistory m_history;
    private PropertyChangedEventHandler m_historyChangedHandler;

    public RollbackDialog()
    {
        InitializeComponent();

        PropertyChanged += (_, args) =>
        {
            if (args.Property.Name != nameof(DataContext))
                return;

            if (DataContext != null)
            {
                m_history = (SnapshotHistory)DataContext;
                if (m_history == null)
                    return;

                m_cpuPauser = m_history.CreatePauser();
                Monitor.Enter(m_history.CpuStepLock);

                m_historyChangedHandler = (_, changeArgs) =>
                {
                    if (changeArgs.PropertyName == nameof(SnapshotHistory.IndexToRestore) ||
                        changeArgs.PropertyName == nameof(SnapshotHistory.ScreenPreview))
                    {
                        PreviewImage?.InvalidateVisual();
                    }
                };
                m_history.PropertyChanged += m_historyChangedHandler;
            }
            else
            {
                if (m_history == null)
                    return;

                if (m_historyChangedHandler != null)
                    m_history.PropertyChanged -= m_historyChangedHandler;
                m_cpuPauser?.Dispose();
                Monitor.Exit(m_history.CpuStepLock);
                m_historyChangedHandler = null;
            }
        };
    }

    private void OnRollback(object sender, RoutedEventArgs e) =>
        m_history?.Rollback();
}
