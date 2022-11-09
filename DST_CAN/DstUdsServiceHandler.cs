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
        private bool _servicePending = false;
        private Thread testerPresentThread;
        private AutoResetEvent _responseFlag;
        private DstUdsHandler udsHandler;
        private DstUdsMessage dstUdsServiceResponseMessage;

        public DstUdsServiceHandler(DstUdsHandler udsHandler)
        {
            testerPresentThread = new(() => SendTesterPresent());
            this.udsHandler = udsHandler;
            _responseFlag = new AutoResetEvent(false);
        }

        public bool Authenticate(UDSApi.UDS_SERVICE_DSC sessionType, UDSApi.UDS_ACCESS_LEVEL accessLevel)
        {
            try
            {
                if (!SendDiagnosticSessionControl(sessionType)
                || !SendSecurityAccess(accessLevel, out List<byte> seed)
                || !SendSecurityAccessWithData((byte)(accessLevel + 1), new SecurityAccess().GetKey(seed.ToArray(), (byte)accessLevel).ToList())) return false;
                else testerPresentThread.Start();
                return true;
            }
            catch (Exception) { return false; }
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

        private void SendTesterPresent()
        {
            while (true)
            {
                while (_servicePending) Thread.Sleep(10);
                _servicePending = true;
                udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x3E, Data = new() { 0x00 }, Address = udsHandler.sourceAddress });
                udsHandler.UdsMessageReceived += WaitForServce;
                bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
                if (response == null || response == false)
                {
                    udsHandler.UdsMessageReceived -= WaitForServce;
                    break;
                }
                if (dstUdsServiceResponseMessage.Data[0] != 0x7E) break;
                _servicePending = false;
                Thread.Sleep(3000);
            }
            _servicePending = false;
        }

        private bool SendDiagnosticSessionControl(UDSApi.UDS_SERVICE_DSC sessionType)
        {
            Console.Write("Security access pending: ");
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x10, Data = new() { (byte)sessionType }, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForServce;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x50) return true;
            else return false;
        }

        private bool SendSecurityAccess(UDSApi.UDS_ACCESS_LEVEL securityAccessLevel, out List<byte> securitySeed)
        {
            Console.Write("Security access pending: ");
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            securitySeed = new();
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x27, Data = new() { (byte)securityAccessLevel }, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
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
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            List<byte> tempList = securityAccessData;
            tempList.Insert(0, securityAccessLevel);
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = (byte)(tempList.Count + 1), SID = 0x27, Data = tempList, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
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
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x11, Data = new() { (byte)resetParameter }, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
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
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            outData = Array.Empty<byte>();
            List<byte> tempList = new();
            foreach (DATA_IDENTIFIER d in dataIdentifiers) tempList.AddRange(BitConverter.GetBytes((ushort)d));
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x22, Data = tempList });
            udsHandler.UdsMessageReceived += WaitForServce;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
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
