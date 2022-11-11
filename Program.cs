using PCAN_UDS_TEST.DST_CAN;
using PCAN_UDS_TEST.PCAN;
using Peak.Can.IsoTp;
using Peak.Can.Uds;
using System.Collections.Generic;
using static PCAN_UDS_TEST.DST_CAN.DstUdsServiceHandler;
using static Peak.Can.Uds.UDSApi;
using DATA_IDENTIFIER = Peak.Can.Uds.UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER;

namespace PCAN_UDS_TEST
{
    internal class Program
	{
        #region PCAN_VARIABLES
        private static uint timeoutValue = 5000;
        private static readonly CantpHandle handle = CantpHandle.PCANTP_HANDLE_USBBUS1;
        private static readonly CantpBaudrate baudrate = CantpBaudrate.PCANTP_BAUDRATE_250K;
		private static readonly byte bodasSourceAddress = 0xFA;
		private static readonly byte bodasDestinationAddress = 0x01;
        private static readonly byte pcanUdsSourceAddress = 0xFA;
        private static readonly byte pcanUdsDestinationAddress = 0x03;
        #endregion

        #region DST_CAN_VARIABLES
        private static DstUdsHandler dstUdsHandler;
        private static readonly uint dstUdsSourceAddress = 0x18DA03FA;
        private static readonly uint dstUdsDestinationAddress = 0x18DAFA03;
        private static string portName = "COM3";
        #endregion

        #region PCAN_WRAPPERS
        private static bool PcanUninitialize(CantpHandle handle)
        {
            UdsStatus status = UDSApi.Uninitialize_2013(handle);
            Console.WriteLine($"CAN interface uninitialization: {status}");
            return UDSApi.StatusIsOk_2013(status);
        }

        private static bool PcanInitialize(CantpHandle handle, CantpBaudrate baudrate, uint timeoutValue)
        {
            UdsStatus status = UDSApi.Initialize_2013(handle, baudrate);
            Console.WriteLine($"CAN interface initialization: {status}");
            Console.WriteLine($"Set request timeout(ms): {UDSApi.SetValue_2013(handle, UdsParameter.PUDS_PARAMETER_TIMEOUT_REQUEST, ref timeoutValue, sizeof(uint))}");
            Console.WriteLine($"Set response timeout(ms): {UDSApi.SetValue_2013(handle, UdsParameter.PUDS_PARAMETER_TIMEOUT_RESPONSE, ref timeoutValue, sizeof(uint))}");
            return UDSApi.StatusIsOk_2013(status);
        }

        static void PrintControllerInformation(PCANBodasServiceHandler serviceHandler, DATA_IDENTIFIER[] controllerInformationIdentifiers)
		{
			if (serviceHandler.GetControllerInformation(controllerInformationIdentifiers, out List<string> dataList)) foreach (string str in dataList) Console.WriteLine(str);
		}

		static void PrintParameters(PCANBodasServiceHandler serviceHandler, DATA_IDENTIFIER[] dataIdentifiers)
		{
            Dictionary<string, Dictionary<string, List<Data>>> menuStructure = new();
            if (serviceHandler.GetMenus(DATA_IDENTIFIER.GET_PARAMETERS_MENUS, out List<string> menuStrings))
            {
                for (byte i = 0; i < menuStrings.Count; i++)
                {
                    if (menuStrings[i].Equals(string.Empty)) continue;
                    Dictionary<string, List<Data>> menuContents = new();
                    if (serviceHandler.GetSubMenus(i, out List<string> subMenuStringsList))
                    {
                        for (byte y = 0; y < subMenuStringsList.Count; y++)
                        {
                            if (subMenuStringsList[y].Equals(string.Empty)) continue;
                            List<Data> tempDataList = new();
                            while (tempDataList.Count < dataIdentifiers.Length)
                            {
                                if (serviceHandler.GetDataByIdentifiers(i, y, DATA_IDENTIFIER.SET_PARAMETERS_SUBMENU_CURSOR, dataIdentifiers, out byte[] byteArray))
                                    if (serviceHandler.GetDataFromByteArray(byteArray, 0x00, out List<Data> dataList)) tempDataList.AddRange(dataList);
                                    else
                                    {
                                        Console.WriteLine("Failed to parse data");
                                        break;
                                    }
                                else Console.WriteLine("Failed to get parameters");
                            }
                            menuContents.Add(subMenuStringsList[y], tempDataList);
                        }
                    }
                    else Console.WriteLine("Failed to get sub-menus");
                    menuStructure.Add(menuStrings[i], menuContents);
                }
            }
            else Console.WriteLine("Failed to get menus");

            foreach (KeyValuePair<string, Dictionary<string, List<Data>>> menu in menuStructure)
            {
                Console.WriteLine(menu.Key);
                foreach (KeyValuePair<string, List<Data>> subMenu in menu.Value)
                {
                    Console.WriteLine($"  |__ {subMenu.Key}");
                    foreach (Data data in subMenu.Value.Where(x => x.isAccessible)) Console.WriteLine($"  |  |__ {data}, DID: {data.dataIdentifier:X4}");
                }
            }
        }

        static void PrintProcessData(PCANBodasServiceHandler serviceHandler, DATA_IDENTIFIER[] processDataIdentifiers)
        {
            Dictionary<string, List<Data>> menuContents = new();
            if (serviceHandler.GetMenus(DATA_IDENTIFIER.GET_PROCESSDATA_MENUS, out List<string> menuStrings))
            {
                for (byte i = 0; i < menuStrings.Count; i++)
                {
                    List<Data> tempProcessDataList = new();
                    while (tempProcessDataList.Count < processDataIdentifiers.Length)
                    {
                        if (serviceHandler.GetDataByIdentifiers(i, null, DATA_IDENTIFIER.SET_PROCESSDATA_MENU_CURSOR, processDataIdentifiers, out byte[] byteArray))
                        {
                            if (serviceHandler.GetDataFromByteArray(byteArray, 0x80, out List<Data> processDataList)) tempProcessDataList.AddRange(processDataList);
                            else
                            {
                                Console.WriteLine("Failed to parse data");
                                break;
                            }
                        }
                        else Console.WriteLine("Failed to get parameters");
                    }
                    menuContents.Add(menuStrings[i], tempProcessDataList);
                }
            }

            foreach (var pair in menuContents)
            {
                Console.WriteLine($"{pair.Key}");
                foreach (Data data in pair.Value.Where(x => x.isAccessible)) Console.WriteLine($"  |__ {data}, DID: {data.dataIdentifier:X4}");
            }
        }

        static void PrintErrors(PCANBodasServiceHandler serviceHandler, DATA_IDENTIFIER dataIdentifier)
        {
            if (serviceHandler.GetErrors(dataIdentifier, out List<Error> errorList))
                foreach (Error error in errorList) Console.WriteLine(error);
        }

        static void ChangeControllerLanguage(PCANBodasServiceHandler serviceHandler, DATA_IDENTIFIER languageIdentifier)
        {
            serviceHandler.ChangeControllerLanguage(languageIdentifier);
        }

        static void SetParameter(PCANBodasServiceHandler serviceHandler, byte menuNumber, byte subMenuNumber, byte parameterNumber, short value)
        {
            serviceHandler.SetParameter(menuNumber, subMenuNumber, parameterNumber, value);
        }

        static void SoftResetECU(PCANBodasServiceHandler serviceHandler)
        {
            serviceHandler.SendEcuReset(UDSApi.UDS_SERVICE_PARAMETER_ECU_RESET.SOFT_RESET);
        }

        static void DiagnosticSessionControl(PCANBodasServiceHandler serviceHandler, UDSApi.UDS_SERVICE_DSC sessionType)
        {
            serviceHandler.SendDiagnosticSessionControl(sessionType);
        }

        static void SecurityAccessLevel(PCANBodasServiceHandler serviceHandler, byte accesslevel)
        {
            serviceHandler.UdsSetSecurityAccessLevel(accesslevel);
        }
        #endregion

        #region DST_CAN_WRAPPERS
        static void DstInitialize()
        {
            dstUdsHandler = new(portName, dstUdsSourceAddress, dstUdsDestinationAddress, 60000);
            dstUdsHandler.UdsMessageReceived += DebugReceiveDstUds;
        }

        static void DebugReceiveDstUds(DstUdsMessage udsMessage)
        {
            Console.Write($"{udsMessage.Size} - {udsMessage.SID} - ");
            foreach (byte b in udsMessage.Data) Console.Write($"{b:X2} ");
            Console.WriteLine();
        }
        #endregion

        static void Main(string[] args)
		{
            DATA_IDENTIFIER[] dataIdentifiers =
            {
                DATA_IDENTIFIER.GET_PARAMETER_0,
                DATA_IDENTIFIER.GET_PARAMETER_1,
                DATA_IDENTIFIER.GET_PARAMETER_2,
                DATA_IDENTIFIER.GET_PARAMETER_3,
                DATA_IDENTIFIER.GET_PARAMETER_4,
                DATA_IDENTIFIER.GET_PARAMETER_5,
                DATA_IDENTIFIER.GET_PARAMETER_6,
                DATA_IDENTIFIER.GET_PARAMETER_7
            };

            DATA_IDENTIFIER[] processDataIdentifiers =
            {
                DATA_IDENTIFIER.GET_PROCESSDATA_0,
                DATA_IDENTIFIER.GET_PROCESSDATA_1,
                DATA_IDENTIFIER.GET_PROCESSDATA_2,
                DATA_IDENTIFIER.GET_PROCESSDATA_3,
                DATA_IDENTIFIER.GET_PROCESSDATA_4,
                DATA_IDENTIFIER.GET_PROCESSDATA_5,
                DATA_IDENTIFIER.GET_PROCESSDATA_6,
                DATA_IDENTIFIER.GET_PROCESSDATA_7
            };

            DATA_IDENTIFIER[] controllerInformationIdentifiers =
            {
                DATA_IDENTIFIER.GET_SYSTEM_VOLTAGE,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_SSECUHWNDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_SSIDDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ADIDID,
                DATA_IDENTIFIER.GET_OPERATION_TIME_AND_RESET_COUNTER,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ASFPDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_SSECUHWVNDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ECUSNDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_BSIDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_BSFPDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ECUMDDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ADSDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_SNOETDID
            };

            List<MenuParameterMapping> menuParameterMappings = new()
            {
                new MenuParameterMapping { menuNumber = 0, parameterNumber = 0 },
                new MenuParameterMapping { menuNumber = 1, parameterNumber = 1 }
            };

            DstInitialize();
            DstUdsServiceHandler udsServiceHandler = new(dstUdsHandler);
            udsServiceHandler.Authenticate(UDS_SERVICE_DSC.ECU_EXTENDED_DIAGNOSTIC_SESSION, UDS_ACCESS_LEVEL.DEVELOPER);
            udsServiceHandler.ResetECU(UDSApi.UDS_SERVICE_PARAMETER_ECU_RESET.SOFT_RESET);
            //udsServiceHandler.UdsGetErrorsList(0x02, 0x01, out byte[] activeErrorsResponse);
            //Console.WriteLine("Errors:");
            //foreach (byte b in activeErrorsResponse) Console.Write($"{b:X2} ");

            //udsServiceHandler.UdsGetUnitcodes(out Dictionary<byte, string> units);
            //foreach (KeyValuePair<byte, string> kv in units) Console.Write($"{kv.Key} {kv.Value}");



            //PcanInitialize(handle, baudrate, timeoutValue);
            //UdsServiceHandler udsServiceHandler = new(handle, udsSourceAddress, udsDestinationAddress);
            //udsServiceHandler.UdsSendDiagnosticSessionControl(UDSApi.uds_svc_param_dsc.PUDS_SVC_PARAM_DSC_ECUEDS);
            //udsServiceHandler.UdsSetSecurityAccessLevel(0x03);
            //udsServiceHandler.UdsGetActiveErrors(out List<Error> errorList);
            //foreach (var err in errorList) Console.WriteLine(err.description);

            //udsServiceHandler.UdsGetListDescriptions(out List<ListEntry> listDescriptions);
            //foreach (var listEntry in listDescriptions)
            //    foreach (var listtext in listEntry.listEntries)
            //        Console.WriteLine($"{listEntry.address} - {listtext.Key} - {listtext.Value}");

            //udsServiceHandler.UdsGetUnitcodes(out Dictionary<byte, string> unitcodesList);
            //foreach (var unicode in unitcodesList) Console.WriteLine($"{unicode.Key} - {unicode.Value}");

            //udsServiceHandler.UdsGetProcessDataGroups(out Dictionary<byte, string> groupList);
            //Dictionary<byte, Dictionary<byte, Data>> parameterList = new();
            //foreach (KeyValuePair<byte, string> group in groupList)
            //{
            //    udsServiceHandler.UdsGetProcessData(group.Key, 0x08, out Dictionary<byte, Data> tempParameterList);
            //    parameterList.Add(group.Key, tempParameterList);
            //}

            //foreach (KeyValuePair<byte, Dictionary<byte, Data>> group in parameterList)
            //{
            //    Console.WriteLine($"{group.Key + 1} - {groupList[group.Key]}");
            //    foreach (KeyValuePair<byte, Data> parameter in group.Value)
            //        Console.WriteLine($"    {group.Key + 1}.{parameter.Key + 1} - {parameter.Value.name}");
            //}

            //int i = 0;
            //    udsServiceHandler.UdsGetParameterMenus(out Dictionary<byte, string> menuNames);
            //    Dictionary<byte, Dictionary<byte, string>> subMenuNames = new();
            //    Dictionary<byte, Dictionary<byte, Dictionary<byte, Data>>> parameters = new();


            //    foreach (KeyValuePair<byte, string> menu in menuNames)
            //    {
            //        if (i > 5) break;
            //        udsServiceHandler.UdsGetParameterSubmenus(menu.Key, out Dictionary<byte, string> tempSubMenuNames);
            //        subMenuNames.Add(menu.Key, tempSubMenuNames);
            //        parameters.Add(menu.Key, new Dictionary<byte, Dictionary<byte, Data>>());
            //        foreach (KeyValuePair<byte, string> subMenu in tempSubMenuNames)
            //        {
            //            udsServiceHandler.UdsGetParameters(menu.Key, subMenu.Key, 0x08, out Dictionary<byte, Data> tempParameters);
            //            parameters[menu.Key].Add(subMenu.Key, tempParameters);
            //        }
            //        i++;
            //    }
            //    foreach (KeyValuePair<byte, Dictionary<byte, Dictionary<byte, Data>>> menu in parameters)
            //    {
            //        Console.WriteLine($"{menu.Key + 1} - {menuNames[menu.Key]}");
            //        foreach (KeyValuePair<byte, Dictionary<byte, Data>> subMenu in menu.Value)
            //        {
            //            Console.WriteLine($"   {menu.Key + 1}.{subMenu.Key + 1} - {subMenuNames[menu.Key][subMenu.Key]}");
            //            foreach (KeyValuePair<byte, Data> parameter in subMenu.Value)
            //                Console.WriteLine($"        {menu.Key + 1}.{subMenu.Key + 1}.{parameter.Key + 1} - {parameter.Value.name}");
            //        }
            //    }

            //    udsServiceHandler.UdsSendDiagnosticSessionControl(UDSApi.uds_svc_param_dsc.PUDS_SVC_PARAM_DSC_ECUEDS);
            //    udsServiceHandler.UdsSetSecurityAccessLevel(0x0D);
            //    i = 0;
            //    foreach (KeyValuePair<byte, Dictionary<byte, Dictionary<byte, Data>>> menu in parameters)
            //    {
            //        if (i > 5) break;
            //        foreach (KeyValuePair<byte, Dictionary<byte, Data>> subMenu in menu.Value)
            //        {
            //            foreach (KeyValuePair<byte, Data> parameter in subMenu.Value.Where(x => x.Value.isAccessible == false))
            //            {
            //                udsServiceHandler.UdsGetParameters(menu.Key, subMenu.Key, parameter.Key, out Dictionary<byte, Data> tempParameters);
            //                parameters[menu.Key][subMenu.Key][parameter.Key] = tempParameters[parameter.Key];
            //            }
            //        }
            //        i++;
            //    }
            //    foreach (KeyValuePair<byte, Dictionary<byte, Dictionary<byte, Data>>> menu in parameters)
            //    {
            //        Console.WriteLine($"{menu.Key + 1} - {menuNames[menu.Key]}");
            //        foreach (KeyValuePair<byte, Dictionary<byte, Data>> subMenu in menu.Value)
            //        {
            //            Console.WriteLine($"   {menu.Key + 1}.{subMenu.Key + 1} - {subMenuNames[menu.Key][subMenu.Key]}");
            //            foreach (KeyValuePair<byte, Data> parameter in subMenu.Value)
            //                Console.WriteLine($"        {menu.Key + 1}.{subMenu.Key + 1}.{parameter.Key + 1} - {parameter.Value.name}");
            //        }
            //    }

            //var watch = System.Diagnostics.Stopwatch.StartNew();
            //foreach (KeyValuePair<byte, Dictionary<byte, Dictionary<byte, Data>>> menu in parameters)
            //    foreach (KeyValuePair<byte, Dictionary<byte, Data>> subMenu in menu.Value)
            //        foreach (KeyValuePair<byte, Data> parameter in subMenu.Value)
            //            udsServiceHandler.UdsGetDataByIdentifiers(new DATA_IDENTIFIER[] { (DATA_IDENTIFIER)(10000 + 100 * menu.Key + 10 * subMenu.Key + parameter.Key) }, out byte[] byteArray);
            //watch.Stop();
            //Console.WriteLine($"Elapsed: {watch.ElapsedMilliseconds / 1000}s");

            //Uninitialize(handle);
        }
	}
}