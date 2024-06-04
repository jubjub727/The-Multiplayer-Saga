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
        private Dictionary<string, double> _Store = new Dictionary<string, double>();

        public Dictionary<string, double> Times
        { get { return _Store; } }

        public PerfTimer()
        {
            _Timer = new Stopwatch();
        }

        public void Start()
        {
            _Timer.Restart();
        }

        public void Store(string name)
        {
            _Store[name] = _Timer.Elapsed.TotalMicroseconds;
            _Timer.Restart();
        }

        public double? Collect(string name)
        {
            if (_Store.ContainsKey(name))
            {
                return _Store[name];
            }
            else
            {
                return null;
            }
        }
    }
}
