using Peak.Can.IsoTp;
using Peak.Can.Uds;
using System.Linq.Expressions;
using System.Text;
using DATA_IDENTIFIER = Peak.Can.Uds.UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER;

namespace PCAN_UDS_TEST
{
    #region Structs
    public struct ListEntry
    {
        public byte address;
        public Dictionary<byte, string> listEntries;
    }
    #endregion

    public class UdsServiceHandler
    {
        #region GlobalParameters
        private readonly CantpHandle handle;
        private UdsNetAddressInfo NAI;
        private UdsMessageConfig requestConfig;
        private UdsMessageConfig responseConfig;
        #endregion

        #region Constructor
        public UdsServiceHandler(CantpHandle handle, byte sourceAddress, byte destinationAddress)
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

        #region UdsServiceWrappers
        public bool UdsEcuReset(UDSApi.UDS_SERVICE_PARAMETER_ECU_RESET resetParameter)
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

        public bool UdsGetErrorsList(UDSApi.UDS_SERVICE_PARAMETER_READ_DTC_INFORMATION_TYPE dtcType, byte statusMask, out byte[] response)
        {
            response = Array.Empty<byte>();
            try
            {
                response = SendReadDTCInformation(dtcType, statusMask);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UdsSendDiagnosticSessionControl(UDSApi.uds_svc_param_dsc sessionType)
        {
            byte[] response = SendDiagnosticSessionControl(sessionType);
            return true;
        }

        public bool UdsSetSecurityAccessLevel(byte accessLevel)
        {
            try
            {
                byte[] response = SendSecurityAccess(accessLevel);
                byte[] responseSeed = new byte[response.Length - 2];
                Array.Copy(response, 2, responseSeed, 0, response.Length - 2);
                Array.Resize(ref responseSeed, 16);
                SecurityAccess securityAccess = new();
                byte[] key = securityAccess.GetKey(responseSeed, accessLevel);
                response = SendSecurityAccessWithData((byte)(accessLevel + 1), key);
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
                dataArray = SendReadDataByIdentifier(dataIdentifiers);
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
                byte[] dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1220 });
                byte numberOfMenus = dataArray[3];
                for (int i = 4; ; i++)
                {
                    byte address = dataArray[i++];
                    string menuName = string.Empty;
                    while (dataArray[i] != 0x00) menuName += (char)dataArray[i++];
                    menuList.Add(address, menuName);
                    if (menuList.Count == numberOfMenus) break;
                    if (i == dataArray.Length - 1)
                    {
                        dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1220 });
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

        public bool UdsGetParameterSubmenus(byte menuAddress, out Dictionary<byte, string> submenuList)
        {
            submenuList = new();
            try
            {
                int i = 3;
                byte[] dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x1221 + menuAddress) });
                byte numberOfSubmenus = dataArray[i++];
                if (dataArray[i + 1] == 0x00) return true;
                for (;; i++)
                {
                    byte address = dataArray[i++];
                    string submenuName = string.Empty;
                    while (dataArray[i] != 0x00) submenuName += (char)dataArray[i++];
                    submenuList.Add(address, submenuName);
                    if (submenuList.Count == numberOfSubmenus) break;
                    if (i == dataArray.Length - 1)
                    {
                        dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x1221 + menuAddress) });
                        i = 3;
                    }
                }
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public bool UdsGetProcessDataGroups(out Dictionary<byte, string> groupList)
        {
            groupList = new();
            try
            {
                byte[] dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1261 });
                byte numberOfGroups = dataArray[3];
                for (int i = 4; ; i++)
                {
                    byte address = dataArray[i++];
                    string groupName = string.Empty;
                    while (dataArray[i] != 0x00) groupName += (char)dataArray[i++];
                    groupList.Add(address, groupName);
                    if (groupList.Count == numberOfGroups) break;
                    if (i == dataArray.Length - 1)
                    {
                        dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1261 });
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

        public bool UdsGetProcessData(byte groupAddress, out Dictionary<byte, Data> parameterList)
        {
            parameterList = new();
            try
            {
                int i = 3;
                byte[] dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x1200 + groupAddress) });
                byte numberOfParameters = dataArray[i++];
                if (dataArray[i + 1] == 0x00) return true;
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    if (dataArray[i + 1] == 0x00)
                    {
                        parameterList.Add(address, new Data());
                        i++;
                        continue;
                    }
                    Data data = new();
                    while (dataArray[i] != 0x00) data.name += (char)dataArray[i++];
                    i++;
                    data.multiplier = (ushort)((dataArray[i++] << 8) | dataArray[i++]);
                    data.divisor = (ushort)((dataArray[i++] << 8) | dataArray[i++]);
                    data.valueType = dataArray[i++];
                    data.digits = (ushort)((dataArray[i++] << 8) | dataArray[i++]);
                    data.unitCode = dataArray[i++];
                    data.accessLevel = dataArray[i++];
                    parameterList.Add(address, data);
                    if (parameterList.Count == numberOfParameters) break;
                    if (i == dataArray.Length - 1)
                    {
                        dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x1200 + groupAddress) });
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

        public bool UdsGetParameters(byte menuAddress, byte subMenuAddress, out Dictionary<byte, Data> parameterList)
        {
            parameterList = new();
            try
            {
                int i = 3;
                byte[] dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x1000 + (menuAddress << 3) + subMenuAddress) });
                foreach (byte b in dataArray) Console.Write($"{b:X2} ");
                Console.WriteLine();
                byte numberOfParameters = dataArray[i++];
                if (dataArray[i + 1] == 0x00) return true;
                for (; ; i++)
                {
                    byte address = dataArray[i++];
                    if (dataArray[i + 1] == 0x00)
                    {
                        parameterList.Add(address, new Data());
                        i++;
                        continue;
                    }
                    Data data = new();
                    while (dataArray[i] != 0x00) data.name += (char)dataArray[i++];
                    i++;
                    data.value = (short)((dataArray[i++] << 8) | dataArray[i++]);
                    data.multiplier = (ushort)((dataArray[i++] << 8) | dataArray[i++]);
                    data.divisor = (ushort)((dataArray[i++] << 8) | dataArray[i++]);
                    data.digits = (ushort)((dataArray[i++] << 8) | dataArray[i++]);
                    data.valueType = dataArray[i++];
                    data.minValue = (short)((dataArray[i++] << 8) | dataArray[i++]);
                    data.maxValue = (short)((dataArray[i++] << 8) | dataArray[i++]);
                    data.step = (ushort)((dataArray[i++] << 8) | dataArray[i++]);
                    data.defaultValue = (short)((dataArray[i++] << 8) | dataArray[i++]);
                    data.unitCode = dataArray[i++];
                    data.eepromPage = dataArray[i++];
                    data.eepromAddress = dataArray[i++];
                    parameterList.Add(address, data);
                    if (parameterList.Count == numberOfParameters) break;
                    if (i == dataArray.Length - 1)
                    {
                        dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(0x1000 + (menuAddress << 3) + subMenuAddress) });
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

        public bool UdsGetUnitcodes(out Dictionary<byte, string> unitcodesList)
        {
            unitcodesList = new();
            try
            {
                int i = 3;
                byte[] dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1262 });
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
                        dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1262 });
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

        public bool UdsGetListDescriptions(out List<ListEntry> listDescriptions)
        {
            listDescriptions = new();
            try
            {
                int i = 3;
                byte[] dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1263 });
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
                        dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1263 });
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

        public bool UdsGetSavedErrors(out List<Error> errorList)
        {
            errorList = new();
            try
            {
                byte[] dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1264 });
                byte numberOfErrors = dataArray[3];
                for (int i = 4; ; i++)
                {
                    byte address = dataArray[i++];
                    ushort code = (ushort)((dataArray[i++] << 8) + dataArray[i++]);
                    byte parameter = dataArray[i++];
                    byte occurence = dataArray[i++];
                    uint timestamp = (uint)((dataArray[i++] << 24) + (dataArray[i++] << 16) + (dataArray[i++] << 8) + dataArray[i++]);
                    string description = string.Empty;
                    while (dataArray[i] != 0x00) description += (char)dataArray[i++];
                    errorList.Add(new Error() { errorCode = code, occurence = occurence, parameter = parameter, description = description, timestamp = timestamp });
                    if (errorList.Count == numberOfErrors) break;
                    if (i == dataArray.Length - 1)
                    {
                        dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1264 });
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

        public bool UdsGetActiveErrors(out List<Error> errorList)
        {
            errorList = new();
            try
            {
                byte[] dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1264 });
                byte numberOfErrors = dataArray[3];
                for (int i = 4; ; i++)
                {
                    byte address = dataArray[i++];
                    ushort code = (ushort)((dataArray[i++] << 8) + dataArray[i++]);
                    byte parameter = dataArray[i++];
                    byte occurence = dataArray[i++];
                    uint timestamp = (uint)((dataArray[i++] << 24) + (dataArray[i++] << 16) + (dataArray[i++] << 8) + dataArray[i++]);
                    string description = string.Empty;
                    while (dataArray[i] != 0x00) description += (char)dataArray[i++];
                    errorList.Add(new Error() { errorCode = code, occurence = occurence, parameter = parameter, description = description, timestamp = timestamp });
                    if (errorList.Count == numberOfErrors) break;
                    if (i == dataArray.Length - 1)
                    {
                        dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)0x1264 });
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

        private DATA_IDENTIFIER UdsGetParameterIdentifier(byte menuNumber, byte subMenuNumber, byte parameterNumber) => (DATA_IDENTIFIER)(10000 + 100 * menuNumber + 10 * subMenuNumber + parameterNumber);

        private ushort UdsGetProcessDataIdentifier(byte groupNumber, byte parameterNumber) => (ushort)(20000 + 10 * groupNumber + parameterNumber);

        public bool GetDataByIdentifiers(byte menuNumber, byte subMenuNumber, byte parameterNumber, out byte[] dataArray)
        {
            dataArray = Array.Empty<byte>();
            try
            {
                dataArray = SendReadDataByIdentifier(new DATA_IDENTIFIER[] { UdsGetParameterIdentifier(menuNumber, subMenuNumber, parameterNumber) });
                if (dataArray != null && dataArray != Array.Empty<byte>()) return true;
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion

        #region SendServices
        /// <summary>
        /// The DiagnosticSessionControl service is used to enable different diagnostic sessions in the server.
        /// </summary>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendDiagnosticSessionControl(UDSApi.uds_svc_param_dsc authParameter)
        {
            Console.Write("DSC pending: ");
            //Console.WriteLine($"Service: {
                UDSApi.SvcDiagnosticSessionControl_2013(handle, requestConfig, out UdsMessage outMessage, authParameter); //}");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            //Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_DiagnosticSessionControl + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    //Console.WriteLine($"Write response message for service: {
                        UDSApi.Write_2013(handle, ref service_response_msg);//}");
                    //Console.WriteLine($"Free response message: {
                        UDSApi.MsgFree_2013(ref service_response_msg);//}");
                    Console.WriteLine("OK");
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
        /// <param name="securityAccessLevel">Subfunction parameter: type of SecurityAccess (see PUDS_SVC_PARAM_SA_xxx)</param>
        /// If Sending Key, data holds the value generated by the security algorithm corresponding to a specific "seed" value</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendSecurityAccess(byte securityAccessLevel)
        {
            Console.Write("Security access pending: ");
            //Console.WriteLine($"Service: {
            UDSApi.SvcSecurityAccess_2013(handle, requestConfig, out UdsMessage outMessage, securityAccessLevel);// }");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            //Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_SecurityAccess + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    //Console.WriteLine($"Write response message for service: {
                        UDSApi.Write_2013(handle, ref service_response_msg);//}");
                    //Console.WriteLine($"Free response message: {
                        UDSApi.MsgFree_2013(ref service_response_msg);//}");
                    Console.WriteLine("OK");
                    return udsMessageByteArray;
                }
            }
            Console.WriteLine("Corrupted response, no message received");
            return Array.Empty<byte>();
        }

        /// <summary>
        /// SendSecurityAccessWithData service provides a means to access data and/or diagnostic services which have
        /// restricted access for security, emissions or safety reasons.
        /// </summary>
        /// <param name="securityAccessType">Subfunction parameter: type of SecurityAccess (see PUDS_SVC_PARAM_SA_xxx)</param>
        /// <param name="securityAccessData">If Requesting Seed, buffer is the optional data to transmit to a server (like identification).
        /// If Sending Key, data holds the value generated by the security algorithm corresponding to a specific "seed" value</param>
        /// <returns>Byte array that contains response message</returns>
        public byte[] SendSecurityAccessWithData(byte securityAccessType, byte[] securityAccessData)
        {
            Console.Write("Security access w/data pending: ");
            //Console.WriteLine($"Service: {
            UDSApi.SvcSecurityAccess_2013(handle, requestConfig, out UdsMessage outMessage, securityAccessType, securityAccessData, (uint)securityAccessData.Length);// }");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            //Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_SecurityAccess + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    //Console.WriteLine($"Write response message for service: {
                        UDSApi.Write_2013(handle, ref service_response_msg);//}");
                    //Console.WriteLine($"Free response message: {
                        UDSApi.MsgFree_2013(ref service_response_msg);//}");
                    Console.WriteLine("OK");
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
        public byte[] SendReadDataByIdentifier(DATA_IDENTIFIER[] dataIdentifiers)
        {
            Console.Write("Read data service pending: ");
            //Console.WriteLine($"Service: {
            UDSApi.SvcReadDataByIdentifier_2013(handle, requestConfig, out UdsMessage outMessage, dataIdentifiers, (ushort)dataIdentifiers.Length);// }");
            UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            //Console.WriteLine($"WaitForService: {responseStatus}");
            if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            {
                byte[] udsMessageByteArray = new byte[udsMessageResponse.message.MessageDataAnyCopy.length];
                if (CanTpApi.getData_2016(ref udsMessageResponse.message, 0, udsMessageByteArray, (int)udsMessageResponse.message.MessageDataAnyCopy.length))
                {
                    if (UDSApi.StatusIsOk_2013(UDSApi.MsgAlloc_2013(out UdsMessage service_response_msg, responseConfig, 1)))
                        UDSApi.SetDataServiceId_2013(ref service_response_msg, (byte)UDS_SERVICE.PUDS_SERVICE_SI_ReadDataByIdentifier + UDSApi.PUDS_SI_POSITIVE_RESPONSE);
                    //Console.WriteLine($"Write response message for service: {
                    UDSApi.Write_2013(handle, ref service_response_msg);//}");
                    //Console.WriteLine($"Free response message: {
                    UDSApi.MsgFree_2013(ref service_response_msg);//}");
                    Console.WriteLine("OK");
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
        public byte[] SendWriteDataByIdentifier(DATA_IDENTIFIER dataIdentifier, byte[] dataBuffer)
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
            Console.WriteLine($"Service: {UDSApi.SvcClearDiagnosticInformation_2013(handle, requestConfig, out UdsMessage outMessage, groupOfDtc)}");
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

        #region ManualUds
        public bool SendUSDT(byte[] message)
        {
            uint MSG_SIZE = (uint)message.Length;
            UdsStatus status = UDSApi.MsgAlloc_2013(out UdsMessage udsMessage, requestConfig, MSG_SIZE);
            bool result = false;
            if (UDSApi.StatusIsOk_2013(status))
            {
                byte[] txByteArray = new byte[MSG_SIZE];
                for (int i = 0; i < MSG_SIZE; i++) txByteArray[i] = message[i];
                result = CanTpApi.setData_2016(ref udsMessage.message, 0, txByteArray, (int)MSG_SIZE);
            }
            Console.WriteLine($"Allocate TX message: {UDSApi.StatusIsOk_2013(status) && result}");

            UdsStatus resultStatus = UDSApi.Write_2013(handle, ref udsMessage);
            if (UDSApi.StatusIsOk_2013(resultStatus))
            {

                Thread.Sleep(100);
                Console.WriteLine("Write succeeded");
            }
            else Console.WriteLine("Write error: " + result, "Error");
            Console.WriteLine($"Free TX message: {UDSApi.MsgFree_2013(ref udsMessage)}");
            return true;
        }

        public byte[] ReceiveUSDT()
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

        #region PcanDiagnosticService
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