using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Reactive;

namespace ViewModels.ModbusClient.Monitoring
{
    public class MonitoringItem_VM : ReactiveObject
    {
        public ReactiveCommand<Unit, Unit> Command_RemoveItem { get; }

        public MonitoringItem_VM(IMessageBoxMainWindow messageBox)
        {
            Command_RemoveItem = ReactiveCommand.Create(() =>
            {

            });
            Command_RemoveItem.ThrownExceptions.Subscribe(error => messageBox.Show($"Ошибка удаления регистра.\n\n{error.Message}", MessageType.Error, error));
        }
    }
}
