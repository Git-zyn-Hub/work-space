using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokenRailMonitorViaWiFi
{
    public enum CommandType
    {
        RequestConfig = 0xA0,
        AssignClientID = 0xA1,
        UploadConfig = 0xA2,
        ConfigInitialInfo = 0xF0,
        ReadPointInfo = 0xF1,
        GetHistory = 0xF4,
        GetPointRailInfo = 0xF5
    }
}
