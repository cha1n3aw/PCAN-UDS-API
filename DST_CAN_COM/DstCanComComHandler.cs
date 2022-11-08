using System.IO.Ports;

namespace CAN_COM
{
    internal class DstCanComComHandler
    {
        public delegate void ComReceiveHandler(List<byte> comMessage);
        private ComReceiveHandler? _comMessageReceived;
        public event ComReceiveHandler ComMessageReceived
        {
            add
            {
                _comMessageReceived += value;
                Console.WriteLine($"{value.Method.Name} subscribed to comMessageHandler");
            }
            remove
            {
                _comMessageReceived -= value;
                Console.WriteLine($"{value.Method.Name} unsubscribed from comMessageHandler");
            }
        }
        private SerialPort serialPort = new();
        private bool run;
        private string portName;

        public DstCanComComHandler(string portName)
        {
            this.portName = portName;
            run = false;
        }
        private void DebugComReceiveMessage(List<byte> comMessage)
        {
            foreach (byte b in comMessage) Console.Write($"{b:X2} ");
            Console.WriteLine();
        }

        private void ReceiveComMessage()
        {
            while (run)
            {
                if (serialPort.IsOpen && serialPort.BytesToRead > 0)
                {
                    List<byte> comMessage = new();
                    while (serialPort.BytesToRead > 0) comMessage.Add((byte)serialPort.ReadByte());
                    if (comMessage[^1] == CalculateCrc8(comMessage.Skip(1).Take(comMessage.Count - 3).ToArray())) _comMessageReceived?.Invoke(comMessage);
                    Thread.Sleep(1);
                }
            }
        }

        public bool SendComMessage(List<byte> dataToSend)
        {
            dataToSend.Insert(0, 0x24);
            if (dataToSend.Count < 8) dataToSend.Insert(1, (byte)(dataToSend.Count - 1 + 0x10));
            else if (dataToSend.Count == 8) dataToSend.Insert(1, 0x18);
            else return false;
            dataToSend.Add(CalculateCrc8(dataToSend.Skip(1).ToArray()));
            serialPort.Write(dataToSend.ToArray(), 0, dataToSend.Count);
            return true;
        }

        public byte CalculateCrc8(byte[] array)
        {
            byte crc = 0x00;
            byte i, j, b;
            for (j = 0; j < array.Length; j++)
            {
                b = array[j];
                i = 8;
                do
                {
                    if (((b ^ crc) & 0x01) != 0) crc = (byte)(((crc ^ 0x18) >> 1) | 0x80);
                    else crc >>= 1;
                    b >>= 1;
                }
                while (--i > 0);
            }
            return crc;
        }

        public bool Initialize()
        {
            try
            {
                if (SerialPort.GetPortNames().Contains(portName))
                {
                    serialPort = new() { PortName = portName, BaudRate = 115200, Parity = Parity.None, DataBits = 8, StopBits = StopBits.One, ReadTimeout = 500, WriteTimeout = 500 };
                    serialPort.Open();
                    run = true;
                    ComMessageReceived += DebugComReceiveMessage;
                    new Thread(() => { ReceiveComMessage(); }).Start();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }

        public bool Uninitialize()
        {
            try
            {
                run = false;
                ComMessageReceived -= DebugComReceiveMessage;
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }
    }
}
