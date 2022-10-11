using PCAN_UDS_TEST;
using Peak.Can.IsoTp;
using Peak.Can.Uds;

//TODO: basic info & errors

namespace BodAss
{
    internal class Program
	{
        private static uint tiomeoutValue = 5000;
        private static readonly CantpHandle handle = CantpHandle.PCANTP_HANDLE_USBBUS1;
        private static readonly CantpBaudrate baudrate = CantpBaudrate.PCANTP_BAUDRATE_250K;
		private static readonly byte sourceAddress = 0xFA;
		private static readonly byte destinationAddress = 0x01;

		static void Main(string[] args)
		{
            Initialize(handle, baudrate);
            ServiceHandler serviceHandler = new(handle, sourceAddress, destinationAddress);
            UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER[] dataIdentifiers = {
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PARAMETER_0,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PARAMETER_1,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PARAMETER_2,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PARAMETER_3,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PARAMETER_4,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PARAMETER_5,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PARAMETER_6,
				UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PARAMETER_7 };
			UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER[] processDataIdentifiers = {
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PROCESSDATA_0,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PROCESSDATA_1,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PROCESSDATA_2,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PROCESSDATA_3,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PROCESSDATA_4,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PROCESSDATA_5,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PROCESSDATA_6,
                UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PROCESSDATA_7 };

			Dictionary<string, List<Data>> menuContents = new();
			if (serviceHandler.GetMenus(UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.GET_PROCESSDATA_MENUS, out List<string> menuStrings))
			{
				for (byte i = 0; i < menuStrings.Count; i++)
				{
					List<Data> tempProcessDataList = new();
					while (tempProcessDataList.Count < processDataIdentifiers.Length)
					{
						if (serviceHandler.GetDataByIdentifiers(i, null, UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER.SET_PROCESSDATA_MENU_CURSOR, processDataIdentifiers, out byte[] byteArray))
						{
							if (serviceHandler.GetDataFromByteArray(byteArray, 0x80, out List<Data> processDataList))
							{
								if (i == 5) foreach (byte b in byteArray) Console.Write($"{b:X2} ");
								Console.WriteLine();
								tempProcessDataList.AddRange(processDataList);
							}
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


			//       Dictionary<string, Dictionary<string, List<Data>>> menuStructure = new();
			//if (serviceHandler.GetMenus(0xFE00, out List<string> menuStrings))
			//{
			//	for (byte i = 0; i < menuStrings.Count; i++)
			//	{
			//		if (menuStrings[i].Equals(string.Empty)) continue;
			//		Dictionary<string, List<Data>> menuContents = new();
			//		if (serviceHandler.GetSubMenus(i, out List<string> subMenuStringsList))
			//		{
			//			for (byte y = 0; y < subMenuStringsList.Count; y++)
			//			{
			//				if (subMenuStringsList[y].Equals(string.Empty)) continue;
			//				List<Data> tempDataList = new();
			//				while (tempDataList.Count < dataIdentifiers.Count)
			//				{
			//					if (serviceHandler.GetDataByIdentifiers(i, y, dataIdentifiers.Cast<UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER>().ToArray(), out byte[] byteArray))
			//						if (serviceHandler.GetDataFromByteArray(byteArray, 0x00, out List<Data> dataList)) tempDataList.AddRange(dataList);
			//						else
			//						{
			//							Console.WriteLine("Failed to parse data");
			//							break;
			//						}
			//					else Console.WriteLine("Failed to get parameters");
			//				}
			//				menuContents.Add(subMenuStringsList[y], tempDataList);
			//			}
			//		}
			//		else Console.WriteLine("Failed to get sub-menus");
			//		menuStructure.Add(menuStrings[i], menuContents);
			//	}
			//}
			//else Console.WriteLine("Failed to get menus");

			//       foreach (KeyValuePair<string, Dictionary<string, List<Data>>> menu in menuStructure)
			//       {
			//           Console.WriteLine(menu.Key);
			//           foreach (KeyValuePair<string, List<Data>> subMenu in menu.Value)
			//           {
			//               Console.WriteLine($"  |__ {subMenu.Key}");
			//               foreach (Data data in subMenu.Value.Where(x => x.isAccessible)) Console.WriteLine($"  |  |__ {data}, DID: {data.dataIdentifier:X4}");
			//           }
			//       }

			Uninitialize(handle);
        }

		private static bool Uninitialize(CantpHandle handle)
		{
			UdsStatus status = UDSApi.Uninitialize_2013(handle);
            Console.WriteLine($"CAN interface uninitialization: {status}");
			return UDSApi.StatusIsOk_2013(status);
        }

		private static bool Initialize(CantpHandle handle, CantpBaudrate baudrate)
		{
			UdsStatus status = UDSApi.Initialize_2013(handle, baudrate);
            Console.WriteLine($"CAN interface initialization: {status}");
            Console.WriteLine($"Set request timeout(ms): {UDSApi.SetValue_2013(handle, UdsParameter.PUDS_PARAMETER_TIMEOUT_REQUEST, ref tiomeoutValue, sizeof(uint))}");
            Console.WriteLine($"Set response timeout(ms): {UDSApi.SetValue_2013(handle, UdsParameter.PUDS_PARAMETER_TIMEOUT_RESPONSE, ref tiomeoutValue, sizeof(uint))}");
            return UDSApi.StatusIsOk_2013(status);
		}
	}
}