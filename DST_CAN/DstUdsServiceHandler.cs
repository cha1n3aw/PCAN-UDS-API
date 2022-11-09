using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PCAN_UDS_TEST.DST_CAN.DstUdsServiceHandler;

namespace PCAN_UDS_TEST.DST_CAN
{
    internal partial class DstUdsServiceHandler
    {
        private AutoResetEvent _responseFlag;
        private DstUdsHandler udsHandler;
        //private uint sourceAddress;
        //private uint destinationAddress;

        public DstUdsServiceHandler(DstUdsHandler udsHandler)
        {
            this.udsHandler = udsHandler;
            _responseFlag = new AutoResetEvent(false);
        }

        public byte[] SendDiagnosticSessionControl(UDS_SERVICE_DSC sessionType)
        {
            //neebu
            return null;
        }

    }
}
