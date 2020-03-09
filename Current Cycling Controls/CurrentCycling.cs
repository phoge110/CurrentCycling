using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Timers;
using System.IO.Ports;
using System.ComponentModel;
using System.IO;

namespace Current_Cycling_Controls {
    public delegate void TDKEvent(object sender, GUIArgs e);
    public class CurrentCycling {
        public event TDKEvent UpdateUI;
        private SerialPort _serTDK;
        private DateTime _cycleTimer = DateTime.Now;
        private Stopwatch _timer;
        private Stopwatch _resultsTimer;
        private Stopwatch _totalTimer;
        private Stopwatch _intoCycleTimer;
        private bool _timeOut;
        public List<TDK> _TDK;
        public bool _updateRun;
        public GUIArgs _args;
        public string _resultsDir;
        public bool STOP;
        public bool SMOKEALARM;
        public bool TEMPALARM;
        public bool BIASON;
        public bool SAVE;
        public List<double> _temps;
        public event CoreCommandEvent NewCoreCommand;
        public CurrentCycling() {

        }

        private string CompileDataStr(TDK t) {
            string[] str = { t.CycleCount.ToString(), (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds.ToString(),
                (_totalTimer.ElapsedMilliseconds / 3.6E+6).ToString(), // total time (hrs)
                (_intoCycleTimer.ElapsedMilliseconds / 60000.0).ToString(),
                (BIASON) ? "ON" : "OFF",
                t.SampleName, t.Current, t.Voltage, t.NumCells, t.Voc,
                t.TempSensor, t.SetCurrent, "",
                $"{_temps[0].ToString("F1")}",$"{_temps[1].ToString("F1")}",$"{_temps[2].ToString("F1")}",
                $"{_temps[3].ToString("F1")}",$"{_temps[4].ToString("F1")}",$"{_temps[5].ToString("F1")}",
                $"{_temps[6].ToString("F1")}",$"{_temps[7].ToString("F1")}",$"{_temps[8].ToString("F1")}",
                $"{_temps[9].ToString("F1")}",$"{_temps[10].ToString("F1")}",$"{_temps[11].ToString("F1")}",
                $"{_temps[12].ToString("F1")}",$"{_temps[13].ToString("F1")}",$"{_temps[14].ToString("F1")}",
                $"{_temps[15].ToString("F1")}" };
            return string.Join(",", str);
        }


        public void StartCycling(StartCyclingArgs args) {
            _serTDK = new SerialPort();
            OpenPorts();
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
                _TDK = tdk;
                _resultsDir = args.ResultsDirectory;
                _totalTimer = new Stopwatch();
                _totalTimer.Start();

                // Loop forever until we get a stop command from main thread
                while (true) {
                    _intoCycleTimer = new Stopwatch();
                    _intoCycleTimer.Start();

                    // BIAS ON
                    foreach (var t in tdk) {
                        TurnON(t);
                    }
                    BIASON = true;
                    NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateHeartBeatPacket });
                    StartTimer();
                    StartResultsTimer();
                    _cycleTimer = DateTime.Now.AddMilliseconds(args.BiasOnTime);
                    while (_timer.ElapsedMilliseconds < args.BiasOnTime
                        && !STOP && !TEMPALARM && !SMOKEALARM) {
                        if (_resultsTimer.ElapsedMilliseconds > 1000) SAVE = true;
                        foreach (var tt in tdk) {
                            SetAddress(tt);

                            _serTDK.Write("MV?\r\n");
                            Wait(50); // lag in measured value
                            tt.Voltage = _serTDK.ReadLine();

                            _serTDK.Write("MC?\r\n");
                            Wait(50);
                            tt.Current = _serTDK.ReadLine();
                            _args = new GUIArgs(tt.Voltage, tt.Current, tt.CycleCount, tt.Port, _cycleTimer);
                            NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateUI });
                            if (SAVE) SaveResults(tt);
                        }
                        // if we have saved then restart timer
                        if (SAVE) {
                            SAVE = false;
                            _resultsTimer.Restart();
                        }
                    }
                    if (STOP || SMOKEALARM || TEMPALARM) break;
                    // BIAS OFF
                    TurnOff(tdk);
                    BIASON = false;
                    NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateHeartBeatPacket });
                    StartTimer();
                    StartResultsTimer();
                    _cycleTimer = DateTime.Now.AddMilliseconds(args.BiasOffTime);
                    while (_timer.ElapsedMilliseconds < args.BiasOffTime
                        && !STOP && !TEMPALARM && !SMOKEALARM) {
                        if (_resultsTimer.ElapsedMilliseconds > 1000) SAVE = true;
                        foreach (var tt in tdk) {
                            _serTDK.Write("MV?\r\n");
                            Wait(50); // lag in measured value
                            tt.Voltage = _serTDK.ReadLine();

                            _serTDK.Write("MC?\r\n");
                            Wait(50);
                            tt.Current = _serTDK.ReadLine();
                            _args = new GUIArgs(tt.Voltage, tt.Current, tt.CycleCount, tt.Port, _cycleTimer);
                            NewCoreCommand?.Invoke(this, new CoreCommand() { Type = U.CmdType.UpdateUI });
                            if (SAVE) SaveResults(tt);
                        }

                        // if we have saved then restart timer
                        if (SAVE) {
                            SAVE = false;
                            _resultsTimer.Restart();
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

        private void SaveResults(TDK t) {
            var str = CompileDataStr(t);
            var path = _resultsDir + $"\\{t.SampleName}.txt";
            using (var writer = new StreamWriter(path, true)) {
                writer.WriteLine(str);
            }
        }

        private void OpenPorts() {
            string[] ports = SerialPort.GetPortNames();
            foreach (var port in ports) { // ping each port and see if we get the correct response
                try {
                    _serTDK.BaudRate = U.BaudRate;
                    _serTDK.PortName = port; // com3
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
                catch (Exception exc) { _serTDK.Close(); }
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
                _serTDK.Write("PC " + tdk.SetCurrent + "\r\n");
                if (_serTDK.ReadLine() == "OK") {
                    Console.WriteLine($"Current: OKAY");
                }
                _serTDK.Write("PC?\r\n");
            } while (_serTDK.ReadLine() == tdk.SetCurrent);

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
            _serTDK.Close();
        }

        private void StartTimer() {
            _timer = new Stopwatch();
            _timer.Start();
        }

        private void StartResultsTimer() {
            _resultsTimer = new Stopwatch();
            _resultsTimer.Start();
        }

        private void Wait(int t) {
            long elapsed = _timer.ElapsedMilliseconds;
            while (_timer.ElapsedMilliseconds - elapsed < t) { }
        }

        private void WaitResults(int t) {
            long elapsed = _resultsTimer.ElapsedMilliseconds;
            while (_resultsTimer.ElapsedMilliseconds - elapsed < t) { }
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
