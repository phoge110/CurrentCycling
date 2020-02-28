using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Current_Cycling_Controls {

    public delegate void CoreCommandEvent(object sender, CoreCommand c);
    public class CoreCommand {

        public U.CmdType Type { get; set; }
        public List<TDK> TDK { get; set; }
        public StartCyclingArgs StartArgs { get; set; }
        public RecievePacket ArduinoArgs { get; set; }

        public static CoreCommand StartCycling(U.CmdType op) {
            return new CoreCommand {
                Type = U.CmdType.Sequence,
            };

        }


    }
}
