namespace PCAN_UDS_TEST.DST_CAN
{
    internal class DstCanHandler
    {
        private DstComHandler comHandler;
        public delegate void CanReceiveHandler(DstCanMessage message);
        private CanReceiveHandler? _canMessageReceived;
        public event CanReceiveHandler CanMessageReceived
        {
            add
            {
                _canMessageReceived += value;
                //Console.WriteLine($"{value.Method.Name} subscribed to canMessageHandler");
            }
            remove
            {
                _canMessageReceived -= value;
                //Console.WriteLine($"{value.Method.Name} unsubscribed from canMessageHandler");
            }
        }

        public DstCanHandler(string portName)
        {
            comHandler = new(portName);
            comHandler.ComMessageReceived += ParseCanMessage;
            comHandler.Initialize();
        }

        public bool SendCanMessage(DstCanMessage canMessage)
        {
            try
            {
                List<byte> tempBuffer = new() { (byte)(canMessage.Address >> 24), (byte)((canMessage.Address >> 16) & 0xFF), (byte)((canMessage.Address >> 8) & 0xFF), (byte)(canMessage.Address & 0xFF) };
                tempBuffer.AddRange(canMessage.Data);
                if (!comHandler.SendComMessage(tempBuffer)) return false;
                else return true;
            }
            catch { return false; }
        }

        private void ParseCanMessage(List<byte> comMessage)
        {
            byte i = 1;
            DstCanMessage canMessage = new()
            {
                Size = (byte)(comMessage[i] & 0x0F),
                Type = (byte)((comMessage[i++] & 0xF0) >> 4),
                Address = (uint)((comMessage[i++] << 24) + (comMessage[i++] << 16) + (comMessage[i++] << 8) + comMessage[i++])
            };
            canMessage.Data = comMessage.Skip(i).Take(canMessage.Size).ToList();
            _canMessageReceived?.Invoke(canMessage);
        }
    }
}
