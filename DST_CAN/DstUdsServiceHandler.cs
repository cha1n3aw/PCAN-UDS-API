using PCAN_UDS_TEST.PCAN;
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
                //else testerPresentThread.Start();
                return true;
            }
            catch (Exception) { return false; }
        }

        #region UdsServiceWrappers
        public bool ResetECU(UDSApi.UDS_SERVICE_PARAMETER_ECU_RESET resetParameter)
        {
            try
            {
                SendEcuReset(resetParameter);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsSetParameter(DATA_IDENTIFIER dataIdentifier, byte[] value)
        {
            try
            {
                SendWriteDataByIdentifier(dataIdentifier, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetErrorsList(byte dtcType, UDSApi.UDS_SERVICE_ERRORS_TYPE statusMask, out List<byte> response)
        {
            response = new();
            try
            {
                return SendReadDTCInformation(dtcType, (byte)statusMask, out response);
			}
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsSetSecurityAccessLevel(UDSApi.UDS_ACCESS_LEVEL accessLevel)
        {
            try
            {
                SendSecurityAccess(accessLevel, out List<byte> seed);
                SendSecurityAccessWithData((byte)(accessLevel + 1), new SecurityAccess().GetKey(seed.ToArray(), (byte)accessLevel).ToList());
				return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetDataByIdentifiers(DATA_IDENTIFIER[] dataIdentifiers, out byte[] dataArray)
        {
            dataArray = Array.Empty<byte>();
            try
            {
                SendReadDataByIdentifier(dataIdentifiers, out dataArray);
                if (dataArray != null && dataArray != Array.Empty<byte>()) return true;
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetParameterMenus(out Dictionary<byte, string> menuList)
        {
            menuList = new();
            try
            {
                int i = 2;
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2200 }, out byte[] dataArray);
                byte numberOfMenus = dataArray[i++];
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    string menuName = string.Empty;
                    while (dataArray[i] != 0x00) menuName += (char)dataArray[i++];
                    menuList.Add(address, menuName);
                    if (menuList.Count == numberOfMenus) break;
                    if (i == dataArray.Length - 1)
                    {
                        SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2200 }, out dataArray);
                        i = 2;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetParameterSubmenus(byte menuAddress, out Dictionary<byte, string> submenuList)
        {
            submenuList = new();
            try
            {
                int i = 2;
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x2201 + menuAddress) }, out byte[] dataArray);
                byte numberOfSubmenus = dataArray[i++];
                if (dataArray.Length < 5) return true;
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    string submenuName = string.Empty;
                    while (dataArray[i] != 0x00) submenuName += (char)dataArray[i++];
                    submenuList.Add(address, submenuName);
                    if (submenuList.Count == numberOfSubmenus) break;
                    if (i == dataArray.Length - 1)
                    {
                        SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x2201 + menuAddress) }, out dataArray);
                        i = 2;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetProcessDataGroups(out Dictionary<byte, string> groupList)
        {
            groupList = new();
            try
            {
                int i = 3;
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2201 }, out byte[] dataArray);
                byte numberOfGroups = dataArray[i++];
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    string groupName = string.Empty;
                    while (dataArray[i] != 0x00) groupName += (char)dataArray[i++];
                    groupList.Add(address, groupName);
                    if (groupList.Count == numberOfGroups) break;
                    if (i == dataArray.Length - 1)
                    {
                        SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2201 }, out dataArray);
                        i = 3;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetProcessData(byte groupAddress, byte parameterAddress, out Dictionary<byte, Data> parameterList)
        {
            parameterList = new();
            try
            {
                int i = 3;
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x2000 + (groupAddress << 4) + parameterAddress) }, out byte[] dataArray);
                byte numberOfParameters = dataArray[i++];
                if (dataArray.Length < 5) return true; //62 DIDHB DIDLB SIZE
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    if (dataArray[i] == 0x00)
                    {
                        parameterList.Add(address, new Data() { isAccessible = false, name = "Inaccessible" });
                        continue;
                    }
                    Data data = new() { isAccessible = true };
                    while (dataArray[i] != 0x00) data.name += (char)dataArray[i++];
                    i++;
                    data.multiplier = (ushort)(dataArray[i++] << 8 | dataArray[i++]);
                    data.divisor = (ushort)(dataArray[i++] << 8 | dataArray[i++]);
                    data.valueType = dataArray[i++];
                    data.digits = (ushort)(dataArray[i++] << 8 | dataArray[i++]);
                    data.unitCode = dataArray[i++];
                    data.accessLevel = dataArray[i++];
                    parameterList.Add(address, data);
                    if (parameterList.Count == numberOfParameters) break;
                    if (i == dataArray.Length - 1)
                    {
                        SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x2000 + (groupAddress << 4) + parameterAddress) }, out dataArray);
                        i = 5;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetParameters(byte menuAddress, byte subMenuAddress, byte parameterAddress, out Dictionary<byte, Data> parameterList)
        {
            parameterList = new();
            try
            {
                int i = 2;
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)((menuAddress << 7) + (subMenuAddress << 4) + parameterAddress) }, out byte[] dataArray);
                byte numberOfParameters = dataArray[i++];
                if (dataArray.Length < 5) return true;
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    if (dataArray[i] == 0x00)
                    {
                        parameterList.Add(address, new Data() { isAccessible = false, name = "Inaccessible" });
                        continue;
                    }
                    Data data = new() { isAccessible = true };
                    while (dataArray[i] != 0x00) data.name += (char)dataArray[i++];
                    i++;
                    data.value = (short)(dataArray[i++] << 8 | dataArray[i++]);
                    data.multiplier = (ushort)(dataArray[i++] << 8 | dataArray[i++]);
                    data.divisor = (ushort)(dataArray[i++] << 8 | dataArray[i++]);
                    data.digits = (ushort)(dataArray[i++] << 8 | dataArray[i++]);
                    data.valueType = dataArray[i++];
                    data.minValue = (short)(dataArray[i++] << 8 | dataArray[i++]);
                    data.maxValue = (short)(dataArray[i++] << 8 | dataArray[i++]);
                    data.step = (ushort)(dataArray[i++] << 8 | dataArray[i++]);
                    data.defaultValue = (short)(dataArray[i++] << 8 | dataArray[i++]);
                    data.unitCode = dataArray[i++];
                    data.eepromPage = dataArray[i++];
                    data.eepromAddress = dataArray[i++];
                    parameterList.Add(address, data);
                    if (parameterList.Count == numberOfParameters) break;
                    if (i == dataArray.Length - 1)
                    {
                        SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)((menuAddress << 7) + (subMenuAddress << 4) + parameterAddress) }, out dataArray);
                        //Console.WriteLine($"{menuAddress} {subMenuAddress} {parameterAddress}");
                        //Console.WriteLine($"{(menuAddress << 7) + (subMenuAddress << 4) + parameterAddress:X4}");
                        //foreach (byte b in dataArray) Console.Write($"{b:X2} ");
                        //Console.WriteLine();
                        i = 2;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetUnitcodes(out Dictionary<byte, string> unitcodesList)
        {
            unitcodesList = new();
            try
            {
                int i = 2;
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2402 }, out byte[] dataArray);
                byte numberOfUnitcodes = dataArray[i++];
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    string unitcode = string.Empty;
                    while (dataArray[i] != 0x00) unitcode += (char)dataArray[i++];
                    unitcodesList.Add(address, unitcode);
                    if (unitcodesList.Count == numberOfUnitcodes) break;
                    if (i == dataArray.Length - 1)
                    {
                        SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2402 }, out dataArray);
                        i = 2;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetListDescriptions(out List<ListEntry> listDescriptions)
        {
            listDescriptions = new();
            try
            {
                int i = 3;
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2403 }, out byte[] dataArray);
                ushort numberOfListDescriptions = (ushort)((dataArray[i++] << 8) + dataArray[i++]);
                byte maxNumberOfListEntries = dataArray[i++];
                for (; ; i++)
                {
                    ushort address = (ushort)((dataArray[i++] << 8) + dataArray[i++]);
                    string listEntryString = string.Empty;
                    while (dataArray[i] != 0x00) listEntryString += (char)dataArray[i++];
                    if (listDescriptions.Any(x => x.address == address / maxNumberOfListEntries)) listDescriptions.First(x => x.address == address / maxNumberOfListEntries).listEntries.Add((byte)(address % maxNumberOfListEntries), listEntryString);
                    else listDescriptions.Add(new ListEntry() { address = (byte)(address / maxNumberOfListEntries), listEntries = new Dictionary<byte, string>(new[] { new KeyValuePair<byte, string>((byte)(address % maxNumberOfListEntries), listEntryString) }) });
                    if (listDescriptions.Count == numberOfListDescriptions / maxNumberOfListEntries) break;
                    if (i == dataArray.Length - 1)
                    {
                        SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2403 }, out dataArray);
                        i = 5;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetSavedErrors(out Dictionary<byte, Error> errorList)
        {
            errorList = new();
            try
            {
                int i = 2;
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2404 }, out byte[] dataArray);
                byte numberOfErrors = dataArray[i++];
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    ushort code = (ushort)((dataArray[i++] << 8) + dataArray[i++]);
                    byte parameter = dataArray[i++];
                    byte occurence = dataArray[i++];
                    uint timestamp = (uint)((dataArray[i++] << 24) + (dataArray[i++] << 16) + (dataArray[i++] << 8) + dataArray[i++]);
                    string description = string.Empty;
                    while (dataArray[i] != 0x00) description += (char)dataArray[i++];
					if (description == string.Empty) description = "No description available";
					errorList.Add(address, new Error() { errorCode = code, occurence = occurence, parameter = parameter, description = description, timestamp = timestamp });
                    if (errorList.Count == numberOfErrors) break;
                    if (i == dataArray.Length - 1)
                    {
                        SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2404 }, out dataArray);
                        i = 2;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsGetActiveErrors(out Dictionary<byte, Error> errorList)
        {
            errorList = new();
            try
            {
                int i = 2;
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2405 }, out byte[] dataArray);
                byte numberOfErrors = dataArray[i++];
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    ushort code = (ushort)((dataArray[i++] << 8) + dataArray[i++]);
                    byte parameter = dataArray[i++];
                    byte occurence = dataArray[i++];
                    uint timestamp = (uint)((dataArray[i++] << 24) + (dataArray[i++] << 16) + (dataArray[i++] << 8) + dataArray[i++]);
                    string description = string.Empty;
                    while (dataArray[i] != 0x00) description += (char)dataArray[i++];
                    if (description == string.Empty) description = "No description available";
                    errorList.Add(address, new Error() { errorCode = code, occurence = occurence, parameter = parameter, description = description, timestamp = timestamp });
                    if (errorList.Count == numberOfErrors) break;
                    if (i == dataArray.Length - 1)
                    {
                        SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x2405 }, out dataArray);
                        i = 2;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private DATA_IDENTIFIER UdsGetParameterIdentifier(byte menuNumber, byte subMenuNumber, byte parameterNumber) => (DATA_IDENTIFIER)(10000 + 100 * menuNumber + 10 * subMenuNumber + parameterNumber);

        private ushort UdsGetProcessDataIdentifier(byte groupNumber, byte parameterNumber) => (ushort)(20000 + 10 * groupNumber + parameterNumber);

        public bool GetDataByIdentifiers(byte menuNumber, byte subMenuNumber, byte parameterNumber, out byte[] dataArray) //only value
        {
            dataArray = Array.Empty<byte>();
            try
            {
                SendReadDataByIdentifier(new DATA_IDENTIFIER[] { UdsGetParameterIdentifier(menuNumber, subMenuNumber, parameterNumber) }, out dataArray);
                if (dataArray != null && dataArray != Array.Empty<byte>()) return true;
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion

        #region DstUdsServiceHandlers
        private void WaitForService(DstUdsMessage udsMessage)
        {
            if (udsMessage.Address == udsHandler.destinationAddress)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                dstUdsServiceResponseMessage = udsMessage;
                _responseFlag.Set();
            }
        }

        private void SendTesterPresent()
        {

			while (true)
            {
				Thread.Sleep(3000);
				Console.WriteLine("TesterPresent executed");
                while (_servicePending) Thread.Sleep(10);
                _servicePending = true;
                udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x3E, Data = new() { 0x00 }, Address = udsHandler.sourceAddress });
                udsHandler.UdsMessageReceived += WaitForService;
                bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
                if (response == null || response == false)
                {
                    udsHandler.UdsMessageReceived -= WaitForService;
                    break;
                }
                if (dstUdsServiceResponseMessage.SID != 0x7E) break;
                _servicePending = false;
            }
            _servicePending = false;
        }

        public bool SendDiagnosticSessionControl(UDSApi.UDS_SERVICE_DSC sessionType)
        {
            Console.WriteLine("DSC pending");
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x10, Data = new() { (byte)sessionType }, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForService;
			bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x50) return true;
            else return false;
        }

        private bool SendSecurityAccess(UDSApi.UDS_ACCESS_LEVEL securityAccessLevel, out List<byte> securitySeed)
        {
            Console.WriteLine("Security access pending");
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            securitySeed = new();
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x27, Data = new() { (byte)securityAccessLevel }, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForService;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                return false;
            }
            securitySeed = dstUdsServiceResponseMessage.Data.Skip(1).ToList();
            //Console.Write("Received seed: ");
            //foreach (byte b in securitySeed) Console.Write($"{b:X2} ");
            //Console.WriteLine();
            if (dstUdsServiceResponseMessage.SID == 0x67) return true;
            else return false;
        }

        private bool SendSecurityAccessWithData(byte securityAccessLevel, List<byte> securityAccessData)
        {
            Console.WriteLine("Security access w/data pending");
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            List<byte> tempList = securityAccessData;
            tempList.Insert(0, securityAccessLevel);
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = (byte)(tempList.Count + 1), SID = 0x27, Data = tempList, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForService;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x67) return true;
            else return false;
        }

        private bool SendEcuReset(UDSApi.UDS_SERVICE_PARAMETER_ECU_RESET resetParameter)
        {
            Console.WriteLine("ECU Reset pending");
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = 2, SID = 0x11, Data = new() { (byte)resetParameter }, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForService;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x51) return true;
            else return false;

        }

        private bool SendReadDataByIdentifier(DATA_IDENTIFIER[] dataIdentifiers, out byte[] outData)
        {
            Console.WriteLine("Read data service pending");
            while (_servicePending) Thread.Sleep(1);
            _servicePending = true;
            outData = Array.Empty<byte>();
            List<byte> tempList = new();
            foreach (DATA_IDENTIFIER dataIdentifier in dataIdentifiers)
            {
                tempList.Add((byte)((ushort)dataIdentifier >> 8));
                tempList.Add((byte)((ushort)dataIdentifier & 0xFF));
            }
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = (byte)(tempList.Count + 1), SID = 0x22, Data = tempList, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForService;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            _servicePending = false;
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                return false;
            }
            outData = dstUdsServiceResponseMessage.Data.ToArray();
            if (dstUdsServiceResponseMessage.SID == 0x62) return true;
            else return false;
        }

        private bool SendWriteDataByIdentifier(DATA_IDENTIFIER dataIdentifier, byte[] value)
        {
            Console.WriteLine("Write data service pending");
            List<byte> tempList = new() { (byte)(((ushort) dataIdentifier) >> 8), (byte)(((ushort)dataIdentifier) & 0xFF) };
            tempList.AddRange(value);
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = (byte)(tempList.Count + 1), SID = 0x2E, Data = tempList, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForService;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x6E) return true;
            else return false;
        }

        private bool SendReadMemoryByAddress(byte[] memoryAddressBuffer, byte[] memorySizeBuffer)
        {
            Console.WriteLine("Write data service pending");
            List<byte> tempList = new();
            tempList.AddRange(memoryAddressBuffer);
            tempList.AddRange(memorySizeBuffer);
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = (byte)(tempList.Count + 1), SID = 0x23, Data = tempList, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForService;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x63) return true;
            else return false;
        }

        private bool SendWriteMemoryByAddress()
        {
            Console.WriteLine("Write data service pending");
            List<byte> tempList = new();
            udsHandler.SendUdsMessage(new DstUdsMessage { Size = (byte)(tempList.Count + 1), SID = 0x3D, Data = tempList, Address = udsHandler.sourceAddress });
            udsHandler.UdsMessageReceived += WaitForService;
            bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
            if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x7D) return true;
            else return false;
        }

        private bool SendReadDTCInformation(byte readInformationType, byte dtcStatusMask, out List<byte> arrayResponse)
        {
			arrayResponse = new();
            Console.WriteLine("Read DTC service pending");
            while (_servicePending) Thread.Sleep(1);
			_servicePending = true;
			udsHandler.SendUdsMessage(new DstUdsMessage { Size = 3, SID = 0x19, Data = new() { readInformationType, dtcStatusMask }, Address = udsHandler.sourceAddress });
			udsHandler.UdsMessageReceived += WaitForService;
			bool? response = _responseFlag?.WaitOne(udsHandler.MaxWait);
			_servicePending = false;
			if (response == null || response == false)
            {
                udsHandler.UdsMessageReceived -= WaitForService;
                return false;
            }
            if (dstUdsServiceResponseMessage.SID == 0x59)
            {
                arrayResponse = dstUdsServiceResponseMessage.Data;
                return true;
            }
            else return false;
        }
        #endregion
    }
}
