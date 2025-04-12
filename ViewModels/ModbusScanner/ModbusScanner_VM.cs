﻿using ReactiveUI;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using MessageBox_Core;
using Core.Models;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using ViewModels.Validation;
using Core.Models.Modbus;
using Services.Interfaces;

namespace ViewModels.ModbusScanner
{
    public class ModbusScanner_VM : ValidatedDateInput, IValidationFieldInfo
    {
        private bool _searchInProcess = false;

        public bool SearchInProcess
        {
            get => _searchInProcess;
            set => this.RaiseAndSetIfChanged(ref _searchInProcess, value);
        }

        private string _slavesAddresses = string.Empty;

        public string SlavesAddresses
        {
            get => _slavesAddresses;
            set => this.RaiseAndSetIfChanged(ref _slavesAddresses, value);
        }

        private string _pdu_SearchRequest = string.Empty;

        public string PDU_SearchRequest
        {
            get => _pdu_SearchRequest;
            set => this.RaiseAndSetIfChanged(ref _pdu_SearchRequest, value);
        }

        public string PDU_SearchRequest_Default
        {
            get => "03 00 01 00 02";
        }

        private string? _pauseBetweenRequests;

        public string? PauseBetweenRequests
        {
            get => _pauseBetweenRequests;
            set
            {
                this.RaiseAndSetIfChanged(ref _pauseBetweenRequests, value);
                ValidateInput(nameof(PauseBetweenRequests), value);
            }
        }

        private string _deviceReadTimeout = "Таймаут чтения ";

        public string DeviceReadTimeout
        {
            get => _deviceReadTimeout;
            set => this.RaiseAndSetIfChanged(ref _deviceReadTimeout, value);
        }

        private const string ButtonContent_Start = "Начать поиск";
        private const string ButtonContent_Stop = "Остановить поиск";

        private string _actionButtonContent = ButtonContent_Start;

        public string ActionButtonContent
        {
            get => _actionButtonContent;
            set => this.RaiseAndSetIfChanged(ref _actionButtonContent, value);
        }

        private string _currentSlaveID = string.Empty;

        public string CurrentSlaveID
        {
            get => _currentSlaveID;
            set => this.RaiseAndSetIfChanged(ref _currentSlaveID, value);
        }

        private int _progressBar_Value;

        public int ProgressBar_Value
        {
            get => _progressBar_Value;
            set => this.RaiseAndSetIfChanged(ref _progressBar_Value, value);
        }

        // Минимальное значение адреса устройства Modbus.
        // Не берем в учет широковещательный адрес (SlaveId = 0).
        public int ProgressBar_Minimun
        {
            get => 1;
        }

        // Максимальное значение адреса устройства Modbus.
        public int ProgressBar_Maximun
        {
            get => 255;
        }

        public string ErrorMessageInUI
        {
            get => "Ни одно устройство не ответило.\nВозможно стоит повысить значение паузы между запросами.";
        }

        private bool _errorIsVisible = false;

        public bool ErrorIsVisible
        {
            get => _errorIsVisible;
            set => this.RaiseAndSetIfChanged(ref _errorIsVisible, value);
        }

        public ReactiveCommand<Unit, Unit> Command_Start_Stop_Search { get; }

        private readonly IMessageBox _messageBox;
        private readonly ConnectedHost _connectedHostModel;
        private readonly Model_Modbus _modbusModel;

        private Task? _searchTask;
        private CancellationTokenSource? _searchCancel;

        private uint _pauseBetweenRequests_ForWork;

        public ModbusScanner_VM(IMessageBoxModbusScanner messageBox,
            ConnectedHost connectedHostModel, Model_Modbus modbusModel)
        {
            _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
            _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
            _modbusModel = modbusModel ?? throw new ArgumentNullException(nameof(modbusModel));

            DeviceReadTimeout += _connectedHostModel.Host_ReadTimeout.ToString() + " мс.";

            Command_Start_Stop_Search = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SearchInProcess)
                {
                    await StopPolling();

                    return;
                }

                StartPolling();
            });
            Command_Start_Stop_Search.ThrownExceptions.Subscribe(error => _messageBox.Show(error.Message, MessageType.Error, error));

            // Значения по умолчанию
            PauseBetweenRequests = "100";
        }

        public async Task WindowClosed()
        {
            if (SearchInProcess && _searchTask != null)
            {
                _searchCancel?.Cancel();

                await Task.WhenAny(_searchTask);
            }
        }

        private void StartPolling()
        {
            if (string.IsNullOrEmpty(PauseBetweenRequests))
            {
                _messageBox.Show("Не задано значение паузы.", MessageType.Warning);
                return;
            }

            string? validationMessage = CheckFields();

            if (!string.IsNullOrEmpty(validationMessage))
            {
                _messageBox.Show(validationMessage, MessageType.Warning);
                return;
            }

            ActionButtonContent = ButtonContent_Stop;
            SearchInProcess = true;

            SlavesAddresses = string.Empty;

            ErrorIsVisible = false;

            _searchCancel = new CancellationTokenSource();

            _searchTask = Task.Run(() => SearchDevices(_searchCancel.Token));
        }

        private void ViewSlaveAddress(int slaveId)
        {
            SlavesAddresses +=
                "Slave ID:\n" +
                "dec:   " + slaveId + "\n" +
                "hex:   " + slaveId.ToString("X2") + "\n\n";
        }

        private string? CheckFields()
        {
            if (!HasErrors)
            {
                return null;
            }

            StringBuilder message = new StringBuilder();

            foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
            {
                message.AppendLine($"[{GetFieldViewName(element.Key)}]\n{GetFullErrorMessage(element.Key)}\n");
            }

            if (message.Length > 0)
            {
                message.Insert(0, "Ошибки валидации:\n\n");
                return message.ToString().TrimEnd('\r', '\n');
            }

            return null;
        }

        private async Task StopPolling()
        {
            _searchCancel?.Cancel();

            if (_searchTask != null)
            {
                await Task.WhenAny(_searchTask);
            }

            StopPolling_UI_Actions();
        }

        private void StopPolling_UI_Actions()
        {
            ActionButtonContent = ButtonContent_Start;
            SearchInProcess = false;

            ProgressBar_Value = ProgressBar_Minimun;
        }

        private async Task SearchDevices(CancellationToken taskCancel)
        {
            try
            {
                ushort address = 0;
                int numberOfRegisters = 2;
                ModbusMessage modbusMessageType = new ModbusRTU_Message();
                ushort CRC16_Polynom = 0xA001;


                ModbusReadFunction readFunction = Function.AllReadFunctions.Single(x => x.Number == 3);

                var data = new ReadTypeMessage(
                    0,
                    address,
                    numberOfRegisters,
                    modbusMessageType is ModbusTCP_Message ? false : true,
                    CRC16_Polynom);

                ModbusOperationResult result;

                for (int i = ProgressBar_Minimun; i <= ProgressBar_Maximun; i++)
                {
                    try
                    {
                        data.SlaveID = (byte)i;

                        CurrentSlaveID = i + " (0x" + i.ToString("X2") + ")";

                        result = await _modbusModel.ReadRegister(readFunction, data, modbusMessageType);

                        ViewSlaveAddress(i);
                    }

                    catch (TimeoutException)
                    {
                        continue;
                    }

                    catch (ModbusException)
                    {
                        ViewSlaveAddress(i);
                    }

                    finally
                    {
                        ProgressBar_Value++;

                        await Task.Delay((int)_pauseBetweenRequests_ForWork, taskCancel);
                    }
                }

                if (SlavesAddresses == string.Empty)
                {
                    ErrorIsVisible = true;
                }
            }

            catch (OperationCanceledException)
            {

            }

            catch (Exception error)
            {
                _messageBox.Show(error.Message, MessageType.Error, error);
            }

            finally
            {
                StopPolling_UI_Actions();
            }
        }

        protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (!StringValue.IsValidNumber(value, NumberStyles.Number, out _pauseBetweenRequests_ForWork))
            {
                return AllErrorMessages[DecError_uint];
            }

            return null;
        }

        public string GetFieldViewName(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(PauseBetweenRequests):
                    return "Пауза";

                default:
                    return fieldName;
            }
        }
    }
}
