﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Current_Cycling_Controls {
    public class TDK {
        public bool ON { get; set; }
        public int Port { get; set; }
        public string Address { get; set; }
        public string Current { get; set; }
        public string TempSensor { get; set; }
        public int CycleCount { get; set; }

        public TDK(string address, int port) {
            Port = port;
            Address = address;
            CycleCount = 0;
            //Current = current;
            //TempSensor = tempsensor;
            //ON = on;

        }

    }
}