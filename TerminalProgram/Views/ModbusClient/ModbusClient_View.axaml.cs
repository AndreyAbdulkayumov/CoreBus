using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ViewModels.ModbusClient;
using ViewModels.ModbusClient.DataTypes;

namespace TerminalProgram.Views.ModbusClient
{
    public class ModbusDataDisplayedForTable
    {
        public ushort OperationID { get; set; }
        public string? FuncNumber { get; set; }
        public string? ViewAddress { get; set; }
        public string? ViewData { get; set; }
        public SolidColorBrush? RowBackground { get; set; }
    }

    public partial class ModbusClient_View : UserControl
    {
        private const string ResourceKey_DataGrid_Color_RowBackground = "DataGrid_Color_RowBackground";
        private const string ResourceKey_DataGrid_Color_AlternatingRowBackground = "DataGrid_Color_AlternatingRowBackground";

        private const string ResourceKey_DataGrid_Color_RowBackground_Selected = "DataGrid_Color_RowBackground_Selected";
        private const string ResourceKey_DataGrid_Color_RowBorderBrush_Selected = "DataGrid_Color_RowBorderBrush_Selected";

        private ObservableCollection<ModbusDataDisplayedForTable> _dataInDataGrid = new ObservableCollection<ModbusDataDisplayedForTable>();

        private readonly ItemsControl? _itemsControl = new ItemsControl();

        private Border? _selectedBorder;
        private SolidColorBrush? _selectedBorder_InitColor;

        public ModbusClient_View()
        {
            InitializeComponent();

            _itemsControl = this.FindControl<ItemsControl>("ItemsControl_ModbusData");

            if (_itemsControl != null)
            {
                _itemsControl.ItemsSource = _dataInDataGrid;
            }

            ModbusClient_VM.AddDataOnTable += ViewModel_ModbusClient_AddDataOnTable;

            if (Application.Current != null)
            {
                Application.Current.ActualThemeVariantChanged += Current_ActualThemeVariantChanged;
            }            
        }

        private void Current_ActualThemeVariantChanged(object? sender, EventArgs e)
        {
            if (_itemsControl == null || _dataInDataGrid.Count == 0)
            {
                return;
            }

            var items = new ObservableCollection<ModbusDataDisplayedForTable>();
            items.AddRange(_dataInDataGrid);

            string resourceKey;

            for (int i = 0;  i < items.Count; i++)
            {
                resourceKey = i % 2 != 0 ? ResourceKey_DataGrid_Color_AlternatingRowBackground : ResourceKey_DataGrid_Color_RowBackground;

                if (this.TryFindResource(resourceKey, out object? rowBackground) == false)
                {
                    rowBackground = Brushes.Transparent;
                }

                items[i].RowBackground = rowBackground as SolidColorBrush;
            }

            _dataInDataGrid.Clear();
            _dataInDataGrid.AddRange(items);
        }

        private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            Border? border = sender as Border;

            if (border != null)
            {
                // ����� ��������� � ������� ������
                if (border == _selectedBorder)
                {
                    border.Background = _selectedBorder_InitColor;
                    border.BorderBrush = _selectedBorder_InitColor;

                    _selectedBorder = null;

                    return;
                }

                // ����� ��������� � ������� ��������� ������
                if (_selectedBorder != null)
                {
                    _selectedBorder.Background = _selectedBorder_InitColor;
                    _selectedBorder.BorderBrush = _selectedBorder_InitColor;
                }

                // ��������� � �������� ��������� ������
                _selectedBorder = border;
                _selectedBorder_InitColor = (SolidColorBrush?)border.Background;

                if (this.TryFindResource(ResourceKey_DataGrid_Color_RowBackground_Selected, out object? rowBackground))
                {
                    border.Background = (IBrush?)rowBackground;
                }

                if (this.TryFindResource(ResourceKey_DataGrid_Color_RowBorderBrush_Selected, out object? rowBorderBrush))
                {
                    border.BorderBrush = (IBrush?)rowBorderBrush;
                }                
            }
        }

        private async void ViewModel_ModbusClient_AddDataOnTable(object? sender, ModbusDataDisplayed? e)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (e != null)
                {
                    string resourceKey = _dataInDataGrid.Count % 2 != 0 ? ResourceKey_DataGrid_Color_AlternatingRowBackground : ResourceKey_DataGrid_Color_RowBackground;

                    if (this.TryFindResource(resourceKey, out object? rowBackground) == false)
                    {
                        rowBackground = Brushes.Transparent;
                    }

                    _dataInDataGrid.Add(new ModbusDataDisplayedForTable()
                    {
                        OperationID = e.OperationID,
                        FuncNumber = e.FuncNumber,
                        ViewAddress = e.ViewAddress,
                        ViewData = e.ViewData,
                        RowBackground = rowBackground as SolidColorBrush
                    });

                    await ScrollToEnd();

                    return;
                }

                _dataInDataGrid.Clear();
            });            
        }

        public async Task ScrollToEnd()
        {
            var listView = this.FindControl<ScrollViewer>("ScrollViewer_ModbusData");

            if (listView != null)
            {
                await Task.Delay(10);  // ������� �������������� �� �����. ����� ������� ���������, ����� ����� ����� ��������� � ���������� �������� 

                listView.ScrollToEnd();
            }
        }
    }
}
