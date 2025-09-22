using Core.Clients.DataTypes;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;

namespace Core.Models.Modbus;

public class Model_Modbus
{
    public double CycleMode_Period
    {
        get => _cycleModeTimer.Interval;
        set => _cycleModeTimer.Interval = value;
    }

    public event EventHandler<Exception>? Model_ErrorInCycleMode;

    private static bool _isBusy = false;

    private IConnection? _device;

    private readonly System.Timers.Timer _cycleModeTimer;
    private const double IntervalDefault = 100;

    private Action? _readRegisterInCycleMode;

    public Model_Modbus()
    {
        _cycleModeTimer = new System.Timers.Timer(IntervalDefault);
        _cycleModeTimer.Elapsed += CycleModeTimer_Elapsed;
    }

    public void Host_DeviceIsConnect(object? sender, IConnection? e)
    {
        if (e != null && e.IsConnected)
        {
            _device = e;
        }
    }

    public void Host_DeviceIsDisconnected(object? sender, IConnection? e)
    {
        _device = null;
    }

    public async Task<ModbusOperationResult> WriteRegister(ModbusWriteFunction writeFunction, MessageData dataForWrite, ModbusMessage message)
    {
        while (_isBusy)
        {
            await Task.Delay(1); // Асинхронная задержка, чтобы создать асинхронное ожидание.
        }

        _isBusy = true;

        var TX = Array.Empty<byte>();
        var RX = Array.Empty<byte>();

        var result = new ModbusOperationResult();

        ModbusOperationInfo? TX_Info = null, RX_Info = null;

        try
        {
            if (_device == null)
            {
                throw new Exception("Хост не инициализирован.");
            }

            TX = message.CreateMessage(writeFunction, dataForWrite);

            TX_Info = await _device.Send(TX, TX.Length);

            RX_Info = await _device.Receive();

            if (RX_Info.ResponseBytes != null && RX_Info.ResponseBytes.Length > 0)
            {
                RX = RX_Info.ResponseBytes;

                ModbusResponse Data = message.DecodingMessage(writeFunction, RX);
            }

            else
            {
                throw new TimeoutException();
            }
        }

        catch (ModbusException error)
        {
            throw new ModbusException(
                errorObject: error,
                requestBytes: TX.Length > 0 ? TX : Array.Empty<byte>(),
                responseBytes: GetOutputRX(RX, RX.Length),
                request_ExecutionTime: TX_Info != null ? TX_Info.ExecutionTime : new DateTime(),
                response_ExecutionTime: RX_Info != null ? RX_Info.ExecutionTime : new DateTime()
                );
        }

        catch (TimeoutException)
        {
            string errorMessage = "Хост не ответил.";

            if (_device != null)
            {
                errorMessage += "\n\nТаймаут записи: " + _device.WriteTimeout + " мс." + "\n" +
                    "Таймаут чтения: " + _device.ReadTimeout + " мс.";
            }

            throw new TimeoutException(errorMessage);
        }

        catch (Exception error)
        {
            throw new Exception(error.Message,
                new ModbusExceptionInfo()
                {
                    Details = new ModbusActionDetails()
                    {
                        RequestBytes = TX.Length > 0 ? TX : Array.Empty<byte>(),
                        ResponseBytes = GetOutputRX(RX, RX.Length),

                        Request_ExecutionTime = TX_Info != null ? TX_Info.ExecutionTime : new DateTime(),
                        Response_ExecutionTime = RX_Info != null ? RX_Info.ExecutionTime : new DateTime()
                    }
                });
        }

        finally
        {
            result.Details = new ModbusActionDetails()
            {
                RequestBytes = TX.Length > 0 ? TX : Array.Empty<byte>(),
                ResponseBytes = GetOutputRX(RX, RX.Length),

                Request_ExecutionTime = TX_Info != null ? TX_Info.ExecutionTime : new DateTime(),
                Response_ExecutionTime = RX_Info != null ? RX_Info.ExecutionTime : new DateTime()
            };

            _isBusy = false;
        }

        return result;
    }

    private byte[] GetOutputRX(byte[] RX, int length)
    {
        var outputArray = new byte[length];

        Array.Copy(RX, 0, outputArray, 0, outputArray.Length);

        return outputArray;
    }

    public async Task<ModbusOperationResult> ReadRegister(ModbusReadFunction readFunction, MessageData dataForRead, ModbusMessage message)
    {
        while (_isBusy)
        {
            await Task.Delay(1); // Асинхронная задержка, чтобы создать асинхронное ожидание.
        }

        _isBusy = true;

        var TX = Array.Empty<byte>();
        var RX = Array.Empty<byte>();

        var result = new ModbusOperationResult();

        ModbusOperationInfo? TX_Info = null, RX_Info = null;

        try
        {
            if (_device == null)
            {
                throw new Exception("Хост не инициализирован.");
            }

            TX = message.CreateMessage(readFunction, dataForRead);

            TX_Info = await _device.Send(TX, TX.Length);

            RX_Info = await _device.Receive();

            if (RX_Info.ResponseBytes != null && RX_Info.ResponseBytes.Length > 0)
            {
                RX = RX_Info.ResponseBytes;

                ModbusResponse DeviceResponse = message.DecodingMessage(readFunction, RX);

                result.ReadedData = DeviceResponse.Data;
            }

            else
            {
                throw new TimeoutException();
            }
        }

        catch (ModbusException error)
        {
            throw new ModbusException(
                errorObject: error,
                requestBytes: TX.Length > 0 ? TX : Array.Empty<byte>(),
                responseBytes: GetOutputRX(RX, RX.Length),
                request_ExecutionTime: TX_Info != null ? TX_Info.ExecutionTime : new DateTime(),
                response_ExecutionTime: RX_Info != null ? RX_Info.ExecutionTime : new DateTime()
                );
        }

        catch (TimeoutException)
        {
            string errorMessage = "Хост не ответил.";

            if (_device != null)
            {
                errorMessage += "\n\nТаймаут записи: " + _device.WriteTimeout + " мс." + "\n" +
                    "Таймаут чтения: " + _device.ReadTimeout + " мс.";
            }

            throw new TimeoutException(errorMessage);
        }

        catch (Exception error)
        {
            throw new Exception(error.Message,
                new ModbusExceptionInfo()
                {
                    Details = new ModbusActionDetails()
                    {
                        RequestBytes = TX.Length > 0 ? TX : Array.Empty<byte>(),
                        ResponseBytes = GetOutputRX(RX, RX.Length),

                        Request_ExecutionTime = TX_Info != null ? TX_Info.ExecutionTime : new DateTime(),
                        Response_ExecutionTime = RX_Info != null ? RX_Info.ExecutionTime : new DateTime()
                    }
                });
        }

        finally
        {
            result.Details = new ModbusActionDetails()
            {
                RequestBytes = TX.Length > 0 ? TX : Array.Empty<byte>(),
                ResponseBytes = GetOutputRX(RX, RX.Length),

                Request_ExecutionTime = TX_Info != null ? TX_Info.ExecutionTime : new DateTime(),
                Response_ExecutionTime = RX_Info != null ? RX_Info.ExecutionTime : new DateTime()
            };

            _isBusy = false;
        }

        return result;
    }

    public void CycleMode_Start(Action readRegister_Handler)
    {
        _readRegisterInCycleMode = readRegister_Handler;

        _readRegisterInCycleMode.Invoke();

        _cycleModeTimer.Start();
    }

    public void CycleMode_Stop()
    {
        _cycleModeTimer.Stop();
    }

    private void CycleModeTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            if (_readRegisterInCycleMode != null)
            {
                _readRegisterInCycleMode.Invoke();
            }
        }

        catch (Exception error)
        {
            CycleMode_Stop();

            Model_ErrorInCycleMode?.Invoke(this, error);
        }
    }
}
