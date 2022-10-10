﻿using PCAN_UDS_TEST;
using Peak.Can.IsoTp;
using Peak.Can.Uds;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleApp2
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
			/*
			Initialize(handle, baudrate);
            ServiceHandler serviceHandler = new(handle, sourceAddress, destinationAddress);
			//if (!serviceHandler.GetSubMenus(0, out List<string> subMenuStrings)) return;
			//Console.WriteLine(subMenuStrings);

			//byte[] byteArray = serviceHandler.SendReadDataByIdentifier(new UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER[] { (UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0xFE02 });

			List<string> menustrings;
			List<List<string>> submenustrings = new();

			if (!serviceHandler.GetMenus(out List<string> menuStrings)) Console.WriteLine("Failed to get menus");
			else
			{
				menustrings = menuStrings;
				for (byte i = 0; i < menuStrings.Count; i++)
				{
					//Console.WriteLine(menuStrings[i]);
					if (!serviceHandler.GetSubMenus(i, out List<string> subMenuStrings)) Console.WriteLine("Failed to get sub-menus");
					else
					{
						submenustrings.Add(subMenuStrings);
						for (byte y = 0; y < subMenuStrings.Count; y++)
						{
							//Console.WriteLine(subMenuStrings[y]);
							if (!serviceHandler.GetDataByIdentifiers(out byte[] byteArray)) Console.WriteLine("Failed to get parameters");
							//if (!serviceHandler.GetDataFromByteArray(byteArray, out List<Data> dataArray)) Console.WriteLine("Failed to parse data");
							//for (int x = 0; x < dataArray.Count; x++)
							//	Console.WriteLine($"{dataArray[x].name} {dataArray[x].value}");
						}
					}
				}
			}



			for (byte i = 0; i < menuStrings.Count; i++)
			{
				Console.WriteLine(menuStrings[i]);
				//if (!serviceHandler.GetSubMenus(i, out List<string> subMenuStrings)) Console.WriteLine("Failed to get sub-menus");
				//else
				//{
					for (byte y = 0; y < submenustrings.Count; y++)
					{
						for (int z = 0; z < submenustrings[y].Count; z++)
							Console.WriteLine("	|___" + submenustrings[y][z]);
						//if (!serviceHandler.GetDataByIdentifiers(out byte[] byteArray)) Console.WriteLine("Failed to get parameters");
						//if (!serviceHandler.GetDataFromByteArray(byteArray, out List<Data> dataArray)) Console.WriteLine("Failed to parse data");
						//for (int x = 0; x < dataArray.Count; x++)
						//	Console.WriteLine($"{dataArray[x].name} {dataArray[x].value}");
					}
				//}
			}

			//написать функцию для преобразования 1-64 в 0x70 0x00 - байты ХХ и УУ в схеме 10 09 2E [XX] 75 FE 01 00 00 [YY] 00
			//Console.WriteLine(serviceHandler.GetMenus((UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0xFE02));
			//serviceHandler.SendWriteDataByIdentifier((UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0x7175, new byte[] { 0xFE, 0x01, 0x00, 0x00, 0x01, 0x00 });
			//Console.WriteLine(serviceHandler.GetMenus((UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0xFE02));

			//UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER[] identifiers = { (UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0x5375, (UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0xFE01, (UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0x0000, (UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0x2300 }; //0xFE00 menus, 0xFE02 menu 2
			//byte[] byteArray = serviceHandler.SendWriteDataByIdentifier((UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0xFE01, new byte[] { 0x00, 0x00, 0x23, 0x00 });
			//foreach (byte b in byteArray) Console.Write($"{b:X2} ");
			//Console.WriteLine();

			Uninitialize(handle);
			*/

			byte[] byteArray = { 0x62, 0x01, 0x64, 0xE7, 0xFD, 0x0E, 0x62, 0xFF, 0x00, 0x42, 0x6F, 0x6F, 0x6D, 0x20, 0x55, 0x70, 0x20, 0x6D, 0x69, 0x6E, 0x00, 0x00, 0x00, 0x00, 0x03, 0xE8, 0x00, 0x0A, 0x6D, 0x41, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x80, 0x01, 0x5E, 0x03, 0x59, 0x00, 0x00, 0x01, 0x5E, 0xFD, 0x1E, 0x42, 0x6F, 0x6F, 0x6D, 0x20, 0x55, 0x70, 0x20, 0x6D, 0x61, 0x78, 0x00, 0x00, 0x00, 0x00, 0x03, 0xE8, 0x00, 0x0A, 0x6D, 0x41, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x80, 0x02, 0x9E, 0x03, 0x5A, 0x00, 0x00, 0x02, 0x9E, 0xFD, 0x2E, 0x42, 0x6F, 0x6F, 0x6D, 0x20, 0x44, 0x6F, 0x77, 0x6E, 0x20, 0x6D, 0x69, 0x6E, 0x00, 0x00, 0x00, 0x00, 0x03, 0xE8, 0x00, 0x0A, 0x6D, 0x41, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x80, 0x01, 0x5E, 0x03, 0x5B, 0x00, 0x00, 0x01, 0x5E, 0xFD, 0x3E, 0x42, 0x6F, 0x6F, 0x6D, 0x20, 0x44, 0x6F, 0x77, 0x6E, 0x20, 0x6D, 0x61, 0x78, 0x00, 0x00, 0x00, 0x00, 0x03, 0xE8, 0x00, 0x0A, 0x6D, 0x41, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x80, 0x02, 0x74, 0x03, 0x5C, 0x00, 0x00, 0x02, 0x74, 0xFD, 0x4E, 0x42, 0x75, 0x63, 0x6B, 0x65, 0x74, 0x20, 0x54, 0x69, 0x6C, 0x74, 0x20, 0x49, 0x6E, 0x20, 0x6D, 0x69, 0x6E, 0x00, 0x00, 0x00, 0x00, 0x03, 0xE8, 0x00, 0x0A, 0x6D, 0x41, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x80, 0x01, 0x5E, 0x03, 0x5D, 0x00, 0x00, 0x01, 0x5E };
			Console.WriteLine(ServiceHandler.GetDataFromByteArray(byteArray, out List<Data> data));
			foreach (Data d in data)
				Console.WriteLine(d.ToString());
		}

        private static void GetVersionInformation()
		{
			UdsStatus status;
			const int BUFFER_SIZE = 256;
			StringBuilder buffer = new(BUFFER_SIZE);
			status = UDSApi.GetValue_2013(CantpHandle.PCANTP_HANDLE_NONEBUS, UdsParameter.PUDS_PARAMETER_API_VERSION, buffer, BUFFER_SIZE);
			Console.WriteLine($"PCAN-UDS API Version: {buffer} ({status})");
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