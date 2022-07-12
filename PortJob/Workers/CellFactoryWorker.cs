using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortJob {
    public class CellFactoryWorker : Worker {
        private ESM _esm { get; }
        private List<JObject> _cells { get; }
        private int _start { get; }
        private int _end { get; }
        private List<Cell> _processedCells { get; }
        public List<Cell> ProcessedCells {
            get {
                _thread.Join();
                return _processedCells;
            }
        }

        public CellFactoryWorker(ESM esm, List<JObject> cells, int start, int end) {
            _processedCells = new List<Cell>();
            _esm = esm;
            _cells = cells;
            _start = start;
            _end = end;
            _thread = new Thread(ProcessCell);
            _thread.Start();
        }

        private void ProcessCell() {
            ExitCode = 1;
           // ProgressBar progress = new($"Processing cells {_start} - {_end}", Console.CursorTop);
            for (int i = _start; i < _cells.Count && i < _end; i++) {
                Cell genCell = new(_esm, _cells[i]);
                _processedCells.Add(genCell);
                //progress.Report(i / (_end - _start));
            }
            IsDone = true;
            ExitCode = 0;
        }
    }
}
