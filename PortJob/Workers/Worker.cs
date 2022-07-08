using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortJob {
    public abstract class Worker {
        public bool IsDone { get; protected set; }
        protected Thread _thread { get; set; }
        public int ExitCode { get; set; }
    }
}
