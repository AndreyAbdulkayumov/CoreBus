﻿using ReactiveUI;
using System.Reactive;

namespace ViewModels.ModbusClient.WriteFields.DataItems
{
    public class MultipleCoils_Item : ReactiveObject
    {
        private string? _startAddressAddition;

        public string? StartAddressAddition
        {
            get => _startAddressAddition;
            set
            {
                this.RaiseAndSetIfChanged(ref _startAddressAddition, value);
            }
        }

        private bool _logic_Zero;

        public bool Logic_Zero
        {
            get => _logic_Zero;
            set => this.RaiseAndSetIfChanged(ref _logic_Zero, value);
        }

        private bool _logic_One;

        public bool Logic_One
        {
            get => _logic_One;
            set => this.RaiseAndSetIfChanged(ref _logic_One, value);
        }

        public ReactiveCommand<Unit, Unit>? Command_RemoveItem { get; set; }

        public readonly Guid Id;


        public MultipleCoils_Item(int startAddressAddition, Action<Guid> removeItemHandler)
        {
            Id = Guid.NewGuid();

            StartAddressAddition = $"+{startAddressAddition}";

            Command_RemoveItem = ReactiveCommand.Create(() =>
            {
                removeItemHandler?.Invoke(Id);
            });

            Logic_Zero = true;
        }
    }
}
