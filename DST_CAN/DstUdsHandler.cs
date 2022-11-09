namespace PCAN_UDS_TEST.DST_CAN
{
    struct DstCanMessage
    {
        public byte Size;
        public byte Type;
        public uint Address;
        public List<byte> Data;
    }

    struct DstUdsMessage
    {
        public uint Address;
        public byte Size;
        public byte SID;
        public List<byte> Data;
    }
    internal class DstUdsHandler
    {
        internal uint sourceAddress { get; private set; } //32-bit address that is used to send uds packets
        internal uint destinationAddress { get; private set; } //32-bit address that is used to receive uds packets
        private readonly DstCanHandler canHandler;
        internal readonly TimeSpan MaxWait;
        private AutoResetEvent _consequentFramesAckFlag;
        private DstUdsMessage udsMessage;

        public delegate void UdsReceiveHandler(DstUdsMessage message);
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

        private void ParseConsequentFramesAck(DstCanMessage canMessage)
        {
            if (canMessage.Type != 0x00 && canMessage.Address == destinationAddress && canMessage.Data[0] == 0x30)
            {
                canHandler.CanMessageReceived -= ParseConsequentFramesAck;
                canHandler.CanMessageReceived += ParseUdsMessage;
                _consequentFramesAckFlag?.Set();
            }
        }

        private void ParseUdsMessage(DstCanMessage canMessage)
        {
            if (canMessage.Type != 0x00 && canMessage.Address == destinationAddress)
            {
                if (udsMessage.Size == 0)
                {
                    int i = 0;
                    if (canMessage.Data[i] < 0x08)
                    {
                        udsMessage.Address = canMessage.Address;
                        udsMessage.Size = canMessage.Data[i++];
                        udsMessage.SID = canMessage.Data[i++];
                        for (; i < canMessage.Size; i++) udsMessage.Data.Add(canMessage.Data[i]);
                        _udsMessageReceived?.Invoke(udsMessage);
                        udsMessage = new() { Data = new(), SID = 0, Size = 0, Address = 0 };
                    }
                    else
                    {
                        i++; //skip counter 0x10
                        udsMessage.Address = canMessage.Address;
                        udsMessage.Size = canMessage.Data[i++];
                        udsMessage.SID = canMessage.Data[i++];
                        for (; i < canMessage.Size; i++) udsMessage.Data.Add(canMessage.Data[i]);
                        canHandler.SendCanMessage(new DstCanMessage() { Address = sourceAddress, Data = new List<byte>() { 0x30, 0x0A, 0x0A } });
                    }
                }
                else
                {
                    for (int i = 1; i < canMessage.Size; i++) udsMessage.Data.Add(canMessage.Data[i]);
                    if (udsMessage.Size - 1 == udsMessage.Data.Count)
                    {
                        _udsMessageReceived?.Invoke(udsMessage);
                        udsMessage = new() { Data = new(), SID = 0, Size = 0, Address = 0 };
                    }
                }
            }
        }

        public DstUdsHandler(string portName, uint sourceAddress, uint destinationAddress, int timeout)
        {
            MaxWait = TimeSpan.FromMilliseconds(timeout);
            udsMessage = new() { Data = new(), SID = 0, Size = 0 };
            this.sourceAddress = sourceAddress;
            this.destinationAddress = destinationAddress;
            _consequentFramesAckFlag = new AutoResetEvent(false);
            canHandler = new(portName);
            canHandler.CanMessageReceived += ParseUdsMessage;
        }

        public bool SendUdsMessage(DstUdsMessage udsMessage)
        {
            try
            {
                if (udsMessage.Size <= 7)
                {
                    List<byte> buffer = new() { udsMessage.Size, udsMessage.SID };
                    buffer.AddRange(udsMessage.Data);
                    if (!canHandler.SendCanMessage(new DstCanMessage() { Data = buffer, Address = udsMessage.Address })) return false;
                }
                else
                {
                    byte counter = 0x10;
                    List<byte> tempByteList = udsMessage.Data;
                    List<byte> buffer = new() { counter, udsMessage.Size, udsMessage.SID };
                    counter += 0x11;
                    buffer.AddRange(tempByteList.Take(5));
                    tempByteList.RemoveRange(0, 5);
                    if (!canHandler.SendCanMessage(new DstCanMessage() { Data = buffer, Address = udsMessage.Address })) return false;
                    canHandler.CanMessageReceived -= ParseUdsMessage;
                    canHandler.CanMessageReceived += ParseConsequentFramesAck;
                    bool? response = _consequentFramesAckFlag?.WaitOne(MaxWait);
                    if (response == null || response == false)
                    {
                        canHandler.CanMessageReceived -= ParseConsequentFramesAck;
                        canHandler.CanMessageReceived += ParseUdsMessage;
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
                        if (!canHandler.SendCanMessage(new DstCanMessage() { Data = buffer, Address = udsMessage.Address })) return false;
                    }
                }
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
