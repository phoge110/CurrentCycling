using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Current_Cycling_Controls {
    public class ArduinoMachine {

        private SerialPort _serArduino;

        public void StartArduinoMachine() {



        }


    }

    public class RecievePacket{
        private List<double> TempList { get; set; }
        private List<double> SmokeList { get; set; }
        private bool TempAlarm { get; set; }
        private bool SmokeAlarm { get; set; }
        private bool EMSSTOP { get; set; }
        private bool HeartBeatGood { get; set; }

        public RecievePacket() {


        }

    }

    public class TransmitPacket {
        private string TempSetPoint { get; set; }
        private string SmokeSetPoint { get; set; }
        private string BiasCurrentONTemp { get; set; }
        private string BiasCurrentOFFTemp { get; set; }
        private string BiasCurrentStatus { get; set; }
        // TODO: Decide on the active smoke/temp packet structure that the arduino wants
            
        public TransmitPacket() {


        }

    }
}
