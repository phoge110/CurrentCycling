using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Current_Cycling_Controls {
    public class U {

        public static int BaudRate = 57600;
        public static string COMPort = "COM3";
        public static string VoltageCompliance = "5"; //Kat's code

        public enum CmdType {
            None,
            Sequence,
            StartCycling,
            UpdateUI,
            StopCycling,
            CleanGUI,
            RecievedPacket
        }

        public enum Status {
            Error = -1,
            Initialize,

        }

        public string GetCOMPort(string adrs) {
            switch (adrs) {
                case "01":
                    return "COM01";
                case "02":
                    return "COM01";
                case "03":
                    return "COM01";
                case "04":
                    return "COM01";
                case "05":
                    return "COM01";
                case "06":
                    return "COM01";
                case "07":
                    return "COM01";
                case "08":
                    return "COM01";
                case "09":
                    return "COM01";
                case "10":
                    return "COM01";
                case "11":
                    return "COM01";
                case "12":
                    return "COM01";
                default:
                    return "";
            }
        }


    }
}
