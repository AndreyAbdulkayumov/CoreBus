﻿using ReactiveUI;
using System.Globalization;
using System.Text.RegularExpressions;
using MessageBox_Core;
using Core.Models.Settings;
using ViewModels.Validation;
using Services.Interfaces;

namespace ViewModels.Settings.Tabs
{
    public class Connection_Ethernet_VM : ValidatedDateInput, IValidationFieldInfo
    {
        private string? _ip_address = string.Empty;

        public string? IP_Address
        {
            get => _ip_address;
            set
            {
                this.RaiseAndSetIfChanged(ref _ip_address, value);
                ValidateInput(nameof(IP_Address), value);
            }
        }

        private string? _port = string.Empty;

        public string? Port
        {
            get => _port;
            set
            {
                this.RaiseAndSetIfChanged(ref _port, value);
                ValidateInput(nameof(Port), value);
            }
        }

        private readonly Model_Settings SettingsFile;

        private readonly IMessageBox _messageBox;


        public Connection_Ethernet_VM(IMessageBoxSettings messageBox)
        {
            _messageBox = messageBox;

            SettingsFile = Model_Settings.Model;            
        }

        public string GetFieldViewName(string fieldName)
        {
            switch (fieldName)
            {
                case nameof(IP_Address):
                    return "IP-адрес";

                case nameof(Port):
                    return "Порт";

                default:
                    return fieldName;
            }
        }

        public void SettingsFileChanged()
        {
            try
            {
                if (SettingsFile.Settings == null)
                {
                    throw new Exception("Не инициализирован файл настроек.");
                }

                if (SettingsFile.Settings.Connection_IP == null)
                {
                    IP_Address = null;
                    Port = null;

                    return;
                }

                IP_Address = SettingsFile.Settings.Connection_IP.IP_Address;
                Port = SettingsFile.Settings.Connection_IP.Port;
            }

            catch (Exception error)
            {
                _messageBox.Show("Ошибка обновления значений на странице Ethernet.\n\n" + error.Message, MessageType.Error);
            }
        }

        protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            switch (fieldName)
            {
                case nameof(IP_Address):
                    return Check_IP_Address(value);

                case nameof(Port):
                    return Check_Port(value);
            }            

            return null;
        }

        private ValidateMessage? Check_IP_Address(string value)
        {
            // Регулярное выражение для проверки корректного IPv4 адреса
            string pattern = @"^(25[0-5]|2[0-4][0-9]|[1][0-9]{2}|[1-9]?[0-9])\.(25[0-5]|2[0-4][0-9]|[1][0-9]{2}|[1-9]?[0-9])\.(25[0-5]|2[0-4][0-9]|[1][0-9]{2}|[1-9]?[0-9])\.(25[0-5]|2[0-4][0-9]|[1][0-9]{2}|[1-9]?[0-9])$";

            if (!Regex.IsMatch(value, pattern))
            {
                return AllErrorMessages[IP_Address_Invalid];
            }

            return null;
        }

        private ValidateMessage? Check_Port(string value)
        {
            if (!StringValue.IsValidNumber(value, NumberStyles.Number, out uint _))
            {
                return AllErrorMessages[DecError_uint];
            }

            return null;
        }
    }
}
