﻿namespace CAN_COM
{
    internal class DstCanComUdsHandler
    {
        private readonly uint sourceAddress; //32-bit address that is used to send uds packets
        private readonly uint destinationAddress; //32-bit address that is used to receive uds packets
        private readonly DstCanComCanHandler canHandler;
        private readonly TimeSpan MaxWait = TimeSpan.FromMilliseconds(10000);
        private AutoResetEvent _consequentFramesAckFlag;
        private UdsMessage udsMessage;

        public delegate void UdsReceiveHandler(UdsMessage message);
        private UdsReceiveHandler? _udsMessageReceived;
        public event UdsReceiveHandler UdsMessageReceived
        {
            add
            {
                _udsMessageReceived += value;
                Console.WriteLine($"{value.Method.Name} subscribed to udsMessageHandler");
            }
            remove
            {
                _udsMessageReceived -= value;
                Console.WriteLine($"{value.Method.Name} unsubscribed from udsMessageHandler");
            }
        }

        private void ParseConsequentFramesAck(CanMessage canMessage)
        {
            if (canMessage.Type != 0x00 && canMessage.Address == destinationAddress && canMessage.Data[0] == 0x30)
            {
                canHandler.CanMessageReceived -= ParseConsequentFramesAck;
                _consequentFramesAckFlag?.Set();
            }
        }

        private void ParseUdsMessage(CanMessage canMessage)
        {
            if (canMessage.Type != 0x00 && canMessage.Address == destinationAddress)
            {
                if (udsMessage.Size == 0)
                {
                    int i = 0;
                    if (canMessage.Data[i++] < 0x08)
                    {
                        udsMessage.SID = canMessage.Data[i++];
                        for (; i < canMessage.Size; i++) udsMessage.Data.Add(canMessage.Data[i]);
                    }
                    else
                    {
                        udsMessage.Size = canMessage.Data[i++];
                        udsMessage.SID = canMessage.Data[i++];
                        for (; i < canMessage.Size; i++) udsMessage.Data.Add(canMessage.Data[i]);
                        canHandler.SendCanMessage(new CanMessage() { Address = sourceAddress, Data = new List<byte>() { 0x30, 0x0A, 0x0A } });
                    }
                }
                else
                {
                    for (int i = 1; i < canMessage.Size; i++) udsMessage.Data.Add(canMessage.Data[i]);
                    if (udsMessage.Size == udsMessage.Data.Count)
                    {
                        _udsMessageReceived?.Invoke(udsMessage);
                        udsMessage = new() { Data = new(), SID = 0, Size = 0 };
                    }
                }
            }
        }

        public DstCanComUdsHandler(string portName, uint sourceAddress, uint destinationAddress)
        {
            udsMessage = new() { Data = new(), SID = 0, Size = 0 };
            this.sourceAddress = sourceAddress;
            this.destinationAddress = destinationAddress;
            _consequentFramesAckFlag = new AutoResetEvent(false);
            canHandler = new(portName);
            
        }

        public void Initialize()
        {
            canHandler.CanMessageReceived += ParseUdsMessage;
            canHandler.Initialize();
        }

        public void Uninitialize()
        {
            canHandler.CanMessageReceived -= ParseUdsMessage;
            canHandler.Uninitialize();
        }

        public bool SendUdsMessage(UdsMessage udsMessage)
        {
            try
            {
                if (udsMessage.Size > 7)
                {
                    byte counter = 0x10;
                    List<byte> tempByteList = udsMessage.Data;
                    List<byte> buffer = new() { counter, udsMessage.Size };
                    counter += 0x11;
                    buffer.AddRange(tempByteList.Take(6));
                    tempByteList.RemoveRange(0, 6);
                    if (!canHandler.SendCanMessage(new CanMessage() { Data = buffer, Address = sourceAddress })) return false;
                    canHandler.CanMessageReceived += ParseConsequentFramesAck;
                    bool? response = _consequentFramesAckFlag?.WaitOne(MaxWait);
                    if (response == null || response == false)
                    {
                        canHandler.CanMessageReceived -= ParseConsequentFramesAck;
                        return false;
                    }
                    while (tempByteList.Count > 0)
                    {
                        buffer.Clear();
                        buffer.Add(counter++);
                        if (tempByteList.Count >= 7)
                        {
                            buffer.AddRange(tempByteList.Take(7));
                            tempByteList.RemoveRange(0, 7);
                        }
                        else
                        {
                            buffer.AddRange(tempByteList);
                            tempByteList.Clear();
                        }
                        if (!canHandler.SendCanMessage(new CanMessage() { Data = buffer, Address = sourceAddress })) return false;
                    }
                }
                else
                {
                    List<byte> buffer = new() { udsMessage.Size };
                    buffer.AddRange(udsMessage.Data);
                    if (!canHandler.SendCanMessage(new CanMessage() { Data = buffer, Address = sourceAddress })) return false;
                }
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
