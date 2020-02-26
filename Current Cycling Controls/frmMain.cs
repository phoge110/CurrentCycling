using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Current_Cycling_Controls
{
    public partial class frmMain : Form
    {
        public bool Connected { get; set; }
        private readonly AutoResetEvent _commReset = new AutoResetEvent(false);
        private BackgroundWorker _commWorker = new BackgroundWorker();
        private BackgroundWorker _TDKWorker = new BackgroundWorker();
        private readonly Queue<CoreCommand> _commandQueue = new Queue<CoreCommand>();
        private CurrentCycling _cycling = new CurrentCycling();
        private int _count;
        private readonly object _lock = new object();
        private List<TDK> _TDKS;
        private List<CheckBox> _checkBoxes;
        private List<TextBox> _tempSensors;
        private List<TextBox> _setCurrents;
        public frmMain()
        {
            InitializeComponent();
            txtDirectory.Text = Properties.Settings.Default.Path;

            _commWorker.DoWork += RunCommMachine;
            _commWorker.WorkerReportsProgress = true;
            _commWorker.ProgressChanged += UpdateUi;
            _commWorker.RunWorkerAsync();

            _TDKWorker.DoWork += RunCurrentCycling;
            _TDKWorker.RunWorkerCompleted += CyclingComplete;

            _cycling.NewCoreCommand += NewCoreCommand;

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

            _TDKS = new List<TDK> { };
            for (int i = 1; i <13; i++) {
                _TDKS.Add(new TDK("0" + i, i));
            }
            

        }

        private void RunCurrentCycling (object s, DoWorkEventArgs e) {
            var tdk = (StartCyclingArgs)e.Argument;
            _cycling.StartCycling(tdk);
        }

        private void CyclingComplete(object s, RunWorkerCompletedEventArgs e) {
            // clean up TDKs and maybe graph/show output results to file?
            NewCoreCommand(this, new CoreCommand { Type = U.CmdType.UpdateUI }); // TODO: SET GUI OBJECTS AS ENABLED
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
            Info(c);
            switch (c.Type) {
                case U.CmdType.None:
                    break;
                case U.CmdType.StartCycling:
                    Console.WriteLine($"Starting TDK Worker thread");
                    _TDKWorker.RunWorkerAsync(c.StartArgs);
                    // communicate with TDKs with the StartInspectionProperties
                    break;
                case U.CmdType.UpdateUI:
                    _commWorker.ReportProgress(5, c.StartArgs);
                    break;
                case U.CmdType.StopCycling:
                    _cycling.STOP = true;
                    break;
            }
        }
        // TODO: AUTORESET EVENT FOR ARDUNIO RESEARCH
        private void NewCoreCommand(object sender, CoreCommand c) {
            lock (_lock) {
                _commandQueue.Enqueue(c);
                _commReset.Set();
            }
        }


        private void UpdateUi(object sender, ProgressChangedEventArgs e) {
            try {
                _count = 0;
                if (e.ProgressPercentage == 5) {
                    var args = _cycling._args;
                    lblVoltage1.Text = (string)args.Volt;
                    lblCurrent1.Text = (string)args.Current;
                    lblCycle1.Text = (string)args.Cycle;
                    return;
                }
                else if (e.ProgressPercentage == 0) {
                    return;
                }
            }
            catch { }
        }


        public static void Info(object m, string module = "Server") {
            Console.WriteLine($@"[{DateTime.Now:G}]:[{module}] {m}");
        }

        private void BtnStart_Click(object sender, EventArgs e) {
            CheckPorts();
            var startargs = new StartCyclingArgs(_TDKS.Where(t => t.Current != null).ToList(), 
                int.Parse(txtBiasOn.Text), int.Parse(txtBiasOff.Text));

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
            btnStart.Enabled = false;



        }

        private void CheckPorts() {
            var tdk = new List<TDK>(_TDKS);
            if (chkbxPort1.Checked) {
                _TDKS.Where(t => t.Port == 1).FirstOrDefault().Current = txtSetCurr1.Text;
                _TDKS.Where(t => t.Port == 1).FirstOrDefault().TempSensor = txtTempSensSample1.Text;
            }
            if (chkbxPort2.Checked) {
                _TDKS.Where(t => t.Port == 2).FirstOrDefault().Current = txtSetCurr2.Text;
                _TDKS.Where(t => t.Port == 2).FirstOrDefault().TempSensor = txtTempSensSample2.Text;
            }
            if (chkbxPort3.Checked) {
                _TDKS.Where(t => t.Port == 3).FirstOrDefault().Current = txtSetCurr3.Text;
                _TDKS.Where(t => t.Port == 3).FirstOrDefault().TempSensor = txtTempSensSample3.Text;
            }
            if (chkbxPort4.Checked) {
                _TDKS.Where(t => t.Port == 4).FirstOrDefault().Current = txtSetCurr4.Text;
                _TDKS.Where(t => t.Port == 4).FirstOrDefault().TempSensor = txtTempSensSample4.Text;
            }
            if (chkbxPort5.Checked) {
                _TDKS.Where(t => t.Port == 5).FirstOrDefault().Current = txtSetCurr5.Text;
                _TDKS.Where(t => t.Port == 5).FirstOrDefault().TempSensor = txtTempSensSample5.Text;
            }
            if (chkbxPort6.Checked) {
                _TDKS.Where(t => t.Port == 6).FirstOrDefault().Current = txtSetCurr6.Text;
                _TDKS.Where(t => t.Port == 6).FirstOrDefault().TempSensor = txtTempSensSample6.Text;
            }
            if (chkbxPort7.Checked) {
                _TDKS.Where(t => t.Port == 7).FirstOrDefault().Current = txtSetCurr7.Text;
                _TDKS.Where(t => t.Port == 7).FirstOrDefault().TempSensor = txtTempSensSample7.Text;
            }
            if (chkbxPort8.Checked) {
                _TDKS.Where(t => t.Port == 8).FirstOrDefault().Current = txtSetCurr8.Text;
                _TDKS.Where(t => t.Port == 8).FirstOrDefault().TempSensor = txtTempSensSample8.Text;
            }
            if (chkbxPort9.Checked) {
                _TDKS.Where(t => t.Port == 9).FirstOrDefault().Current = txtSetCurr9.Text;
                _TDKS.Where(t => t.Port == 9).FirstOrDefault().TempSensor = txtTempSensSample9.Text;
            }
            if (chkbxPort10.Checked) {
                _TDKS.Where(t => t.Port == 10).FirstOrDefault().Current = txtSetCurr10.Text;
                _TDKS.Where(t => t.Port == 10).FirstOrDefault().TempSensor = txtTempSensSample10.Text;
            }
            if (chkbxPort11.Checked) {
                _TDKS.Where(t => t.Port == 11).FirstOrDefault().Current = txtSetCurr11.Text;
                _TDKS.Where(t => t.Port == 11).FirstOrDefault().TempSensor = txtTempSensSample11.Text;
            }
            if (chkbxPort12.Checked) {
                _TDKS.Where(t => t.Port == 12).FirstOrDefault().Current = txtSetCurr12.Text;
                _TDKS.Where(t => t.Port == 12).FirstOrDefault().TempSensor = txtTempSensSample12.Text;
            }
        }



        private void ChkbxPort1_CheckedChanged(object sender, EventArgs e) {

        }

        private void BtnStop_Click(object sender, EventArgs e) {
            NewCoreCommand(this, new CoreCommand{ Type = U.CmdType.StopCycling });
        }
    }
}
