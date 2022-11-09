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
        private DstUdsMessage dstUdsServiceResponseMessage;

        public DstUdsServiceHandler(DstUdsHandler udsHandler)
        {
            this.udsHandler = udsHandler;
            _responseFlag = new AutoResetEvent(false);
        }

        public bool SendDiagnosticSessionControl(UDS_SERVICE_DSC sessionType)
        {
            Console.Write("Security access pending: ");
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x10, Data = new() { (byte)sessionType } });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForServce;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x50) return true;
            else return false;
        }

        private void WaitForServce(DstUdsMessage udsMessage)
        {
            if (udsMessage.Address == udsHandler.destinationAddress)
            {
                udsHandler.UdsMessageReceived -= WaitForServce;
                dstUdsServiceResponseMessage = udsMessage;
                _responseFlag.Set();
            }
        }

        private bool SendSecurityAccess(byte securityAccessLevel, out List<byte> securitySeed)
        {
            Console.Write("Security access pending: ");
            securitySeed = new();
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x27, Data = new() { securityAccessLevel } });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForServce;
                return false;
            }
            securitySeed = dstUdsServiceResponseMessage.Data;
            if (dstUdsServiceResponseMessage.SID == 0x67) return true;
            else return false;
        }

        private bool SendSecurityAccessWithData(byte securityAccessLevel, byte[] securityAccessData)
        {
            Console.Write("Security access w/data pending: ");
            List<byte> tempList = securityAccessData.ToList();
            tempList.Insert(0, securityAccessLevel);
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = (byte)(2 + securityAccessData.Length), SID = 0x27, Data = tempList });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForServce;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x67) return true;
            else return false;
        }

        private bool SendEcuReset(byte resetParameter)
        {
            Console.Write("ECU Reset pending: ");
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x11, Data = new() { resetParameter } });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForServce;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x51) return true;
            else return false;

        }


    }
}
