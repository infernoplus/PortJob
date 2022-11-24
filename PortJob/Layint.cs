using CommonFunc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static CommonFunc.Const;

namespace PortJob {
    /* This class takes the list of interior cells that the ESM generates and merges them down to fit in a number of MSBs */
    /* Calculates information for which msbs contain which cells, and what drawgroups each cell will have */
    /* class name is in fact a pun: layOUT for exterior cells and layINT for interior cells */
    class Layint {
        public static List<Layint> Calculate(ESM esm, int msbBudget) {
            Log.Info(0, "Calculating interior cell merge...");

            List<Cell> cells = esm.GetInteriorCells();
            List<Layint> layints = new();

            int cellsPer = cells.Count / msbBudget;
            int id = 0;

            for(int i=0;i<msbBudget;i++) {
                Layint layint = new Layint(id++);

                for (int j = 0; j < cellsPer && (i * cellsPer) + j < cells.Count; j++) {
                    layint.cells.Add(cells[(i * cellsPer) + j]);
                }

                layints.Add(layint);
            }

            Log.Info(0, "Cell distribution info:");
            foreach (Layint layint in layints) {
                Log.Info(2, "Layint[" + layint.id + "] -> " + layint.cells.Count + " cells");
            }

            Log.Info(0, "");

            return layints;
        }

        private bool generated;
        public int id;
        public List<Cell> cells;
        public List<KeyValuePair<Bounds, Cell>> mergedCells;
        public Layint(int id) {
            generated = false;
            this.id = id;
            cells = new List<Cell>();
            mergedCells = new();
        }

        /* Loads cell data and calculates bounding boxes so that we can determine the spacing between each cell. */
        public void generate(ESM esm) {
            if (generated) { return; }
            int rowMax = (int)Math.Sqrt(Math.Min(DEBUG_MAX_INT_CELLS, cells.Count));

            Bounds last = null;
            float rowSpace = 0f;
            int nextId = 0;
            for (int c = 0; c < cells.Count; c++) {
                if (c > DEBUG_MAX_INT_CELLS) { break; }
                Cell cell = cells[c];

                Log.Info(0, "Loading Interior Cell: " + cell.name, "test");
                cell.Generate(esm);

                /* Generate drawgroups */
                cell.drawGroups = new uint[NUM_DRAW_GROUPS];
                cell.drawId = nextId++;
                cell.drawGroups[cell.drawId / 32] |= (uint)1 << (cell.drawId % 32);

                /* Generate bounds and offsets */     // This is far from efficent. We could pack things more tightly by testing bounding boxes or something
                int ind = c % rowMax;
                if (ind == 0) {
                    for (int i = 0; i < mergedCells.Count; i++) {
                        Bounds bb = mergedCells[i].Key;
                        rowSpace = Math.Max(rowSpace, bb.center.Z + (bb.length * .5f));
                    }
                    last = null;
                }

                Vector3 min = cell.content.Count>0?cell.content[0].position+Vector3.Zero:new(), max = min+Vector3.Zero; // End me
                foreach (Content content in cell.content) {
                    if (!PortJob.CONVERT_ALL.Contains(content.type)) { continue; }  // Only process valid world meshes
                    if (content.mesh == null || !content.mesh.Contains("\\")) { continue; } // Skip invalid or top level placeholder meshes

                    /* Calculate min and max bounds */
                    min.X = Math.Min(min.X, content.position.X);
                    max.X = Math.Max(max.X, content.position.X);
                    min.Y = Math.Min(min.Y, content.position.Y);
                    max.Y = Math.Max(max.Y, content.position.Y);
                    min.Z = Math.Min(min.Z, content.position.Z);
                    max.Z = Math.Max(max.Z, content.position.Z);
                }

                Vector3 offset = Vector3.Add(Vector3.Multiply(min, -1f), Vector3.Multiply(Vector3.Subtract(max, min), -.5f));

                float width = max.X - min.X;
                float height = max.Y - min.Y;
                float length = max.Z - min.Z;
                Vector3 position = new();
                if(last != null) { position.X = last.center.X + (last.width * .5f) + (width * .5f) + INTERIOR_CELL_BUFFER + INTERIOR_CELL_OVERSIZE; }
                position.Z += rowSpace + (length * .5f) + INTERIOR_CELL_BUFFER + INTERIOR_CELL_OVERSIZE;

                Bounds bounds = new(offset, position, width + INTERIOR_CELL_OVERSIZE, length + INTERIOR_CELL_OVERSIZE, height + INTERIOR_CELL_OVERSIZE);
                last = bounds;

                mergedCells.Add(new KeyValuePair<Bounds, Cell>(bounds, cell));
            }
            generated = true;
        }
    }
    public class Bounds {
        public float width, length, height;
        public Vector3 offset;      // Calculated position offset to add to the contents of the cell this bounds is tied to. Will offset the contents of that cell to be centered on [0,0,0]
        public Vector3 center;      // Center of the bounds. Add this to content after offset to move it to the position of this bounds!
        public Bounds(Vector3 offset, Vector3 center, float width, float length, float height) {
            this.offset = offset;
            this.center = center;
            this.width = width;
            this.length = length;
            this.height = height;
        }
    }
}
