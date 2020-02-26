using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace Current_Cycling_Controls {
    public class SerialPortClass {
        private SerialPort _serialPort = new SerialPort();

        private SerialPortClass() {
            _serialPort.BaudRate = U.BaudRate;
            _serialPort.PortName = U.COMPort;
            _serialPort.NewLine = "\r";
            _serialPort.ReadTimeout = 1500;
            _serialPort.Open();
        }

        private static SerialPortClass _instance = new SerialPortClass();
        public static SerialPortClass Instance {
            get {
                return _instance;
            }
        }

        public void Write(string s) {
            _serialPort.Write(s);
        }

        public string ReadLine() {
            return _serialPort.ReadLine();
        }

        //private void Open(string s) {
        //    _serialPort.Write(s);
        //}
    }
}
