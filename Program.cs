using PCAN_UDS_TEST;
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
            Initialize(handle, baudrate);
            ServiceHandler serviceHandler = new(handle, sourceAddress, destinationAddress);
            List<ushort> dataIdentifiers = new() {
                //0xFD0E,
                //0xFD1E,
                //0xFD2E,
                //0xFD3E,
                //0xFD4E,
                //0xFD5E,
                //0xFD6E,
                0xFD7E };

            Dictionary<string, Dictionary<string, List<Data>>> menuStructure = new();
            if (serviceHandler.GetMenus(out List<string> menuStrings))
            {
                for (byte i = 0; i < menuStrings.Count; i++)
                {
                    Dictionary<string, List<Data>> menuContents = new();
                    if (serviceHandler.GetSubMenus(i, out List<string> subMenuStringsList))
                    {
                        foreach (string subMenuString in subMenuStringsList)
                        {
                            List<Data> tempDataList = new();
                            while(tempDataList.Count < dataIdentifiers.Count)
                            {
                                if (serviceHandler.GetDataByIdentifiers(dataIdentifiers.Cast<UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER>().ToArray(), out byte[] byteArray))
                                    if (serviceHandler.GetDataFromByteArray(byteArray, out List<Data> dataList)) tempDataList.AddRange(dataList);
                                    else Console.WriteLine("Failed to parse data");
                                else Console.WriteLine("Failed to get parameters");
                            }
                            menuContents.Add(subMenuString, tempDataList);
                        }
                    }
                    Console.WriteLine("Failed to get sub-menus");
                    menuStructure.Add(menuStrings[i], menuContents);
                }
            }
            else Console.WriteLine("Failed to get menus");

            foreach (var menu in menuStructure)
            {
                Console.WriteLine(menu.Key);
                foreach (var subMenu in menu.Value)
                {
                    Console.WriteLine(subMenu.Key);
                    foreach (Data data in subMenu.Value)
                        Console.WriteLine(data.ToString());
                }
            }

            Uninitialize(handle);
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