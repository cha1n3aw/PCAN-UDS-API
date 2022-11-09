using Peak.Can.Uds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PCAN_UDS_TEST.DST_CAN.DstUdsServiceHandler;

namespace PCAN_UDS_TEST.DST_CAN
{
    internal partial class DstUdsServiceHandler
    {
        private AutoResetEvent _responseFlag;
        private DstUdsHandler udsHandler;
        private readonly TimeSpan MaxWait = TimeSpan.FromMilliseconds(60000);
        private DstUdsMessage dstUdsMessage;
        //private uint sourceAddress;
        //private uint destinationAddress;

        public DstUdsServiceHandler(DstUdsHandler udsHandler)
        {
            this.udsHandler = udsHandler;
            _responseFlag = new AutoResetEvent(false);
        }

        public byte[] SendDiagnosticSessionControl(UDS_SERVICE_DSC sessionType)
        {
            //neebu
            return null;
        }

        private void WaitForServce(DstUdsMessage udsMessage)
        {
            switch (udsMessage.SID)
            {
                case 0x67:
                    Console.WriteLine("Security access seed received");
                    break;
            }

            _responseFlag.Set();
            udsHandler.UdsMessageReceived -= WaitForServce;
            dstUdsMessage = udsMessage;
        }

        private bool SendSecurityAccess(byte securityAccessLevel, out List<byte> seed)
        {
            Console.Write("Security access pending: ");
            seed = new();
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x27, Data = new() { securityAccessLevel } });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(MaxWait);
            if (response == null || response == false) return false;
            seed = dstUdsMessage.Data;
            return true;
        }
    }
}
