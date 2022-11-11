using System.IO.Ports;

namespace PCAN_UDS_TEST.DST_CAN
{
    internal class DstComHandler
    {
        private Thread receiveThread;
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

        public DstComHandler(string portName)
        {
            this.portName = portName;
            run = false;
        }
        private void DebugComReceiveMessage(List<byte> comMessage)
        {
            Console.Write("CAN MESSAGE RECEIVED: ");
            foreach (byte b in comMessage) Console.Write($"{b:X2} ");
            Console.WriteLine();
        }

        private void ReceiveComMessage()
        {
			while (run)
            {
                if (serialPort.IsOpen && serialPort.BytesToRead > 0)
                {
                    int bytesToRead = serialPort.BytesToRead;
                    Console.WriteLine($"Receive COM message: {bytesToRead} bytes");
					List<byte> comBuffer = new();
                    byte[] byteArray = new byte[bytesToRead];
                    serialPort.Read(byteArray, 0, bytesToRead);
                    comBuffer = byteArray.ToList();
                    //for (int i = 0; i < bytesToRead; i++) comBuffer.Add((byte)serialPort.ReadByte());
                    int y = 0;
                    while (y < comBuffer.Count)
                    {
                        List<byte> comMessage = new();
                        while (comBuffer[y++] != 0x24);
                        int packetStartIndex = --y;
                        int canPacketSize;
                        if (comBuffer[y + 1] >> 4 == 0x00) canPacketSize = (comBuffer[y + 1] & 0x0F) + 4;
                        else canPacketSize = (comBuffer[y + 1] & 0x0F) + 6; //do not jump to 1st address byte because crc requires byteArray [1]..[^1]
                        for (; y < packetStartIndex + canPacketSize; y++) comMessage.Add(comBuffer[y]);
                        if (comBuffer[y++] == CalculateCrc8(comMessage.Skip(1).ToArray())) _comMessageReceived?.Invoke(comMessage.ToList());
                        else y = packetStartIndex + 1;
                    }
                }
            }
        }

        public bool SendComMessage(List<byte> dataToSend)
        {
            try
            {
                if (dataToSend.Count <= 12) dataToSend.Insert(0, (byte)(dataToSend.Count - 4 + 0x10));
                else return false;
                dataToSend.Insert(0, 0x24);
                dataToSend.Add(CalculateCrc8(dataToSend.Skip(1).ToArray()));
                serialPort.Write(dataToSend.ToArray(), 0, dataToSend.Count);
                return true;
            }
            catch(Exception) { return false; }
        }

        private byte CalculateCrc8(byte[] array)
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
                    run = true;
                    serialPort = new() { PortName = portName, BaudRate = 115200, Parity = Parity.None, DataBits = 8, StopBits = StopBits.One, ReadTimeout = 500, WriteTimeout = 500 };
                    serialPort.Open();
                    ComMessageReceived += DebugComReceiveMessage;
                    receiveThread = new Thread(() => { ReceiveComMessage(); });
                    receiveThread.Start();
                    return true;
                }
                else return false;
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
                    while (receiveThread.ThreadState != ThreadState.Stopped) ;
                    serialPort.Close();
                    serialPort.Dispose();
                    GC.Collect();
                    return true;
                }
                else return false;
            }
            catch { return false; }
        }
    }
}