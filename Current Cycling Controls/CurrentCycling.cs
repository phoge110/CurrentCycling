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
        private DateTime _cycleTimer = DateTime.Now;
        private Stopwatch _timer;
        private bool _timeOut;
        public List<TDK> _TDK;
        public bool _updateRun;
        public GUIArgs _args;
        public bool STOP;
        public bool SMOKEALARM;
        public bool TEMPALARM;
        public bool BIASON;
        public event CoreCommandEvent NewCoreCommand;
        public CurrentCycling() {
            // initialize
            _serTDK = new SerialPort();
            OpenPorts();
        }


        public void StartCycling(StartCyclingArgs args) {
            // start serial interface stuff/ start timers
            var tdk = args.TDK;
            try {
                foreach (var t in tdk) {
                    try {
                        SetAddress(t);
                        SetCurrentVoltage(t);
                        t.Connected = true;
                    }
                    catch (TimeoutException exc) {
                        Console.WriteLine($"TIMEOUT ON PORT #{t.Port}");
                        throw new Exception(exc.Message);
                    }
                }

                // Loop forever until we get a stop command from main thread
                while (true) {
                    // BIAS ON
                    foreach (var t in tdk) {
                        TurnON(t);
                    }
                    BIASON = true;
                    NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateHeartBeatPacket });
                    StartTimer();                    
                    _cycleTimer = DateTime.Now.AddMilliseconds(args.BiasOnTime);
                    while (_timer.ElapsedMilliseconds < args.BiasOnTime
                        && !STOP && !TEMPALARM && !SMOKEALARM) {
                        foreach (var tt in tdk) {
                            SetAddress(tt);

                            _serTDK.Write("MV?\r\n");
                            Wait(50); // lag in measured value
                            string volt = _serTDK.ReadLine();

                            _serTDK.Write("MC?\r\n");
                            Wait(50);
                            string current = _serTDK.ReadLine();
                            _args = new GUIArgs(volt, current, tt.CycleCount, tt.Port, _cycleTimer);
                            NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateUI });
                        }
                    }
                    if (STOP || SMOKEALARM || TEMPALARM) break;
                    // BIAS OFF
                    TurnOff(tdk);
                    BIASON = false;
                    NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateHeartBeatPacket });
                    StartTimer();
                    _cycleTimer = DateTime.Now.AddMilliseconds(args.BiasOffTime);
                    while (_timer.ElapsedMilliseconds < args.BiasOffTime
                        && !STOP && !TEMPALARM && !SMOKEALARM) {
                        foreach (var tt in tdk) {
                            _serTDK.Write("MV?\r\n");
                            Wait(50); // lag in measured value
                            string volt = _serTDK.ReadLine();

                            _serTDK.Write("MC?\r\n");
                            Wait(50);
                            string current = _serTDK.ReadLine();
                            _args = new GUIArgs(volt, current, tt.CycleCount, tt.Port, _cycleTimer);
                            NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateUI });
                        }
                    }
                    if (STOP || SMOKEALARM || TEMPALARM) break;

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
                SMOKEALARM = false;
                TEMPALARM = false;
            }
            STOP = false;
            SMOKEALARM = false;
            TEMPALARM = false;
            TurnOffClose(tdk);
        }

        private void OpenPorts() {
            string[] ports = SerialPort.GetPortNames();
            // only one COM port available
            if (ports.Length == 1) {
                _serTDK.BaudRate = U.BaudRate;
                _serTDK.PortName = ports.FirstOrDefault();
                _serTDK.NewLine = "\r";
                _serTDK.ReadTimeout = 1000;
                _serTDK.Open();

                _serTDK.DiscardOutBuffer();
                _serTDK.DiscardInBuffer();
                return;
            }
            foreach (var port in ports) { // ping each port and see if we get the correct response
                try {
                    _serTDK.BaudRate = U.BaudRate;
                    _serTDK.PortName = U.COMPort;
                    _serTDK.NewLine = "\r";
                    _serTDK.ReadTimeout = 1000;
                    _serTDK.Open();

                    _serTDK.Write("ADR " + "01" + "\r\n");
                    if (_serTDK.ReadLine() == "OK") {
                        _serTDK.DiscardOutBuffer();
                        _serTDK.DiscardInBuffer();
                        return;
                    }
                }
                catch { }
            }
        }

        private void SetAddress(TDK tdk) {
            // Sets the address of the power supply
            _serTDK.Write("ADR " + tdk.Address + "\r\n");
            if (_serTDK.ReadLine() == "OK") { }
        }

        private void SetCurrentVoltage(TDK tdk) {
            // Sets the current limit of the power supply
            do {
                _serTDK.Write("PC " + tdk.Current + "\r\n");
                if (_serTDK.ReadLine() == "OK") {
                    Console.WriteLine($"Current: OKAY");
                }
                _serTDK.Write("PC?\r\n");
            } while (_serTDK.ReadLine() == tdk.Current);

            do {
                //Sets the voltage of the power supply
                _serTDK.Write("PV " + U.VoltageCompliance + "\r\n");
                if (_serTDK.ReadLine() == "OK") {
                    Console.WriteLine($"Voltage: OKAY");
                }
                _serTDK.Write("PC?\r\n");
            } while (_serTDK.ReadLine() == U.VoltageCompliance);
            
        }

        private void TurnON(TDK tdk) {
            // Sets the address of the power supply
            SetAddress(tdk);

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
                    Console.WriteLine($"Port #{t.Port} OFF: OKAY");
                }
                _serTDK.DiscardOutBuffer();
                _serTDK.DiscardInBuffer();
                
                
            }
            StartTimer();
            Wait(500);
            foreach (var tt in tdk) {
                _serTDK.Write("MV?\r\n");
                Wait(50); // lag in measured value
                string volt = _serTDK.ReadLine();

                _serTDK.Write("MC?\r\n");
                Wait(50);
                string current = _serTDK.ReadLine();
                _args = new GUIArgs(volt, current, tt.CycleCount, tt.Port, _cycleTimer);
                NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateUI });
            }
            //_serTDK.Close();
        }

        private void StartTimer() {
            _timer = new Stopwatch();
            _timer.Start();
        }

        private void Wait(int t) {
            long elapsed = _timer.ElapsedMilliseconds;
            while (_timer.ElapsedMilliseconds - elapsed < t) { }
        }

        public void Timeout(Object source, ElapsedEventArgs e) {
            _timeOut = true;
        }

    }

    public class GUIArgs {
        public string Volt { get; set; }
        public string Current { get; set; }
        public string Cycle { get; set; }
        public DateTime CycleTime { get; set; }
        public int Port { get; set; }
        public GUIArgs(string volt, string current, int cycle, int port, DateTime dt){
            Volt = volt;
            Current = current;
            Cycle = cycle.ToString();
            Port = port;
            CycleTime = dt;
        }
    }

}
