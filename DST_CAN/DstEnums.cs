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
    }
}
