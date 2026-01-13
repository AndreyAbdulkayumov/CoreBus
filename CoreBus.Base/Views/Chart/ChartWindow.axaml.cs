using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using ViewModels.Chart;
using ViewModels.Chart.DataTypes;

namespace CoreBus.Base.Views.Chart;

public partial class ChartWindow : Window
{
    public static ChartWindow? Instance { get; private set; }
    public static Grid? Workspace { get; private set; }

    private readonly Border _resizeIcon;
    private readonly AvaPlot _chart;
    
    private readonly Dictionary<Guid, DataLogger> _loggers = new Dictionary<Guid, DataLogger>();

    public ChartWindow()
    {
        InitializeComponent();

        Instance = this;

        Workspace = this.FindControl<Grid>("Grid_Workspace") ?? throw new ArgumentNullException(nameof(Workspace));

        _resizeIcon = this.FindControl<Border>("Border_ResizeIcon") ?? throw new ArgumentNullException(nameof(_resizeIcon));
        _chart = this.FindControl<AvaPlot>("Chart") ?? throw new ArgumentNullException(nameof(_chart));

        // Настройка графика

        Chart_VM.InitAxis += Chart_VM_InitAxis;
        Chart_VM.AddPointOnChart += Chart_VM_AddPointOnChart;

        _chart.Plot.Axes.SetLimits(0, 5, 0, 7);

        _chart.Refresh();
    }

    private DataLogger CreateDataLogger(ChartAxis axisSettings)
    {
        var logger = _chart.Plot.Add.DataLogger();

        //logger.AxisManager = new Slide { Width = 20 }; // фокус на последних 20

        logger.LegendText = axisSettings.Name;

        logger.MarkerStyle.IsVisible = true;
        logger.MarkerShape = MarkerShape.FilledCircle;
        logger.MarkerStyle.Size = 6;

        return logger;
    }

    private void Chart_VM_InitAxis(object? sender, IList<ChartAxis> e)
    {
        try
        {
            _chart.Plot.Clear();
            _loggers.Clear();

            foreach (var axis in e)
            {
                var logger = CreateDataLogger(axis);

                _loggers.Add(axis.Id, logger);             
            }
        }
        
        catch (Exception)
        {
            // А как?
        }
    }

    private void Chart_VM_AddPointOnChart(object? sender, ChartPoint e)
    {
        if (_loggers.TryGetValue(e.AxisId, out var logger))
        {
            logger.Add(e.X, e.Y);

            _chart.Refresh();
        }
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