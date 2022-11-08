namespace CAN_COM
{
    struct CanMessage
    {
        public byte Size;
        public byte Type;
        public uint Address;
        public List<byte> Data;
    }

    struct UdsMessage
    {
        public byte Size;
        public byte SID;
        public List<byte> Data;
    }

    internal class Xrogram
    {
        private static uint sourceAddress = 0x18DA01FA;
        private static uint destinationAddress = 0x18DAFA01;
        private static string portName = "COM3";
        static void ReceiveUds(UdsMessage udsMessage)
        {
            Console.Write($"{udsMessage.Size} - {udsMessage.SID} - ");
            foreach (byte b in udsMessage.Data) Console.Write($"{b:X2} ");
            Console.WriteLine();
        }
        static void Main()
        {
            DstCanComUdsHandler udsHandler = new(portName, sourceAddress, destinationAddress);
            udsHandler.UdsMessageReceived += ReceiveUds;
            udsHandler.Initialize();
            udsHandler.SendUdsMessage(new UdsMessage() { Size = 5, SID = 22, Data = new List<byte>() { 0x01, 0x01, 0x01, 0x01, 0x01 } });
            udsHandler.Uninitialize();
        }
    }
}