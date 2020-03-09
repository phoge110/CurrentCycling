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
        public static string SampleTxtHeader = "Cycle Number,Epoch Time (seconds),Total Time (hrs),Time into Cycle (min),Current Status,Sample Name,Current (A),Voltage (V),# Cells,Cell VoC,TempSensor,SetCurrent,Estimated Rs,Temp 1,Temp 2,Temp 3,Temp 4,Temp 5,Temp 6,Temp 7,Temp 8,Temp 9,Temp 10,Temp 11,Temp 12,Temp 13,Temp 14,Temp 15,Temp 16";

        public enum CmdType {
            None,
            Sequence,
            StartCycling,
            UpdateUI,
            StopCycling,
            CleanGUI,
            RecievedPacket,
            UpdateHeartBeatPacket,
            CheckConnection
        }

        public enum Status {
            Error = -1,
            Initialize,

        }

        public enum Results {
            CycleNum,
            Epoch,
            TotalHrs,
            TotalCycleTime,
            BiasStatus,
            SampleName,
            Current,
            Voltage,
            NumCells,
            Voc,
            TempSensors,
            SetCurrent,
            Rs, 
            Temp1,
            Temp2,
            Temp3,
            Temp4,
            Temp5,
            Temp6,
            Temp7,
            Temp8,
            Temp9,
            Temp10,
            Temp11,
            Temp12,
            Temp13,
            Temp14,
            Temp15,
            Temp16,
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
