using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tmpsclient
{
    public class PerfTimer
    {
        private Stopwatch _Timer;

        public PerfTimer()
        {
            _Timer = new Stopwatch();
        }

        public void Start()
        {
            _Timer.Restart();
        }

        public double Collect()
        {
            double retVal = _Timer.Elapsed.TotalMicroseconds;

            _Timer.Restart();

            return retVal;
        }
    }
}
