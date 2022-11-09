using Peak.Can.Uds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PCAN_UDS_TEST.DST_CAN.DstUdsServiceHandler;
using DATA_IDENTIFIER = Peak.Can.Uds.UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER;

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
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x10, Data = new() { (byte)sessionType }, Address = udsHandler.sourceAddress });
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

        public bool Authenticate(byte accessLevel)
        {
            try
            {
                SendSecurityAccess(accessLevel, out List<byte> seed);
                SecurityAccess securityAccess = new();
                byte[] key = securityAccess.GetKey(seed.ToArray(), accessLevel);
                return SendSecurityAccessWithData((byte)(accessLevel + 1), key.ToList());
            }
            catch (Exception)
            {
                return false;
            }
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

        public bool SendSecurityAccess(byte securityAccessLevel, out List<byte> securitySeed)
        {
            Console.Write("Security access pending: ");
            securitySeed = new();
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x27, Data = new() { securityAccessLevel }, Address = udsHandler.sourceAddress });
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

        private bool SendSecurityAccessWithData(byte securityAccessLevel, List<byte> securityAccessData)
        {
            Console.Write("Security access w/data pending: ");
            List<byte> tempList = securityAccessData;
            tempList.Insert(0, securityAccessLevel);
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = (byte)(tempList.Count + 1), SID = 0x27, Data = tempList, Address = udsHandler.sourceAddress });
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

        public bool SendEcuReset(UDS_SERVICE_PARAMETER_ECU_RESET resetParameter)
        {
            Console.Write("ECU Reset pending: ");
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x11, Data = new() { (byte)resetParameter }, Address = udsHandler.sourceAddress });
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

        private bool SendReadDataByIdentifier(DATA_IDENTIFIER[] dataIdentifiers, out byte[] outData)
        {
            Console.Write("Read data service pending: ");
            outData = Array.Empty<byte>();
            List<byte> tempList = new();
            foreach (DATA_IDENTIFIER d in dataIdentifiers) tempList.AddRange(BitConverter.GetBytes((ushort)d));
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x22, Data = tempList });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForServce;
                return false;
            }
            outData = dstUdsServiceResponseMessage.Data.ToArray();
            if (dstUdsServiceResponseMessage.SID == 0x62) return true;
            else return false;
        }
    }
}
