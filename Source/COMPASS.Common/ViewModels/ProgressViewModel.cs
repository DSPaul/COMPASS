using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Models.Progress;

namespace COMPASS.Common.ViewModels;

/// <summary>
/// UI layer over <see cref="ProgressTrackingManager"/>
/// </summary>
public class ProgressViewModel : ViewModelBase, IDisposable
{
    private readonly ProgressTrackingManager _progressManager;

    public ProgressViewModel(ProgressTrackingManager progressManager)
    {
        _progressManager = progressManager;
        _progressManager.TrackingChanged += OnTrackingChanged;
        RefreshTrackers();
    }

    public ObservableCollection<TrackedOperation> TrackedOperations { get; } = [];

    public TrackedOperation? PrimaryOperation
    {
        get;
        private set
        {
            SetProperty(ref field, value);
            OnPropertyChanged(nameof(HasActivity));
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(Percentage));
            OnPropertyChanged(nameof(IsIndeterminate));
        }
    }

    public bool HasActivity => PrimaryOperation is not null;

    public string DisplayText
    {
        get
        {
            if (PrimaryOperation is null) return "";

            string statusMessage = PrimaryOperation.Tracker.StatusMessage;
            return string.IsNullOrEmpty(statusMessage)
                ? PrimaryOperation.Title
                : $"{PrimaryOperation.Title} — {statusMessage}";
        }
    }

    public double Percentage => PrimaryOperation?.Tracker.Percentage ?? 0;

    public bool IsIndeterminate => PrimaryOperation?.Tracker.IsIndeterminate ?? false;

    public RelayCommand CancelPrimaryCommand => field ??= new(CancelPrimary);
    private void CancelPrimary() => PrimaryOperation?.Cancel();

    public RelayCommand CancelAllCommand => field ??= new(() => _progressManager.CancelAll());

    private void OnTrackingChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(RefreshTrackers);

    private void OnTrackerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ProgressTracker.Fraction) or nameof(ProgressTracker.StatusMessage)
            or nameof(ProgressTracker.Percentage) or nameof(ProgressTracker.IsIndeterminate))) return;
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(Percentage));
            OnPropertyChanged(nameof(IsIndeterminate));
        });
    }

    private void RefreshTrackers()
    {
        List<TrackedOperation> freshSnapshot = _progressManager.GetSnapshot().ToList();

        for (int operationIndex = TrackedOperations.Count - 1; operationIndex >= 0; operationIndex--)
        {
            TrackedOperation knownOperation = TrackedOperations[operationIndex];
            if (freshSnapshot.Contains(knownOperation)) continue;
            knownOperation.Tracker.PropertyChanged -= OnTrackerPropertyChanged;
            TrackedOperations.RemoveAt(operationIndex);
        }

        foreach (TrackedOperation freshOperation in freshSnapshot)
        {
            if (TrackedOperations.Contains(freshOperation)) continue;
            freshOperation.Tracker.PropertyChanged += OnTrackerPropertyChanged;
            TrackedOperations.Add(freshOperation);
        }

        PrimaryOperation = TrackedOperations.LastOrDefault();
        OnPropertyChanged(nameof(HasActivity));
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(Percentage));
        OnPropertyChanged(nameof(IsIndeterminate));
    }

    public void Dispose()
    {
        _progressManager.TrackingChanged -= OnTrackingChanged;
        foreach (TrackedOperation operation in TrackedOperations)
        {
            operation.Tracker.PropertyChanged -= OnTrackerPropertyChanged;
        }
        GC.SuppressFinalize(this);
    }
}
