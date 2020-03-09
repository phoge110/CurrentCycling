using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;


namespace Current_Cycling_Controls
{
    public delegate void HeartBeatUpdate(object sender, TransmitPacket t);
    public partial class frmMain : Form
    {
        public bool Connected { get; set; }
        public HeartBeatUpdate heartBeatUpdates;
        private readonly AutoResetEvent _commReset = new AutoResetEvent(false);
        private BackgroundWorker _commWorker = new BackgroundWorker();
        private BackgroundWorker _TDKWorker = new BackgroundWorker();
        private BackgroundWorker _arduinoWorker = new BackgroundWorker();
        private BackgroundWorker _connectionWorker = new BackgroundWorker();
        private readonly Queue<CoreCommand> _commandQueue = new Queue<CoreCommand>();
        private CurrentCycling _cycling = new CurrentCycling();
        private ArduinoMachine _arduino = new ArduinoMachine();
        private readonly object _lock = new object();
        private List<TDK> _TDKS;
        private List<CheckBox> _checkBoxes;
        private List<TextBox> _tempSensors;
        private List<TextBox> _setCurrents;
        private List<Label> _tempLabels;
        private List<Label> _smokeLabels;
        private List<Label> _voltageLabels;
        private List<Label> _currentLabels;
        private List<Label> _cycleLabels;
        private List<Label> _connectedLabels;
        private List<Button> _loadButtons;
        private List<Button> _newButtons;
        private List<TextBox> _voc;
        private List<TextBox> _numCells;

        private DateTime _cycleTimer = DateTime.Now;
        private TransmitPacket _heartBeatPacket;
        public frmMain()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(Form_Closing);

            // reload default settings to GUI
            txtDirectory.Text = Properties.Settings.Default.DataFolder;
            txtOperator.Text = Properties.Settings.Default.Operator;
            txtBiasOn.Text = Properties.Settings.Default.BiasON;
            txtBiasOff.Text = Properties.Settings.Default.BiasOFF;
            txtCurrOnTempSet.Text = Properties.Settings.Default.BiasONTempSet;
            txtCurrOffTempSet.Text = Properties.Settings.Default.BiasOFFTempSet;
            txtOverTempSet.Text = Properties.Settings.Default.OverTempSet;
            txtSmokeOverSet.Text = Properties.Settings.Default.OverSmokeSet;
            txtPauseFans.Text = Properties.Settings.Default.PauseFanTime;


            _commWorker.DoWork += RunCommMachine;
            _commWorker.WorkerReportsProgress = true;
            _commWorker.ProgressChanged += UpdateUi;
            _commWorker.RunWorkerAsync();

            _TDKWorker.DoWork += RunCurrentCycling;
            _TDKWorker.RunWorkerCompleted += CyclingComplete;

            _arduinoWorker.DoWork += RunArduinoLoop;
            _arduinoWorker.WorkerReportsProgress = true;
            _arduinoWorker.ProgressChanged += UpdateHeartBeat;
            _arduinoWorker.RunWorkerAsync();

            _connectionWorker.DoWork += CheckConnect;
            _connectionWorker.RunWorkerCompleted += ConnectComplete;

            _cycling.NewCoreCommand += NewCoreCommand;
            _arduino.NewCoreCommand += NewCoreCommand;


            _checkBoxes = new List<CheckBox> { chkbxPort1 , chkbxPort2, chkbxPort3,
            chkbxPort4, chkbxPort5, chkbxPort6, chkbxPort7, chkbxPort8, chkbxPort9,
            chkbxPort10, chkbxPort11, chkbxPort12};
            _setCurrents = new List<TextBox> { txtSetCurr1, txtSetCurr2, txtSetCurr3,
            txtSetCurr4,txtSetCurr5,txtSetCurr6,txtSetCurr7,txtSetCurr8,txtSetCurr9,
            txtSetCurr10,txtSetCurr11,txtSetCurr12};
            _tempSensors = new List<TextBox> { txtTempSensSample1, txtTempSensSample2,
            txtTempSensSample3,txtTempSensSample4,txtTempSensSample5,txtTempSensSample6,
            txtTempSensSample7,txtTempSensSample8,txtTempSensSample9,
            txtTempSensSample10,txtTempSensSample11,txtTempSensSample12};
            _tempLabels = new List<Label> { labelTemp1, labelTemp2,
            labelTemp3,labelTemp4,labelTemp5,labelTemp6,
            labelTemp7,labelTemp8,labelTemp9,labelTemp10,
            labelTemp11,labelTemp12, labelTemp13, labelTemp14, labelTemp15, labelTemp16};
            _smokeLabels = new List<Label> { labelSmoke1, labelSmoke2,
            labelSmoke3,labelSmoke4,labelSmoke5,labelSmoke6,
            labelSmoke7,labelSmoke8};
            _voltageLabels = new List<Label> { lblVoltage1, lblVoltage2 , lblVoltage3 ,
                lblVoltage4,lblVoltage5,lblVoltage6,lblVoltage7,lblVoltage8,lblVoltage9,
                lblVoltage10,lblVoltage11,lblVoltage12};
            _currentLabels = new List<Label> { lblCurrent1, lblCurrent2 , lblCurrent3 ,
                lblCurrent4,lblCurrent5,lblCurrent6,lblCurrent7,lblCurrent8,lblCurrent9,
                lblCurrent10,lblCurrent11,lblCurrent12};
            _cycleLabels = new List<Label> { lblCycle1, lblCycle2, lblCycle3,
                lblCycle4,lblCycle5,lblCycle6,lblCycle7,lblCycle8,lblCycle9,
                lblCycle10,lblCycle11,lblCycle12};
            _connectedLabels = new List<Label> { lblPSStatus1, lblPSStatus2, lblPSStatus3,
            lblPSStatus4,lblPSStatus5,lblPSStatus6,lblPSStatus7,lblPSStatus8,lblPSStatus9,
            lblPSStatus10,lblPSStatus11,lblPSStatus12 };
            _loadButtons = new List<Button> { btnLoad1, btnLoad2, btnLoad3, btnLoad4,
            btnLoad5,btnLoad6,btnLoad7,btnLoad8,btnLoad9,btnLoad10,btnLoad11,btnLoad12};
            _newButtons = new List<Button> { btnNew1, btnNew2, btnNew3 , btnNew4 ,
            btnNew5,btnNew6,btnNew7,btnNew8,btnNew9,btnNew10,btnNew11,btnNew12};
            _voc = new List<TextBox> { txtVoc1, txtVoc2 , txtVoc3 , txtVoc4 , txtVoc5 ,
            txtVoc6,txtVoc7,txtVoc8,txtVoc9,txtVoc10,txtVoc11,txtVoc12};
            _numCells = new List<TextBox> { txtNumCells1, txtNumCells2 , txtNumCells3 ,
            txtNumCells4,txtNumCells5,txtNumCells6,txtNumCells7,txtNumCells8,txtNumCells9
            ,txtNumCells10,txtNumCells11,txtNumCells12};

            // set all port info to disabled
            //btnLoad1.Enabled = false;

            // initialize TDK objects
            _TDKS = new List<TDK> { };
            for (int i = 1; i <13; i++) {
                _TDKS.Add(new TDK("0" + i, i));
            }

            // initialize heartbeatpacket before arduino declarations
            string tempBin = "";
            foreach (object chk in chkTemp.Items) {
                tempBin += GetBinary(chkTemp.GetItemChecked(chkTemp.Items.IndexOf(chk)));
            }
            string smokeBin = "";
            foreach (object chk in chkSmoke.Items) {
                smokeBin += GetBinary(chkTemp.GetItemChecked(chkTemp.Items.IndexOf(chk)));
            }
            _heartBeatPacket = new TransmitPacket(txtOverTempSet.Text, txtSmokeOverSet.Text,
                txtCurrOnTempSet.Text, txtCurrOffTempSet.Text, "0", "0", tempBin, smokeBin);

            _connectionWorker.RunWorkerAsync();
        }

        private void RunCurrentCycling (object s, DoWorkEventArgs e) {
            var tdk = (StartCyclingArgs)e.Argument;
            _cycling.StartCycling(tdk);
        }

        private void CyclingComplete(object s, RunWorkerCompletedEventArgs e) {
            // clean up TDKs and maybe graph/show output results to file?
            NewCoreCommand(this, new CoreCommand { Type = U.CmdType.CleanGUI });
            foreach (var t in _TDKS) {
                t.SetCurrent = null;
            }
        }

        private void RunArduinoLoop(object s, DoWorkEventArgs e) {
            _arduino.StartArduinoMachine();
        }

        public void UpdateHeartBeat(object sender, ProgressChangedEventArgs e) {
            _arduino.UpdateTransmit(_heartBeatPacket);
        }

        public void CheckConnect(object sender, DoWorkEventArgs e) {
            var labels = CheckConnection();
            e.Result = labels;
        }

        private void ConnectComplete(object s, RunWorkerCompletedEventArgs e) {
            var res = e.Result;
            _commWorker.ReportProgress(4, res);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CoreCommand GetNextCommand() {
            CoreCommand cmd = null;
            lock (_lock) {
                if (_commandQueue.Count > 0) {
                    cmd = _commandQueue.Dequeue();
                }
            }
            return cmd;
        }
        /// <summary>
        /// Wait for Command, if command append it to command queue and FIFO the commands.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunCommMachine(object sender, DoWorkEventArgs e) {
            while (true) {
                try {
                    _commReset.WaitOne(-1);
                    var cmd = GetNextCommand();
                    while (cmd != null) {
                        HandleCommand(cmd);
                        cmd = GetNextCommand();
                    }
                }
                catch (Exception exc) {
                    
                }
            }
        }

        /// <summary>
        /// Handles Core commands from the Controls modules or the GUI. 
        /// </summary>
        private void HandleCommand(CoreCommand c) {
            if (c == null) {
                Info("Got null command");
                return;
            }
            //Info(c);
            switch (c.Type) {
                case U.CmdType.None:
                    break;
                case U.CmdType.StartCycling:
                    Console.WriteLine($"Starting TDK Worker thread");
                    NewCoreCommand(this, new CoreCommand { Type = U.CmdType.UpdateHeartBeatPacket });
                    
                    _TDKWorker.RunWorkerAsync(c.StartArgs);
                    // communicate with TDKs with the StartInspectionProperties
                    break;
                case U.CmdType.UpdateUI:
                    _commWorker.ReportProgress(5, c.StartArgs);
                    break;
                case U.CmdType.StopCycling:
                    _cycling.STOP = true;
                    break;
                case U.CmdType.CleanGUI:
                    _commWorker.ReportProgress(1);
                    break;
                case U.CmdType.RecievedPacket:
                    // update GUI with temp/smoke/alarms
                    _commWorker.ReportProgress(2, c.ArduinoArgs);

                    // if cycling is running then update alarms if needed
                    var packet = _arduino._recievedPacket;
                    if (_TDKWorker.IsBusy) {
                        _cycling.SMOKEALARM = packet.SmokeAlarm;
                        _cycling.TEMPALARM = packet.TempAlarm;
                        _cycling.STOP = packet.EMSSTOP;
                        _cycling._temps = new List<double>(packet.TempList);
                    }

                    if (packet.SmokeAlarm || packet.TempAlarm || packet.EMSSTOP) {
                        SoundPlayer audio = new SoundPlayer(Properties.Resources.AircraftAlarm);
                        audio.Play();
                    }
                    break;
                case U.CmdType.UpdateHeartBeatPacket:
                    _commWorker.ReportProgress(3);
                    break;
                case U.CmdType.CheckConnection:
                    _connectionWorker.RunWorkerAsync();
                    break;
            }
        }

        private void NewCoreCommand(object sender, CoreCommand c) {
            lock (_lock) {
                _commandQueue.Enqueue(c);
                _commReset.Set();
            }
        }


        private void UpdateUi(object sender, ProgressChangedEventArgs e) {
            try {
                // update TDK readings during Cycling
                if (e.ProgressPercentage == 5) {
                    var args = _cycling._args;
                    _voltageLabels[args.Port-1].Text = args.Volt;
                    _currentLabels[args.Port-1].Text = args.Current;
                    _cycleLabels[args.Port-1].Text = args.Cycle; 

                    var ts = (args.CycleTime - DateTime.Now);
                    labelCount.Text = $@"{ts.Minutes:D2}:{ts.Seconds:D2}";
                    return;
                }
                // re-enable GUI buttons
                else if (e.ProgressPercentage == 1) {
                    foreach (var chk in _checkBoxes) {
                        chk.Enabled = true;
                    }
                    foreach (var temp in _tempSensors) {
                        temp.Enabled = true;
                    }
                    foreach (var curr in _setCurrents) {
                        curr.Enabled = true;
                    }
                    foreach (var load in _loadButtons) {
                        load.Enabled = true;
                    }
                    foreach (var neww in _newButtons) {
                        neww.Enabled = true;
                    }
                    foreach (var neww in _voc) {
                        neww.Enabled = true;
                    }
                    foreach (var neww in _numCells) {
                        neww.Enabled = true;
                    }
                    btnStart.Enabled = true;
                    chkTemp.Enabled = true;
                    chkSmoke.Enabled = true;
                    button1.Enabled = true;
                    buttonCheckConnection.Enabled = true;
                    return;
                }
                // update temp/smoke/alarm readings
                else if (e.ProgressPercentage == 2) {
                    var ardArgs = _arduino._recievedPacket;
                    var i = 0;
                    foreach (var lb in _tempLabels) {
                        lb.Text = ardArgs.TempList[i].ToString("F1"); 
                        i++;
                    }
                    i = 0;
                    foreach (var lb in _smokeLabels) {
                        lb.Text = ardArgs.SmokeList[i].ToString("F1");
                        i++;
                    }

                    labelTempAlarm.BackColor = ardArgs.TempAlarm ? Color.Red : Color.Empty;
                    labelSmokeAlarm.BackColor = ardArgs.SmokeAlarm ? Color.Red : Color.Empty;
                    labelEMSStop.BackColor = ardArgs.EMSSTOP ? Color.Red : Color.Empty;

                }
                // send event to arduino thread to update serial transmit packet
                else if (e.ProgressPercentage == 3){
                    string tempBin = "";
                    foreach (object chk in chkTemp.Items) {
                        tempBin += GetBinary(chkTemp.GetItemChecked(chkTemp.Items.IndexOf(chk)));
                    }
                    string smokeBin = "";
                    foreach (object chk in chkSmoke.Items) {
                        smokeBin += GetBinary(chkSmoke.GetItemChecked(chkSmoke.Items.IndexOf(chk)));
                    }
                    string biasON = _cycling.BIASON ? "1" : "0";
                    _heartBeatPacket = new TransmitPacket(txtOverTempSet.Text, txtSmokeOverSet.Text,
                        txtCurrOnTempSet.Text, txtCurrOffTempSet.Text, biasON, "", tempBin, smokeBin);
                    _arduinoWorker.ReportProgress(1);
                }
                // update connection strings from connection worker
                else if (e.ProgressPercentage == 4) {
                    var res = (List<string>)e.UserState;
                    int i = 0;
                    foreach (string str in res) {
                        _connectedLabels[i].Text = str;
                        i++;
                    }
                }
            }
            catch { }
        }


        public static void Info(object m, string module = "Server") {
            Console.WriteLine($@"[{DateTime.Now:G}]:[{module}] {m}");
        }

        private void BtnStart_Click(object sender, EventArgs e) {
            CheckPorts();
            var startargs = new StartCyclingArgs(_TDKS.Where(t => t.SetCurrent != null).ToList(), 
                Double.Parse(txtBiasOn.Text), Double.Parse(txtBiasOff.Text), txtDirectory.Text);

            var start = new CoreCommand {
                Type = U.CmdType.StartCycling,
                StartArgs = startargs
            };
            NewCoreCommand(this, start);

            // Disable GUI buttons from the GUI thread 
            foreach (var chk in _checkBoxes) {
                chk.Enabled = false;
            }
            foreach (var temp in _tempSensors) {
                temp.Enabled = false;
            }
            foreach (var curr in _setCurrents) {
                curr.Enabled = false;
            }
            foreach (var load in _loadButtons) {
                load.Enabled = false;
            }
            foreach (var neww in _newButtons) {
                neww.Enabled = false;
            }
            foreach (var neww in _voc) {
                neww.Enabled = false;
            }
            foreach (var neww in _numCells) {
                neww.Enabled = false;
            }
            chkTemp.Enabled = false;
            chkSmoke.Enabled = false;
            btnStart.Enabled = false;
            buttonCheckConnection.Enabled = false;
            button1.Enabled = false;

            // save GUI inputs to default settings
            Properties.Settings.Default.DataFolder = txtDirectory.Text;
            Properties.Settings.Default.Operator = txtOperator.Text;
            Properties.Settings.Default.BiasON = txtBiasOn.Text;
            Properties.Settings.Default.BiasOFF = txtBiasOff.Text;
            Properties.Settings.Default.BiasONTempSet = txtCurrOnTempSet.Text;
            Properties.Settings.Default.BiasOFFTempSet = txtCurrOffTempSet.Text;
            Properties.Settings.Default.OverTempSet = txtOverTempSet.Text;
            Properties.Settings.Default.OverSmokeSet = txtSmokeOverSet.Text;
            Properties.Settings.Default.PauseFanTime = txtPauseFans.Text;
            Properties.Settings.Default.Save();

        }

        private void CheckPorts() {
            if (chkbxPort1.Checked) {
                _TDKS.Where(t => t.Port == 1).FirstOrDefault().SetCurrent = txtSetCurr1.Text;
                _TDKS.Where(t => t.Port == 1).FirstOrDefault().TempSensor = txtTempSensSample1.Text;
                _TDKS.Where(t => t.Port == 1).FirstOrDefault().SampleName = lblSample1.Text;
                _TDKS.Where(t => t.Port == 1).FirstOrDefault().Voc = txtVoc1.Text;
                _TDKS.Where(t => t.Port == 1).FirstOrDefault().NumCells = txtNumCells1.Text;
                _TDKS.Where(t => t.Port == 1).FirstOrDefault().CycleCount = int.Parse(lblCycle1.Text);
                lblPSStatus1.Text = "Connected";
            }
            if (chkbxPort2.Checked) {
                _TDKS.Where(t => t.Port == 2).FirstOrDefault().SetCurrent = txtSetCurr2.Text;
                _TDKS.Where(t => t.Port == 2).FirstOrDefault().TempSensor = txtTempSensSample2.Text;
                _TDKS.Where(t => t.Port == 2).FirstOrDefault().SampleName = lblSample2.Text;
                _TDKS.Where(t => t.Port == 2).FirstOrDefault().Voc = txtVoc2.Text;
                _TDKS.Where(t => t.Port == 2).FirstOrDefault().NumCells = txtNumCells2.Text;
                _TDKS.Where(t => t.Port == 2).FirstOrDefault().CycleCount = int.Parse(lblCycle2.Text);
            }
            if (chkbxPort3.Checked) {
                _TDKS.Where(t => t.Port == 3).FirstOrDefault().SetCurrent = txtSetCurr3.Text;
                _TDKS.Where(t => t.Port == 3).FirstOrDefault().TempSensor = txtTempSensSample3.Text;
                _TDKS.Where(t => t.Port == 3).FirstOrDefault().SampleName = lblSample3.Text;
                _TDKS.Where(t => t.Port == 3).FirstOrDefault().Voc = txtVoc3.Text;
                _TDKS.Where(t => t.Port == 3).FirstOrDefault().NumCells = txtNumCells3.Text;
                _TDKS.Where(t => t.Port == 3).FirstOrDefault().CycleCount = int.Parse(lblCycle3.Text);
            }
            if (chkbxPort4.Checked) {
                _TDKS.Where(t => t.Port == 4).FirstOrDefault().SetCurrent = txtSetCurr4.Text;
                _TDKS.Where(t => t.Port == 4).FirstOrDefault().TempSensor = txtTempSensSample4.Text;
                _TDKS.Where(t => t.Port == 4).FirstOrDefault().SampleName = lblSample4.Text;
                _TDKS.Where(t => t.Port == 4).FirstOrDefault().Voc = txtVoc4.Text;
                _TDKS.Where(t => t.Port == 4).FirstOrDefault().NumCells = txtNumCells4.Text;
                _TDKS.Where(t => t.Port == 4).FirstOrDefault().CycleCount = int.Parse(lblCycle4.Text);
            }
            if (chkbxPort5.Checked) {
                _TDKS.Where(t => t.Port == 5).FirstOrDefault().SetCurrent = txtSetCurr5.Text;
                _TDKS.Where(t => t.Port == 5).FirstOrDefault().TempSensor = txtTempSensSample5.Text;
                _TDKS.Where(t => t.Port == 5).FirstOrDefault().SampleName = lblSample5.Text;
                _TDKS.Where(t => t.Port == 5).FirstOrDefault().Voc = txtVoc5.Text;
                _TDKS.Where(t => t.Port == 5).FirstOrDefault().NumCells = txtNumCells5.Text;
                _TDKS.Where(t => t.Port == 5).FirstOrDefault().CycleCount = int.Parse(lblCycle5.Text);
            }
            if (chkbxPort6.Checked) {
                _TDKS.Where(t => t.Port == 6).FirstOrDefault().SetCurrent = txtSetCurr6.Text;
                _TDKS.Where(t => t.Port == 6).FirstOrDefault().TempSensor = txtTempSensSample6.Text;
                _TDKS.Where(t => t.Port == 6).FirstOrDefault().SampleName = lblSample6.Text;
                _TDKS.Where(t => t.Port == 6).FirstOrDefault().Voc = txtVoc6.Text;
                _TDKS.Where(t => t.Port == 6).FirstOrDefault().NumCells = txtNumCells6.Text;
                _TDKS.Where(t => t.Port == 6).FirstOrDefault().CycleCount = int.Parse(lblCycle6.Text);
            }
            if (chkbxPort7.Checked) {
                _TDKS.Where(t => t.Port == 7).FirstOrDefault().SetCurrent = txtSetCurr7.Text;
                _TDKS.Where(t => t.Port == 7).FirstOrDefault().TempSensor = txtTempSensSample7.Text;
                _TDKS.Where(t => t.Port == 7).FirstOrDefault().SampleName = lblSample7.Text;
                _TDKS.Where(t => t.Port == 7).FirstOrDefault().Voc = txtVoc7.Text;
                _TDKS.Where(t => t.Port == 7).FirstOrDefault().NumCells = txtNumCells7.Text;
                _TDKS.Where(t => t.Port == 7).FirstOrDefault().CycleCount = int.Parse(lblCycle7.Text);
            }
            if (chkbxPort8.Checked) {
                _TDKS.Where(t => t.Port == 8).FirstOrDefault().SetCurrent = txtSetCurr8.Text;
                _TDKS.Where(t => t.Port == 8).FirstOrDefault().TempSensor = txtTempSensSample8.Text;
                _TDKS.Where(t => t.Port == 8).FirstOrDefault().SampleName = lblSample8.Text;
                _TDKS.Where(t => t.Port == 8).FirstOrDefault().Voc = txtVoc8.Text;
                _TDKS.Where(t => t.Port == 8).FirstOrDefault().NumCells = txtNumCells8.Text;
                _TDKS.Where(t => t.Port == 8).FirstOrDefault().CycleCount = int.Parse(lblCycle8.Text);
            }
            if (chkbxPort9.Checked) {
                _TDKS.Where(t => t.Port == 9).FirstOrDefault().SetCurrent = txtSetCurr9.Text;
                _TDKS.Where(t => t.Port == 9).FirstOrDefault().TempSensor = txtTempSensSample9.Text;
                _TDKS.Where(t => t.Port == 9).FirstOrDefault().SampleName = lblSample9.Text;
                _TDKS.Where(t => t.Port == 9).FirstOrDefault().Voc = txtVoc9.Text;
                _TDKS.Where(t => t.Port == 9).FirstOrDefault().NumCells = txtNumCells9.Text;
                _TDKS.Where(t => t.Port == 9).FirstOrDefault().CycleCount = int.Parse(lblCycle9.Text);
            }
            if (chkbxPort10.Checked) {
                _TDKS.Where(t => t.Port == 10).FirstOrDefault().SetCurrent = txtSetCurr10.Text;
                _TDKS.Where(t => t.Port == 10).FirstOrDefault().TempSensor = txtTempSensSample10.Text;
                _TDKS.Where(t => t.Port == 10).FirstOrDefault().SampleName = lblSample10.Text;
                _TDKS.Where(t => t.Port == 10).FirstOrDefault().Voc = txtVoc10.Text;
                _TDKS.Where(t => t.Port == 10).FirstOrDefault().NumCells = txtNumCells10.Text;
                _TDKS.Where(t => t.Port == 10).FirstOrDefault().CycleCount = int.Parse(lblCycle10.Text);
            }
            if (chkbxPort11.Checked) {
                _TDKS.Where(t => t.Port == 11).FirstOrDefault().SetCurrent = txtSetCurr11.Text;
                _TDKS.Where(t => t.Port == 11).FirstOrDefault().TempSensor = txtTempSensSample11.Text;
                _TDKS.Where(t => t.Port == 11).FirstOrDefault().SampleName = lblSample11.Text;
                _TDKS.Where(t => t.Port == 11).FirstOrDefault().Voc = txtVoc11.Text;
                _TDKS.Where(t => t.Port == 11).FirstOrDefault().NumCells = txtNumCells11.Text;
                _TDKS.Where(t => t.Port == 11).FirstOrDefault().CycleCount = int.Parse(lblCycle11.Text);
            }
            if (chkbxPort12.Checked) {
                _TDKS.Where(t => t.Port == 12).FirstOrDefault().SetCurrent = txtSetCurr12.Text;
                _TDKS.Where(t => t.Port == 12).FirstOrDefault().TempSensor = txtTempSensSample12.Text;
                _TDKS.Where(t => t.Port == 12).FirstOrDefault().SampleName = lblSample12.Text;
                _TDKS.Where(t => t.Port == 12).FirstOrDefault().Voc = txtVoc12.Text;
                _TDKS.Where(t => t.Port == 12).FirstOrDefault().NumCells = txtNumCells12.Text;
                _TDKS.Where(t => t.Port == 12).FirstOrDefault().CycleCount = int.Parse(lblCycle12.Text);
            }
        }

        private string GetBinary(bool value) {
            return (value == true ? "1" : "0");
        }

        private void Form_Closing(object s, FormClosingEventArgs e) {
            if (_TDKWorker.IsBusy) {
                MessageBox.Show("Please Stop Cycling before Closing Form! ");
                e.Cancel = true;
            }
        }

        private void ButtonCheckConnection_Click(object sender, EventArgs e) {
            NewCoreCommand(this, new CoreCommand { Type = U.CmdType.CheckConnection });
        }

        public List<string> CheckConnection() {
            var ser = new SerialPort();
            ser.BaudRate = U.BaudRate;
            ser.PortName = U.COMPort;
            ser.NewLine = "\r";
            ser.ReadTimeout = 100;
            var connectLabels = new List<string>();
            try {
                ser.Open();
            }
            catch (Exception exc) {
                Console.WriteLine($"Unable to access TDK COM Port!");
                Console.WriteLine($"{exc}");
                connectLabels.AddRange(Enumerable.Repeat("Not Connected", 12));
                return connectLabels;
            }
            

            for (var i = 1; i < 13; i++) {
                try {
                    ser.Write("ADR " + $"0{i.ToString()}" + "\r\n");
                    if (ser.ReadLine() == "OK") {
                        connectLabels.Add("Connected");
                        ser.DiscardOutBuffer();
                        ser.DiscardInBuffer();
                    }                    
                }
                catch { } // timed out
                connectLabels.Add("Not Connected");
            }
            ser.Close();
            return connectLabels;
        }


        private void ChkbxPort1_CheckedChanged(object sender, EventArgs e) {
            if (chkbxPort1.Checked) {
                btnLoad1.Enabled = true;
                btnNew1.Enabled = true;
            }
        }

        private void BtnStop_Click(object sender, EventArgs e) {
            NewCoreCommand(this, new CoreCommand{ Type = U.CmdType.StopCycling });
        }

        private void ButtonDataFolder_Click(object sender, EventArgs e) {
            var folderPath = new FolderBrowserDialog();
            if (folderPath.ShowDialog() == DialogResult.Cancel) return;
            Properties.Settings.Default.DataFolder = folderPath.SelectedPath;
            txtDirectory.Text = folderPath.SelectedPath;
        }

        private void BtnNew1_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample1.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad1_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample1.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle1.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells1.Text = values[8];
            txtVoc1.Text = values[9];
            txtTempSensSample1.Text = values[10];
            txtSetCurr1.Text = values[11];

        }

# region "Bloated Button Code"
        private void BtnNew2_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample2.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad2_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample2.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle2.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells2.Text = values[8];
            txtVoc2.Text = values[9];
            txtTempSensSample2.Text = values[10];
            txtSetCurr2.Text = values[11];

        }

        private void BtnNew3_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample3.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad3_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample3.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle3.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells3.Text = values[8];
            txtVoc3.Text = values[9];
            txtTempSensSample3.Text = values[10];
            txtSetCurr3.Text = values[11];

        }

        private void BtnNew4_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample4.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad4_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample4.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle4.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells4.Text = values[8];
            txtVoc4.Text = values[9];
            txtTempSensSample4.Text = values[10];
            txtSetCurr4.Text = values[11];

        }

        private void BtnNew5_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample5.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad5_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample5.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle5.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells5.Text = values[8];
            txtVoc5.Text = values[9];
            txtTempSensSample5.Text = values[10];
            txtSetCurr5.Text = values[11];

        }

        private void BtnNew6_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample6.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad6_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample6.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle6.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells6.Text = values[8];
            txtVoc6.Text = values[9];
            txtTempSensSample6.Text = values[10];
            txtSetCurr6.Text = values[11];

        }

        private void BtnNew7_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample7.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad7_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample7.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle7.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells7.Text = values[8];
            txtVoc7.Text = values[9];
            txtTempSensSample7.Text = values[10];
            txtSetCurr7.Text = values[11];

        }

        private void BtnNew8_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample8.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad8_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample8.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle8.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells8.Text = values[8];
            txtVoc8.Text = values[9];
            txtTempSensSample8.Text = values[10];
            txtSetCurr8.Text = values[11];

        }

        private void BtnNew9_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample9.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad9_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample9.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle9.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells9.Text = values[8];
            txtVoc9.Text = values[9];
            txtTempSensSample9.Text = values[10];
            txtSetCurr9.Text = values[11];

        }

        private void BtnNew10_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample10.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad10_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample10.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle10.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells10.Text = values[8];
            txtVoc10.Text = values[9];
            txtTempSensSample10.Text = values[10];
            txtSetCurr10.Text = values[11];

        }

        private void BtnNew11_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample11.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad11_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample11.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle11.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells11.Text = values[8];
            txtVoc11.Text = values[9];
            txtTempSensSample11.Text = values[10];
            txtSetCurr11.Text = values[11];

        }

        private void BtnNew12_Click(object sender, EventArgs e) {
            // create new file upload dialog and user choose folder then put in sample name.txt
            var saveFile = new SaveFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (saveFile.ShowDialog() == DialogResult.Cancel) return;
            using (var writer = new StreamWriter(saveFile.FileName, true)) {
                writer.WriteLine(U.SampleTxtHeader);
            }
            lblSample12.Text = Path.GetFileNameWithoutExtension(saveFile.FileName);
        }

        private void BtnLoad12_Click(object sender, EventArgs e) {
            // loads the file and reads the last readline and updates the GUI with values (cycle, voc, set current etc)
            var loadFile = new OpenFileDialog() { InitialDirectory = Properties.Settings.Default.DataFolder };
            if (loadFile.ShowDialog() == DialogResult.Cancel) return;

            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            var last = File.ReadLines(loadFile.FileName).Last();
            var values = last.Split(',').Select(sValue => sValue.Trim()).ToList();

            lblSample12.Text = Path.GetFileNameWithoutExtension(loadFile.FileName);
            if (File.ReadLines(loadFile.FileName).Count() < 2) return;
            lblCycle12.Text = values[0]; //TODO: find a better way than indexing
            txtNumCells12.Text = values[8];
            txtVoc12.Text = values[9];
            txtTempSensSample12.Text = values[10];
            txtSetCurr12.Text = values[11];

        }
        #endregion

    }
}
