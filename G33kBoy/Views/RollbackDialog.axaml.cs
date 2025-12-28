// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DTC.SM83.Snapshot;

namespace G33kBoy.Views;

/// <summary>
/// Dialog content for previewing and restoring recent snapshots.
/// </summary>
public partial class RollbackDialog : UserControl
{
    private SnapshotHistory m_history;
    private PropertyChangedEventHandler m_historyChangedHandler;
    private bool m_isSnapshottingPaused;
    private bool m_isAttached;

    public RollbackDialog()
    {
        InitializeComponent();

        PropertyChanged += (_, args) =>
        {
            if (args.Property.Name != nameof(DataContext))
                return;

            if (m_history != null && m_historyChangedHandler != null)
                m_history.PropertyChanged -= m_historyChangedHandler;
            m_historyChangedHandler = null;
            ResumeSnapshotting();

            if (DataContext != null)
            {
                m_history = (SnapshotHistory)DataContext;
                if (m_history == null)
                    return;
                PauseSnapshotting();

                m_historyChangedHandler = (_, changeArgs) =>
                {
                    if (changeArgs.PropertyName == nameof(SnapshotHistory.IndexToRestore) ||
                        changeArgs.PropertyName == nameof(SnapshotHistory.ScreenPreview))
                    {
                        if (Dispatcher.UIThread.CheckAccess())
                            PreviewImage?.InvalidateVisual();
                        else
                            Dispatcher.UIThread.Post(() => PreviewImage?.InvalidateVisual());
                    }
                };
                m_history.PropertyChanged += m_historyChangedHandler;
            }
            else
            {
                if (m_history == null)
                    return;

                m_history = null;
            }
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        m_isAttached = true;
        PauseSnapshotting();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        m_isAttached = false;
        ResumeSnapshotting();
    }

    private void OnRollback(object sender, RoutedEventArgs e) =>
        m_history?.Rollback();

    private void PauseSnapshotting()
    {
        if (m_history == null || m_isSnapshottingPaused || !m_isAttached)
            return;
        m_history.PauseSnapshotting();
        m_isSnapshottingPaused = true;
    }

    private void ResumeSnapshotting()
    {
        if (m_history == null || !m_isSnapshottingPaused)
            return;
        m_history.ResumeSnapshotting();
        m_isSnapshottingPaused = false;
    }
}
