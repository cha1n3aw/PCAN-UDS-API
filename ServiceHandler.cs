using Peak.Can.IsoTp;
using Peak.Can.Uds;
using System.Runtime.InteropServices;

namespace PCAN_UDS_TEST
{
    public class ServiceHandler
    {
        private readonly CantpHandle handle;
        private UdsNetAddressInfo NAI;
        private UdsMessageConfig requestConfig;
        private UdsMessageConfig responseConfig;

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

        public bool SendService()
        {
            //string messageString = string.Empty;
            //Console.WriteLine($"Service: {UDSApi.SvcDiagnosticSessionControl_2013(handle, requestConfig, out UdsMessage outMessage, UDSApi.uds_svc_param_dsc.PUDS_SVC_PARAM_DSC_DS)}");
            //UdsStatus responseStatus = UDSApi.WaitForService_2013(handle, ref outMessage, out UdsMessage udsMessageResponse, out _);
            //Console.WriteLine($"WaitForService: {responseStatus}");
            //if (UDSApi.StatusIsOk_2013(responseStatus) && !udsMessageResponse.Equals(null) && !udsMessageResponse.message.Equals(null) && udsMessageResponse.message.MessageDataAnyCopy.length != 0)
            //{
            //    for (int i = 0; i < udsMessageResponse.message.MessageDataAnyCopy.length; i++)
            //        messageString += $"{Marshal.ReadByte(udsMessageResponse.message.MessageDataAnyCopy.Data + i):X2} ";
            //    Console.WriteLine(messageString);
            //    return true;
            //}
            //else
            //{
            //    Console.WriteLine("Corrupted response, no message received");
            //    return false;
            //}
        }

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
    }
}