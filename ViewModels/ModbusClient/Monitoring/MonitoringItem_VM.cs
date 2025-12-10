using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Reactive;

namespace ViewModels.ModbusClient.Monitoring
{
    public class MonitoringItem_VM : ReactiveObject
    {
        private string _address;

        public string Address
        {
            get => _address;
            set => this.RaiseAndSetIfChanged(ref _address, value);
        }

        private string _alias;

        public string Alias
        {
            get => _alias;
            set => this.RaiseAndSetIfChanged(ref _alias, value);
        }

        private string _value;

        public string Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }

        private string _typedValue;

        public string TypedValue
        {
            get => _typedValue;
            set => this.RaiseAndSetIfChanged(ref _typedValue, value);
        }

        private ObservableCollection<string> _allValueTypes = new ObservableCollection<string>()
        {
            "UInt16", "Int16", "UInt32", "Int32", "Float"
        };

        public ObservableCollection<string> AllValueTypes
        {
            get => _allValueTypes;
        }

        private string _selectedValueType;

        public string SelectedValueType
        {
            get => _selectedValueType;
            set => this.RaiseAndSetIfChanged(ref _selectedValueType, value);
        }

        private string _convertedValue;

        public string ConvertedValue
        {
            get => _convertedValue;
            set => this.RaiseAndSetIfChanged(ref _convertedValue, value);
        }

        private bool _onChart;

        public bool OnChart
        {
            get => _onChart;
            set => this.RaiseAndSetIfChanged(ref _onChart, value);
        }

        public ReactiveCommand<Unit, Unit> Command_FormulaChange { get; }

        public ReactiveCommand<Unit, Unit> Command_RemoveItem { get; }


        public MonitoringItem_VM(IMessageBoxMainWindow messageBox)
        {
            Address = "0";
            Value = "0";
            TypedValue = "0";
            SelectedValueType = AllValueTypes.First();
            ConvertedValue = "0.00";

            Command_FormulaChange = ReactiveCommand.Create(() =>
            {

            });
            Command_FormulaChange.ThrownExceptions.Subscribe(error => messageBox.Show($"Ошибка изменения формулы.\n\n{error.Message}", MessageType.Error, error));

            Command_RemoveItem = ReactiveCommand.Create(() =>
            {

            });
            Command_RemoveItem.ThrownExceptions.Subscribe(error => messageBox.Show($"Ошибка удаления регистра.\n\n{error.Message}", MessageType.Error, error));
        }
    }
}
