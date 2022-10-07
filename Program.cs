using PCAN_UDS_TEST;
using Peak.Can.IsoTp;
using Peak.Can.Uds;
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
            
            UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER[] identifiers = { (UDSApi.UDS_SERVICE_PARAMETER_DATA_IDENTIFIER)0xFE00 }; //0xFE00 menus, 0xFE02 menu 2
            byte[] byteArray = serviceHandler.SendReadDataByIdentifier(identifiers);
            for (int i = 9; i < byteArray.Length; i++)
                if (byteArray[i] == 0x00) Console.WriteLine();
                else Console.Write((char)byteArray[i]);
            Console.WriteLine();
            
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