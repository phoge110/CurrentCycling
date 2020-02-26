using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Current_Cycling_Controls {
    public class StartCyclingArgs {

        public List<TDK> TDK { get; set; }
        public int BiasOnTime { get; set; }
        public int BiasOffTime { get; set; }


        public StartCyclingArgs(List<TDK> tdk, int biason, int biasoff) {
            TDK = tdk;
            BiasOnTime = biason * 60000 ; // convert to msec
            BiasOffTime = biasoff * 60000;

        }
    }
}
