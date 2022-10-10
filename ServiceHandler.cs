﻿using Peak.Can.IsoTp;
using Peak.Can.Uds;
using System.Text;

namespace PCAN_UDS_TEST
{
    public struct Data
    {
        public ushort dataIdentifier;
        public bool isAccessible; //ensures that did is accessible and no "security access denied" error happened
        public string name;
        public byte valueType;
        public short minValue;
        public short maxValue;
        public byte step;
        public string dimension;
        public ushort multiplier;
        public ushort divider;
        public ushort precision;
        public byte accessLevel;
        public short defaultValue;
        public byte eepromPage;
        public byte eepromAddress;
        public short value;

        public override string ToString() => $"{name}: {minValue} - {value} - {maxValue}, {dimension}";
    }

    public class ServiceHandler
    {
        #region GlobalParameters
        private readonly CantpHandle handle;
        private UdsNetAddressInfo NAI;
        private UdsMessageConfig requestConfig;
        private UdsMessageConfig responseConfig;
        #endregion

        #region Constructor
        public ServiceHandler(CantpHandle handle, byte sourceAddress, byte destinationAddress)
        {
            this.handle = handle;
            NAI = new()
            {
                PROTOCOL = UDS_MESSAGE_PROTOCOL.PUDS_MSGPROTOCOL_ISO_15765_2_29B_FIXED_NORMAL,
                TARGET_TYPE = CANTP_ISOTP_ADDRESSING.PCANTP_ISOTP_ADDRESSING_PHYSICAL,
                SOURCE_ADDRESS = sourceAddress,
                DESTINATION_ADDRESS = destinationAddress,
                extension_addr = 0
            };
            requestConfig = new()
            {
                CAN_ID = 0xFFFFFFFF,
                CAN_MESSAGE_TYPE = CANTP_CAN_MESSAGE_TYPE.PCANTP_CAN_MSGTYPE_EXTENDED,
                TYPE = UDS_MESSAGE_TYPE.PUDS_MSGTYPE_USDT,
                NAI = NAI
            };
            responseConfig = requestConfig;
            responseConfig.NAI.SOURCE_ADDRESS = NAI.DESTINATION_ADDRESS;
            responseConfig.NAI.DESTINATION_ADDRESS = NAI.SOURCE_ADDRESS;
        }
        #endregion

        #region HighLevelServices
        public byte[] GetSequence(byte offset, byte length) //A004446, a(n) = Nimsum n + 5.
        {
            byte[] sequence = new byte[length];
            for (byte i = 0; i < length; i++) sequence[i] = (byte)((i ^ 5) + offset);
            return sequence;
        }

        private byte GetMenuAddress(byte menuNumber) => (byte)((0x7F - menuNumber & 0xF0) | (menuNumber & 0x0F));

		public bool GetDataFromByteArray(byte[] byteArray, out List<Data> dataArray)
		{
			dataArray = new();
			int y = 4; //6
            try
            {
                for (; y <= byteArray.Length; y++) //y+=3
                {
					Data data = new();
                    data.dataIdentifier = (ushort)(byteArray[y] << 8 | byteArray[y + 1]);
                    if (byteArray[y] == 0x00)
                    {
                        data.isAccessible = false;
                        dataArray.Add(data);
                        continue;
                    }
                    else data.isAccessible = true;
                    y += 3;
                    for (; byteArray[y] != 0x00; y++) data.name += (char)byteArray[y];
                    y++;
                    data.valueType = byteArray[y];
                    y++;
                    data.minValue = (short)(byteArray[y] << 8 | byteArray[y + 1]);
                    y += 2;
                    data.maxValue = (short)(byteArray[y] << 8 | byteArray[y + 1]);
                    y += 3;
                    data.step = byteArray[y];
                    y++;
                    for (; byteArray[y] != 0x00; y++) data.dimension += (char)byteArray[y];
                    y++;
                    data.multiplier = (ushort)(byteArray[y] << 8 | byteArray[y + 1]);
                    y += 2;
					data.divider = (ushort)(byteArray[y] << 8 | byteArray[y + 1]);
                    y += 2;
					data.precision = (ushort)(byteArray[y] << 8 | byteArray[y + 1]);
                    y += 2;
                    data.accessLevel = byteArray[y];
                    y++;
                    data.defaultValue = (short)(byteArray[y] << 8 | byteArray[y + 1]);
                    y += 2;
                    data.eepromPage = byteArray[y];
                    y++;
                    data.eepromAddress = byteArray[y];
                    y += 3;
                    data.value = (short)(byteArray[y] << 8 | byteArray[y + 1]);
                    y++;
					dataArray.Add(data);
                }
                return true;
            }
            catch(Exception)
            {
                dataArray = new();
                return false;
            }
		}

		public bool GetDataByIdentifiers(byte menuNumber, byte subMenuNumber, UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER[] dataIdentifiers, out byte[] dataArray)
        {
            if (SetSubMenuCursor(menuNumber, subMenuNumber))
            {
                dataArray = SendReadDataByIdentifier(dataIdentifiers);
                if (dataArray != null && dataArray != Array.Empty<byte>()) return true;
                else return false;
            }
            else
            {
                dataArray = Array.Empty<byte>();
                return false;
            }
        }

        public bool GetMenus(out List<string> menuStrings)
        {
            menuStrings = new();
            int menuCount = 0;
            bool run = true;
            while (run)
            {
                byte[] byteArray = SendReadDataByIdentifier(new UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER[] { (UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0xFE00 });
                if (byteArray != null && byteArray != Array.Empty<byte>())
                {
                    for (int i = 8; i < byteArray.Length; i++)
                    {
                        if (i == 8)
                        {
                            menuCount = byteArray[7];
                            if (byteArray[i + 1] == 0x00) menuStrings.Add(string.Empty);
                            else menuStrings.Add($"{(byteArray[i] + 1)} ");
                        }
                        else if (byteArray[i] == 0x00)
                        {
                            i++; //jump to a next byte that contains menu number
                            if (i + 1 < byteArray.Length) //if "out of bounds" is ok
                            {
                                if (byteArray[i + 1] == 0x00) menuStrings.Add(string.Empty); //next 0x00 represents that menu name is empty
                                else menuStrings.Add($"{(byteArray[i] + 1)} ");
                                if (byteArray[i] == menuCount - 1) run = false;
                            }
                        }
                        else menuStrings[^1] += $"{(char)byteArray[i]}";
                    }
                }
                else return false;
            }
            return true;
        }

        public bool GetSubMenus(byte menuNumber, out List<string> subMenuStrings)
        {
            subMenuStrings = new();
            if (!SetSubMenuCursor(menuNumber, 0x00)) return false;
            byte[] byteArray = SendReadDataByIdentifier(new UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER[] { (UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0xFE02 });
            if (byteArray != null && byteArray != Array.Empty<byte>())
                for (int i = 8; i < byteArray.Length; i++)
                    if (i == 8)
                        if (byteArray[i + 1] == 0x00) subMenuStrings.Add(string.Empty); 
                        else subMenuStrings.Add($"{(byteArray[i] + 1)} ");
                    else if (byteArray[i] == 0x00)
                    {
                        i++;
                        if (i + 1 < byteArray.Length)
                            if (byteArray[i + 1] == 0x00) subMenuStrings.Add(string.Empty);
                            else subMenuStrings.Add($"{(byteArray[i] + 1)} ");
                    }
                    else subMenuStrings[^1] += $"{(char)byteArray[i]}";
            else return false;
            return true;
        }

        public bool SetSubMenuCursor(byte menuNumber, byte? subMenuNumber)
        {
            byte[] response;
            if (subMenuNumber == null) response = SendWriteDataByIdentifier((UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)(GetMenuAddress(menuNumber) << 8 | 0xFE), new byte[] { 0x01, 0x00, 0x00, menuNumber });
            else response = SendWriteDataByIdentifier((UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)(GetMenuAddress(menuNumber) << 8 | GetSequence(0x70, 8)[(byte)subMenuNumber]), new byte[] { 0xFE, 0x01, 0x00, 0x00, menuNumber, (byte)subMenuNumber });
            Console.Write("Set submenu cursor response: ");
            foreach (byte b in response) Console.Write($"{b:X2} ");
            Console.WriteLine();
            //if (response.Equals(new byte[] { 0x6E, 0xFE, 0x01 })) return true;
            //return false;
            return true;
        }
        #endregion

        #region SendServices
        /// <summary>
        /// The DiagnosticSessionControl service is used to enable different diagnostic sessions in the server.
        /// </summary>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendDiagnosticSessionControl()
        {
            Console.WriteLine($"Service: {UDSApi.SvcDiagnosticSessionControl_2013(handle, requestConfig, out UdsMessage outMessage, UDSApi.uds_svc_param_dsc.PUDS_SVC_PARAM_DSC_DS)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_DiagnosticSessionControl + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// The ECUReset service is used by the client to request a server reset.
        /// </summary>
        /// <param name="resetParameter">Subfunction parameter: type of Reset (see PUDS_SVC_PARAM_ER_xxx)</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendEcuReset(UDSApi.UDS_SERVICE_PARAMETER_ECU_RESET resetParameter)
        {
            Console.WriteLine($"Service: {UDSApi.SvcECUReset_2013(handle, requestConfig, out UdsMessage outMessage, resetParameter)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_ECUReset + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// SecurityAccess service provides a means to access data and/or diagnostic services which have
        /// restricted access for security, emissions or safety reasons.
        /// </summary>
        /// <param name="securityAccessType">Subfunction parameter: type of SecurityAccess (see PUDS_SVC_PARAM_SA_xxx)</param>
        /// <param name="securityAccessData">If Requesting Seed, buffer is the optional data to transmit to a server (like identification).
        /// If Sending Key, data holds the value generated by the security algorithm corresponding to a specific "seed" value</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendSecurityAccess(byte securityAccessType, byte[] securityAccessData)
        {
            Console.WriteLine($"Service: {UDSApi.SvcSecurityAccess_2013(handle, requestConfig, out UdsMessage outMessage, securityAccessType, securityAccessData, (uint)securityAccessData.Length)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_SecurityAccess + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// TesterPresent service indicates to a server (or servers) that a client is still connected
        /// to the vehicle and that certain diagnostic services and/or communications
        /// that have been previously activated are to remain active.
        /// </summary>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendTesterPresent()
        {
            Console.WriteLine($"Service: {UDSApi.SvcTesterPresent_2013(handle, requestConfig, out UdsMessage outMessage, 0x00)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_TesterPresent + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// The ReadDataByIdentifier service allows the client to request data record values
        /// from the server identified by one or more dataIdentifiers.
        /// </summary>
        /// <param name="dataIdentifiers">buffer containing a list of two-byte Data Identifiers (see PUDS_SVC_PARAM_DI_xxx)</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendReadDataByIdentifier(UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER[] dataIdentifiers)
        {
            Console.WriteLine($"Service: {UDSApi.SvcReadDataByIdentifier_2013(handle, requestConfig, out UdsMessage outMessage, dataIdentifiers, (ushort)dataIdentifiers.Length)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_ReadDataByIdentifier + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// The ReadMemoryByAddress service allows the client to request memory data from the server
        /// via a provided starting address and to specify the size of memory to be read.
        /// </summary>
        /// <param name="memoryAddressBuffer">starting address buffer of server memory from which data is to be retrieved, maximum size is 0x0F</param>
        /// <param name="memorySizeBuffer">number of bytes to be read starting at the address specified by memory_address_buffer, maximum size is 0x0F</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendReadMemoryByAddress(byte[] memoryAddressBuffer, byte[] memorySizeBuffer)
        {
            Console.WriteLine($"Service: {UDSApi.SvcReadMemoryByAddress_2013(handle, requestConfig, out UdsMessage outMessage, memoryAddressBuffer, (byte)memoryAddressBuffer.Length, memorySizeBuffer, (byte)memorySizeBuffer.Length)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_ReadMemoryByAddress + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// The WriteDataByIdentifier service allows the client to write information into the server at an internal location
        /// specified by the provided data identifier.
        /// </summary>
        /// <param name="dataIdentifier">a two-byte Data Identifier (see PUDS_SVC_PARAM_DI_xxx)</param>
        /// <param name="dataBuffer">buffer containing the data to write</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendWriteDataByIdentifier(UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER dataIdentifier, byte[] dataBuffer)
        {
            Console.WriteLine($"Service: {UDSApi.SvcWriteDataByIdentifier_2013(handle, requestConfig, out UdsMessage outMessage, dataIdentifier, dataBuffer, (uint)dataBuffer.Length)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_WriteDataByIdentifier + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// The WriteMemoryByAddress service allows the client to write
        /// information into the server at one or more contiguous memory locations.
        /// </summary>
        /// <param name="memoryAddressBuffer">Starting address buffer of server memory to which data is to be written, maximum size is 0x0F</param>
        /// <param name="memorySizeBuffer">number of bytes to be written starting at the address specified by memory_address_buffer, maximum size is 0x0F</param>
        /// <param name="dataBuffer">buffer containing the data to write</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendWriteMemoryByAddress(byte[] memoryAddressBuffer, byte[] memorySizeBuffer, byte[] dataBuffer)
        {
            Console.WriteLine($"Service: {UDSApi.SvcWriteMemoryByAddress_2013(handle, requestConfig, out UdsMessage outMessage, memoryAddressBuffer, (byte)memoryAddressBuffer.Length, memorySizeBuffer, (byte)memorySizeBuffer.Length, dataBuffer, (uint)dataBuffer.Length)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_WriteMemoryByAddress + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// The ClearDiagnosticInformation service is used by the client to clear diagnostic information
        /// in one server's or multiple servers' memory.
        /// </summary>
        /// <param name="groupOfDtc">a three-byte value indicating the group of DTCs (e.g. powertrain, body, chassis)
        /// or the particular DTC to be cleared (see PUDS_SVC_PARAM_CDI_xxx)</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendClearDiagnosticInformation(uint groupOfDtc)
        {
            Console.WriteLine($"Service: {UDSApi.SvcClearDiagnosticInformation_2013(handle, requestConfig, out UdsMessage outMessage,groupOfDtc)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_ClearDiagnosticInformation + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// This service allows a client to read the status of server-resident Diagnostic Trouble Code (DTC) information.
        /// Only reportNumberOfDTCByStatusMask, reportDTCByStatusMask, reportMirrorMemoryDTCByStatusMask,
        /// reportNumberOfMirrorMemoryDTCByStatusMask, reportNumberOfEmissionsRelatedOBDDTCByStatusMask,
        /// reportEmissionsRelatedOBDDTCByStatusMask Sub-functions are allowed.
        /// </summary>
        /// <param name="readInformationType">Subfunction parameter: ReadDTCInformation type, use one of the following:
        /// PUDS_SVC_PARAM_RDTCI_RNODTCBSM, PUDS_SVC_PARAM_RDTCI_RDTCBSM,
        /// PUDS_SVC_PARAM_RDTCI_RMMDTCBSM, PUDS_SVC_PARAM_RDTCI_RNOMMDTCBSM,
        /// PUDS_SVC_PARAM_RDTCI_RNOOBDDTCBSM, PUDS_SVC_PARAM_RDTCI_ROBDDTCBSM</param>
        /// <param name="dtcStatusMask">Contains eight DTC status bit.</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendReadDTCInformation(UDSApi.UDS_SERVICE_PARAMETER_READ_DTC_INFORMATION_TYPE readInformationType, byte dtcStatusMask)
        {
            Console.WriteLine($"Service: {UDSApi.SvcReadDTCInformation_2013(handle, requestConfig, out UdsMessage outMessage, readInformationType, dtcStatusMask)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_ReadDTCInformation + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /* READ DTC INFORMATION: MANY CUSTOM FUNCTIONS IN UDS API??? */

        /// <summary>
        /// The RoutineControl service is used by the client to start/stop a routine,
        /// and request routine results.
        /// </summary>
        /// <param name="routineControlType">Subfunction parameter: RoutineControl type (see PUDS_SVC_PARAM_RC_xxx)</param>
        /// <param name="routineIdentifier">Server Local Routine Identifier (see PUDS_SVC_PARAM_RC_RID_xxx)</param>
        /// <param name="routineControlOptionBuffer">buffer containing the Routine Control Options (only with start and stop routine sub-functions)</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendRoutineControl(UDSApi.UDS_SERVICE_PARAMETER_ROUTINE_CONTROL routineControlType, UDSApi.UDS_SERVICE_PARAMETER_ROUTINE_CONTROL_IDENTIFIER routineIdentifier, byte[] routineControlOptionBuffer)
        {
            Console.WriteLine($"Service: {UDSApi.SvcRoutineControl_2013(handle, requestConfig, out UdsMessage outMessage, routineControlType, routineIdentifier, routineControlOptionBuffer, (uint)routineControlOptionBuffer.Length)}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_RoutineControl + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                    Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }
        #endregion

        #region ReceiveService
        public byte[] ReceiveService()
        {
            uint destinationAddress = NAI.DESTINATION_ADDRESS;
            AutoResetEvent receive_event = new(false);
            Console.WriteLine("Set server address: {0}", UDSApi.SetValue_2013(handle, UdsParameter.PUDS_PARAMETER_SERVER_ADDRESS, ref destinationAddress, sizeof(uint)));
            Console.WriteLine($"Receive event parameter: {UDSApi.SetValue_2013(handle, UdsParameter.PUDS_PARAMETER_RECEIVE_EVENT, BitConverter.GetBytes(receive_event.SafeWaitHandle.DangerousGetHandle().ToInt64()), sizeof(ulong))}");
            if (receive_event.WaitOne())
            {
                UdsStatus status = UDSApi.Read_2013(handle, out UdsMessage udsMessage);
                Console.WriteLine("Receive message: {0}", (status));
                if (UDSApi.StatusIsOk_2013(status))
                {
                    byte[] udsMessageByteArray = new byte[udsMessage.message.MessageDataAnyCopy.length];
                    Console.WriteLine("Message length: " + udsMessage.message.MessageDataAnyCopy.length);
                    if (udsMessage.message.MessageDataAnyCopy.length > 7)
                    {
                        receive_event.WaitOne();
                        UDSApi.MsgFree_2013(ref udsMessage);
                        UDSApi.Read_2013(handle, out udsMessage);
                    }
                    if (CanTpApi.getData_2016(ref udsMessage.message, 0, udsMessageByteArray, (int)udsMessage.message.MessageDataAnyCopy.length))
                    {
                        if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                            UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_DiagnosticSessionControl + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                        Console.WriteLine($"Write response message for service: {UDSApi.Write_2013(handle, ref service_response_msg)}");
                        Console.WriteLine($"Free response message: {UDSApi.MsgFree_2013(ref service_response_msg)}");
                        return udsMessageByteArray;
                    }
                }
            }
            return Array.Empty<byte>();
        }
		#endregion

		#region DiagnosticService
		public bool GetVersionInformation(out string versionString)
		{
			const int BUFFER_SIZE = 256;
            StringBuilder stringBuilder = new();
            if (UDSApi.StatusIsOk_2013(UDSApi.GetValue_2013(CantpHandle.PCANTP_HANDLE_NONEBUS, UdsParameter.PUDS_PARAMETER_API_VERSION, stringBuilder, BUFFER_SIZE)))
            {
                versionString = $"PCAN-UDS API Version: {stringBuilder}";
                return true;
            }
            else
            {
                versionString = "Failed to fetch API version";
                return false;
            }
		}
		#endregion

	}
}