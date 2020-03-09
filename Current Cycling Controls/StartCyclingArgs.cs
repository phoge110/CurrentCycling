using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Current_Cycling_Controls {
    public class StartCyclingArgs {

        public List<TDK> TDK { get; set; }
        public double BiasOnTime { get; set; }
        public double BiasOffTime { get; set; }
        public string ResultsDirectory { get; set; }

        public StartCyclingArgs(List<TDK> tdk, double biason, double biasoff, string dir) {
            TDK = tdk;
            BiasOnTime = biason * 60000 ; // convert to msec
            BiasOffTime = biasoff * 60000;
            ResultsDirectory = dir;
        }
    }
}
