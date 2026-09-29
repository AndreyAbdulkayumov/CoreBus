using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.AxisLimitManagers;
using ScottPlot.Plottables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ViewModels.Chart;
using ViewModels.Chart.DataTypes;

namespace CoreBus.Base.Views.Chart;

public partial class ChartWindow : Window
{
    private const int ChartRefreshIntervalMs = 40;

    public static ChartWindow? Instance { get; private set; }
    public static Grid? Workspace { get; private set; }

    private readonly Border _resizeIcon;
    private readonly AvaPlot _chart;
    private readonly Dictionary<Guid, DataLogger> _loggers;
    private readonly DispatcherTimer _refreshTimer;
    private readonly ConcurrentQueue<ChartValue> _pendingPoints;

    private uint _incrementX;

    private Chart_VM? _viewModel;


    public ChartWindow()
    {
        InitializeComponent();

        // Чтобы убрать белую рамку вокруг окна на Linux (проблема появилась с миграцией на Avalonia 12.1)
        if (OperatingSystem.IsLinux())
        {
            WindowDecorations = WindowDecorations.None;
        }

        Instance = this;
        Workspace = this.FindControl<Grid>("Grid_Workspace") ?? throw new ArgumentNullException(nameof(Workspace));

        _resizeIcon = this.FindControl<Border>("Border_ResizeIcon") ?? throw new ArgumentNullException(nameof(_resizeIcon));
        _chart = this.FindControl<AvaPlot>("Chart") ?? throw new ArgumentNullException(nameof(_chart));

        _loggers = new Dictionary<Guid, DataLogger>();
        _pendingPoints = new ConcurrentQueue<ChartValue>();
        
        // Настройка графика

        Chart_VM.InitAxes += Chart_VM_InitAxis;
        Chart_VM.AddPointOnChart += Chart_VM_AddPointOnChart;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(ChartRefreshIntervalMs)
        };
        
        _refreshTimer.Tick += RefreshTimer_Tick;
        _refreshTimer.Start();

        _chart.Plot.Axes.SetLimits(0, 5, 0, 7);

        _chart.Refresh();
    }

    public void UnsubscribeFromEvents()
    {
        Chart_VM.InitAxes -= Chart_VM_InitAxis;
        Chart_VM.AddPointOnChart -= Chart_VM_AddPointOnChart;

        _refreshTimer.Stop();
        _refreshTimer.Tick -= RefreshTimer_Tick;
    }

    private void Chart_VM_InitAxis(object? sender, InitAxesEventArgs e)
    {
        ClearPendingPoints();

        _chart.Plot.Clear();
        _loggers.Clear();

        var numberOfVisiblePoints = _viewModel?.NumberOfVisiblePoints ?? 10;

        var axesXWidth = numberOfVisiblePoints * e.IncrementX;

        foreach (var axis in e.Axes)
        {
            var logger = CreateDataLogger(axis, axesXWidth);

            _loggers.Add(axis.Id, logger);
        }

        _chart.Plot.Axes.Bottom.Label.Text = LocalizationProvider.Get("Chart.TimeAxisMs");
        _chart.Plot.Axes.Bottom.Label.Bold = false;

        _incrementX = e.IncrementX;

        _chart.Plot.Axes.SetLimits(0, 1000, -100, 100);

        _chart.Refresh();
    }

    private DataLogger CreateDataLogger(ChartAxis axisSettings, double axesXWidth)
    {
        var logger = _chart.Plot.Add.DataLogger();

        logger.LegendText = axisSettings.Name;

        logger.MarkerStyle.IsVisible = true;
        logger.MarkerShape = MarkerShape.FilledCircle;
        logger.MarkerStyle.Size = 6;

        logger.AxisManager = new Slide()
        {
            Width = axesXWidth,
            PaddingFractionX = 0.05
        };

        return logger;
    }

    private void Chart_VM_AddPointOnChart(object? sender, ChartValue e)
    {
        _pendingPoints.Enqueue(e);
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        var hasNewPoints = false;

        while (_pendingPoints.TryDequeue(out var point))
        {
            if (!_loggers.TryGetValue(point.AxisId, out var logger))
                continue;

            var xCoordinate = logger.Data.Coordinates.Count == 0
                ? 0
                : logger.Data.Coordinates[^1].X + _incrementX;

            logger.Add(xCoordinate, point.Value);
            hasNewPoints = true;
        }

        if (!hasNewPoints)
            return;

        _chart.Refresh();
    }

    private void ClearPendingPoints()
    {
        while (_pendingPoints.TryDequeue(out _)) ;
    }

    private void Window_DataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as Chart_VM ?? throw new Exception(LocalizationProvider.Get("Chart.InvalidViewModel"));
    }

    private void Chrome_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void Button_Minimize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Button_Maximize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        _resizeIcon.IsVisible = WindowState == WindowState.Normal;
    }

    private void Button_Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ResizeIcon_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Cursor = new(StandardCursorType.BottomRightCorner);
        BeginResizeDrag(WindowEdge.SouthEast, e);
        Cursor = new(StandardCursorType.Arrow);
    }
}
