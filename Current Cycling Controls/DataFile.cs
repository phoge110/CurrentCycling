using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Current_Cycling_Controls {
    public class DataFile {
        public string CycleNumber { get; set; }
        public string EpochTime { get; set; }
        public string TotalHrs { get; set; }
        public string IntoCycleTime { get; set; }
        public string BiasStatus { get; set; }
        public string SampleName { get; set; }
        public string Current { get; set; }
        public string Voltage { get; set; }
        public string NumCells { get; set; }
        public string Voc { get; set; }
        public string TempSensor { get; set; }
        public string SetCurrent { get; set; }
        public string Rs { get; set; }
        public string Temp1 { get; set; }
        public string Temp2 { get; set; }
        public string Temp3 { get; set; }
        public string Temp4 { get; set; }
        public string Temp5 { get; set; }
        public string Temp6 { get; set; }
        public string Temp7 { get; set; }
        public string Temp8 { get; set; }
        public string Temp9 { get; set; }
        public string Temp10 { get; set; }
        public string Temp11 { get; set; }
        public string Temp12 { get; set; }
        public string Temp13 { get; set; }
        public string Temp14 { get; set; }
        public string Temp15 { get; set; }
        public string Temp16 { get; set; }


        public DataFile(string cycle, string epoch, string total,
            string intocy, string status, string sample, string current,
            string voltage, string numcells, string voc, string tempsensor,
            string setcurrent, string rs, List<string> temps) {

            CycleNumber = cycle;
            EpochTime = epoch;
            TotalHrs = total;
            IntoCycleTime = intocy;
            BiasStatus = status;
            SampleName = sample;
            Current = current;
            Voltage = voltage;
            NumCells = numcells;
            Voc = voc;
            TempSensor = tempsensor;
            SetCurrent = setcurrent;



        }

    }
}
