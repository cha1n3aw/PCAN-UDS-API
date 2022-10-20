﻿using PCAN_UDS_TEST;
using Peak.Can.IsoTp;
using Peak.Can.Uds;
using System.Runtime.ConstrainedExecution;
using DATA_IDENTIFIER = Peak.Can.Uds.UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER;

namespace BodAss
{
    internal class Program
	{
        private static uint timeoutValue = 5000;
        private static readonly CantpHandle handle = CantpHandle.PCANTP_HANDLE_USBBUS1;
        private static readonly CantpBaudrate baudrate = CantpBaudrate.PCANTP_BAUDRATE_250K;
		private static readonly byte sourceAddress = 0xFA;
		private static readonly byte destinationAddress = 0x01;

		static void PrintControllerInformation(ServiceHandler serviceHandler, DATA_IDENTIFIER[] controllerInformationIdentifiers)
		{
			if (serviceHandler.GetControllerInformation(controllerInformationIdentifiers, out List<string> dataList)) foreach (string str in dataList) Console.WriteLine(str);
		}

		static void PrintParameters(ServiceHandler serviceHandler, DATA_IDENTIFIER[] dataIdentifiers)
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

        static void PrintProcessData(ServiceHandler serviceHandler, DATA_IDENTIFIER[] processDataIdentifiers)
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

        static void PrintErrors(ServiceHandler serviceHandler, DATA_IDENTIFIER dataIdentifier)
        {
            if (serviceHandler.GetErrors(dataIdentifier, out List<Error> errorList))
                foreach (Error error in errorList) Console.WriteLine(error);
        }

        static void PrintLiveData(ServiceHandler serviceHandler)
        {
			while (!Console.KeyAvailable)
			{
				serviceHandler.LiveUpdateParameters(out List<LiveData> dataList);
				foreach (LiveData data in dataList)
				{
					Console.WriteLine($"{data.dataIdentifier:X4}: {data.value:X4}");
				}
			}
		}

		static void Main(string[] args)
		{
            DATA_IDENTIFIER[] dataIdentifiers = {
                DATA_IDENTIFIER.GET_PARAMETER_0,
                DATA_IDENTIFIER.GET_PARAMETER_1,
                DATA_IDENTIFIER.GET_PARAMETER_2,
                DATA_IDENTIFIER.GET_PARAMETER_3,
                DATA_IDENTIFIER.GET_PARAMETER_4,
                DATA_IDENTIFIER.GET_PARAMETER_5,
                DATA_IDENTIFIER.GET_PARAMETER_6,
                DATA_IDENTIFIER.GET_PARAMETER_7 };

            DATA_IDENTIFIER[] processDataIdentifiers = {
                DATA_IDENTIFIER.GET_PROCESSDATA_0,
                DATA_IDENTIFIER.GET_PROCESSDATA_1,
                DATA_IDENTIFIER.GET_PROCESSDATA_2,
                DATA_IDENTIFIER.GET_PROCESSDATA_3,
                DATA_IDENTIFIER.GET_PROCESSDATA_4,
                DATA_IDENTIFIER.GET_PROCESSDATA_5,
                DATA_IDENTIFIER.GET_PROCESSDATA_6,
                DATA_IDENTIFIER.GET_PROCESSDATA_7 };

            DATA_IDENTIFIER[] controllerInformationIdentifiers = {
                DATA_IDENTIFIER.GET_SYSTEM_VOLTAGE,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_SSECUHWNDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_SSIDDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ADIDID,
                DATA_IDENTIFIER.GET_UNKNOWN_DATA, // operation time and reset counter
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ASFPDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_SSECUHWVNDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ECUSNDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_BSIDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_BSFPDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ECUMDDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_ADSDID,
                DATA_IDENTIFIER.PUDS_SVC_PARAM_DI_SNOETDID };

            Initialize(handle, baudrate, timeoutValue);
            ServiceHandler serviceHandler = new(handle, sourceAddress, destinationAddress);

            byte value = 0x50;
			//Console.WriteLine($"{0xA0 + (value ^ 8):X2} "); //*.*.1 !
			Console.WriteLine($"{0xC0 + (value ^ 11):X2} "); //*.*.2  ?
			Console.WriteLine($"{0x60 + (value ^ 14):X2} "); //*.*.3  ?
			Console.WriteLine($"{0x00 + (value ^ 13):X2} "); //*.*.4  !
            Console.WriteLine($"{0x00 + (value ^ 5):X2} "); //*.*.5   !
            Console.WriteLine($"{0x60 + (value ^ 6):X2} "); //*.*.6   ?


			//Console.WriteLine(serviceHandler.ChangeControllerLanguage(0x01));
            //PrintParameters(serviceHandler, dataIdentifiers);
            //PrintLiveData(serviceHandler);

            ////PrintControllerInformation(serviceHandler, controllerInformationIdentifiers);
            ////PrintParameters(serviceHandler, dataIdentifiers);
            ////PrintProcessData(serviceHandler, processDataIdentifiers);
            ////PrintErrors(serviceHandler, DATA_IDENTIFIER.GET_ACTIVE_ERRORS);
            ////PrintErrors(serviceHandler, DATA_IDENTIFIER.GET_SAVED_ERRORS);
            Uninitialize(handle);

        }

		private static bool Uninitialize(CantpHandle handle)
		{
			UdsStatus status = UDSApi.Uninitialize_2013(handle);
            Console.WriteLine($"CAN interface uninitialization: {status}");
			return UDSApi.StatusIsOk_2013(status);
        }

		private static bool Initialize(CantpHandle handle, CantpBaudrate baudrate, uint timeoutValue)
		{
			UdsStatus status = UDSApi.Initialize_2013(handle, baudrate);
            Console.WriteLine($"CAN interface initialization: {status}");
            Console.WriteLine($"Set request timeout(ms): {UDSApi.SetValue_2013(handle, UdsParameter.PUDS_PARAMETER_TIMEOUT_REQUEST, ref timeoutValue, sizeof(uint))}");
            Console.WriteLine($"Set response timeout(ms): {UDSApi.SetValue_2013(handle, UdsParameter.PUDS_PARAMETER_TIMEOUT_RESPONSE, ref timeoutValue, sizeof(uint))}");
            return UDSApi.StatusIsOk_2013(status);
		}
	}
}