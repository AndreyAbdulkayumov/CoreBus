﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models.Settings
{
    public class SerialPort_Info
    {
        public string? COMPort { get; set; }
        public string? BaudRate { get; set; }
        public bool BaudRate_IsCustom { get; set; }
        public string? BaudRate_Custom { get; set; }
        public string? Parity { get; set; }
        public string? DataBits { get; set; }
        public string? StopBits { get; set; }
    }

    public class IP_Info
    {
        public string? IP_Address { get; set; }
        public string? Port { get; set; }
    }

    public class DeviceData : ICloneable
    {
        public const string ConnectionName_SerialPort = "SerialPort";
        public const string ConnectionName_Ethernet = "Ethernet";

        public string? GlobalEncoding { get; set; }

        public string? TimeoutWrite { get; set; }
        public string? TimeoutRead { get; set; }

        public string? TypeOfConnection { get; set; }

        public SerialPort_Info? Connection_SerialPort { get; set; }
        public IP_Info? Connection_IP { get; set; }

        public static DeviceData GetDefault()
        {
            return new DeviceData()
            {
                GlobalEncoding = "UTF-8",

                TimeoutWrite = "300",
                TimeoutRead = "300",

                TypeOfConnection = DeviceData.ConnectionName_SerialPort,

                Connection_SerialPort = null,
                Connection_IP = null
            };
        }

        public object Clone()
        {
            SerialPort_Info? SerialPort;

            if (Connection_SerialPort != null)
            {
                SerialPort = new SerialPort_Info()
                {
                    COMPort = Connection_SerialPort.COMPort,
                    BaudRate = Connection_SerialPort.BaudRate,
                    BaudRate_IsCustom = Connection_SerialPort.BaudRate_IsCustom,
                    BaudRate_Custom = Connection_SerialPort.BaudRate_Custom,
                    Parity = Connection_SerialPort.Parity,
                    DataBits = Connection_SerialPort.DataBits,
                    StopBits = Connection_SerialPort.StopBits
                };
            }

            else
            {
                SerialPort = null;
            }

            IP_Info? IP;

            if (Connection_IP != null)
            {
                IP = new IP_Info()
                {
                    IP_Address = Connection_IP.IP_Address,
                    Port = Connection_IP.Port
                };
            }

            else
            {
                IP = null;
            }

            return new DeviceData()
            {
                GlobalEncoding = GlobalEncoding,

                TimeoutWrite = TimeoutWrite,
                TimeoutRead = TimeoutRead,

                TypeOfConnection = TypeOfConnection,

                Connection_SerialPort = SerialPort,

                Connection_IP = IP
            };
        }
    }
}
