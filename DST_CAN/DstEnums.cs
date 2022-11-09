using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCAN_UDS_TEST.DST_CAN
{
    internal partial class DstUdsServiceHandler
    {
        public enum UDS_SERVICE_DSC : Byte
        {
            /// <summary>
            /// Default Session
            /// </summary>
            DEFAULT_SESSION = 0x01,
            /// <summary>
            /// ECU Programming Session
            /// </summary>
            ECU_PROGRAMMING_SESSION = 0x02,
            /// <summary>
            /// ECU Extended Diagnostic Session
            /// </summary>
            ECU_EXTENDED_DIAGNOSTIC_SESSION = 0x03,
            /// <summary>
            /// Safety System Diagnostic Session
            /// </summary>
            SAFET_SYSTEM_DIAGNOSTIC_SESSION = 0x04
        }

        public enum UDS_SERVICE_PARAMETER_ECU_RESET : Byte
        {
            /// <summary>
            /// Hard Reset
            /// </summary>
            HARD_RESET = 0x01,
            /// <summary>
            /// Key Off on Reset
            /// </summary>
            KEY_OFF_ON_RESET = 0x02,
            /// <summary>
            /// Soft Reset
            /// </summary>
            SOFT_RESET = 0x03,
            /// <summary>
            /// Enable Rapid Power Shutdown
            /// </summary>
            EN_RAPID_POWER_SHUTDOWN = 0x04,
            /// <summary>
            /// Disable Rapid Power Shutdown
            /// </summary>
            DIS_RAPID_POWER_SHUTDOWN = 0x05,
        }
    }
}
