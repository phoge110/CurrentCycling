using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Timers;
using System.IO.Ports;
using System.ComponentModel;

namespace Current_Cycling_Controls {
    public delegate void TDKEvent(object sender, GUIArgs e);
    public class CurrentCycling {
        public event TDKEvent UpdateUI;
        private SerialPort _serTDK;
        //private BackgroundWorker _reportWorker = new BackgroundWorker();
        private Stopwatch _timer;
        private bool _timeOut;
        public List<TDK> _TDK;
        public bool _updateRun;
        public GUIArgs _args;
        public bool STOP;
        public event CoreCommandEvent NewCoreCommand;
        public CurrentCycling() {
            // initialize
            _serTDK = new SerialPort();
        }


        public void StartCycling(StartCyclingArgs args) {
            // start serial interface stuff/ start timers
            //_TDK = args.TDK;
            var tdk = args.TDK;
            try {
                OpenPorts(); // TODO: MOVE TO FRMMAIN()
                foreach (var t in tdk) {                    
                    TurnON(t);
                    SetCurrentVoltage(t);
                }

                StartTimer();
                // Loop forever until we get a stop command from main thread
                while (true) {
                    // BIAS ON
                    while (_timer.ElapsedMilliseconds < args.BiasOnTime && !STOP) {
                        foreach (var tt in tdk) {
                            _serTDK.Write("MV?\r\n");
                            Wait(50); // lag in measured value
                            string volt = _serTDK.ReadLine();

                            _serTDK.Write("MC?\r\n");
                            Wait(50);
                            string current = _serTDK.ReadLine();
                            _args = new GUIArgs(volt, current, tt.CycleCount);
                            NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateUI });
                        }
                    }
                    if (!STOP) break;
                    // BIAS OFF
                    TurnOff(tdk);
                    while (_timer.ElapsedMilliseconds < args.BiasOffTime && !STOP) {
                        foreach (var tt in tdk) {
                            _serTDK.Write("MV?\r\n");
                            Wait(50); // lag in measured value
                            string volt = _serTDK.ReadLine();

                            _serTDK.Write("MC?\r\n");
                            Wait(50);
                            string current = _serTDK.ReadLine();
                            _args = new GUIArgs(volt, current, tt.CycleCount);
                            NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateUI });
                        }
                    }
                    if (!STOP) break;

                    // completed a bias on/off cycle
                    foreach (var ttt in tdk) {
                        ttt.CycleCount++;
                    }
                }               
            }
            catch (Exception exc) {
                Console.WriteLine($"{exc}");
                TurnOffClose(tdk);
                STOP = false;
                foreach (var tttt in tdk) {
                    tttt.CycleCount++;
                }
            }

            STOP = false;
            TurnOffClose(tdk);
        }

        private void OpenPorts() {
            _serTDK.BaudRate = U.BaudRate;
            _serTDK.PortName = U.COMPort;
            _serTDK.NewLine = "\r";
            _serTDK.ReadTimeout = 1000;
            _serTDK.Open();

            
            _serTDK.DiscardOutBuffer();
            _serTDK.DiscardInBuffer();
        }

        private void SetCurrentVoltage(TDK tdk) {
            // Sets the current limit of the power supply
            do {
                _serTDK.Write("PC " + tdk.Current + "\r\n");
                if (_serTDK.ReadLine() == "OK") {
                    Console.WriteLine($"Current: OKAY");
                }
                _serTDK.Write("MC?\r\n");
            } while (_serTDK.ReadLine() == tdk.Current);

            do {
                //Sets the voltage of the power supply
                _serTDK.Write("PV " + U.VoltageCompliance + "\r\n");
                if (_serTDK.ReadLine() == "OK") {
                    Console.WriteLine($"Voltage: OKAY");
                }
                _serTDK.Write("MC?\r\n");
            } while (_serTDK.ReadLine() == U.VoltageCompliance);
            
        }

        private void TurnON(TDK tdk) {
            // Sets the address of the power supply
            _serTDK.Write("ADR " + tdk.Address + "\r\n");
            if (_serTDK.ReadLine() == "OK") {
                Console.WriteLine($"Open: OKAY");
            }

            do {
                _serTDK.Write("OUT ON\r\n");
                if (_serTDK.ReadLine() == "OK") {
                    Console.WriteLine($"ON: OKAY");
                }
                _serTDK.Write("MODE?\r\n");
            } while (_serTDK.ReadLine() == "ON");
        }

        private void TurnOff(List<TDK> tdk) {
            foreach (var t in tdk) {
                _serTDK.Write("ADR " + t.Address + "\r\n");
                _serTDK.Write("OUT OFF\r\n");
                if (_serTDK.ReadLine() == "OK") {
                    Console.WriteLine($"OFF: OKAY");
                }
                _serTDK.DiscardOutBuffer();
                _serTDK.DiscardInBuffer();
            }
        }

        private void TurnOffClose(List<TDK> tdk) {
            foreach (var t in tdk) {
                _serTDK.Write("ADR " + t.Address + "\r\n");
                _serTDK.Write("OUT OFF\r\n");
                if (_serTDK.ReadLine() == "OK") {
                    Console.WriteLine($"OFF: OKAY");
                }
                _serTDK.DiscardOutBuffer();
                _serTDK.DiscardInBuffer();
                _serTDK.Close();
            }
        }

        private void StartTimer() {
            _timer = new Stopwatch();
            _timer.Start();
        }

        private void Wait(int t) {
            long elapsed = _timer.ElapsedMilliseconds;
            while (_timer.ElapsedMilliseconds - elapsed > t) { }
        }

        public void Timeout(Object source, ElapsedEventArgs e) {
            _timeOut = true;
        }

    }

    public class GUIArgs {
        public string Volt { get; set; }
        public string Current { get; set; }
        public string Cycle { get; set; }
        public GUIArgs(string volt, string current, int cycle){
            Volt = volt;
            Current = current;
            Cycle = cycle.ToString();
        }
    }

}
