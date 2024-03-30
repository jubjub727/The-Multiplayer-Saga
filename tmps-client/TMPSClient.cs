using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace LSWTSS.OMP;

public class TMPSClient : IDisposable
{
    public void Dispose()
    {

    }

    ~TMPSClient()
    {
        Dispose();
    }
}
