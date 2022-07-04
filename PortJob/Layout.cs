using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    /* This class takes the list of exterior cells that ESM generates and calculates the layout for MSBs to use */
    /* Calculates information for which msbs contain which cells, what connect collisions to put in which cells, and what drawgroups each cell will have */
    public class Layout {
        public static List<Layout> CalculateLayout(ESM esm) {
            /* Const settings */
            // Nord how the fuck do I make these actual const class values it keeps giving me errors reeeeeeeeeeeeeeeeeeeeeeeeeee
            Box GRID_SIZE = new Box(-18, -17, 24, 28);
            Int2 MSB_SIZE = new Int2(8, 8);
            int LAYOUT_CELL_BUDGET = MSB_SIZE.x * MSB_SIZE.y;
            int LAYOUT_CELL_BUDGET_MAX = (int)(LAYOUT_CELL_BUDGET * 1.25);

            Box MSB_GRID_SIZE = new Box(
                (int)Math.Floor((float)GRID_SIZE.x1 / (float)MSB_SIZE.x),
                (int)Math.Floor((float)GRID_SIZE.y1 / (float)MSB_SIZE.y),
                ((int)Math.Floor((float)GRID_SIZE.x2 / (float)MSB_SIZE.x)) + ((GRID_SIZE.x2 - GRID_SIZE.x1) % (MSB_SIZE.x / 2) > 0?1:0),
                ((int)Math.Floor((float)GRID_SIZE.y2 / (float)MSB_SIZE.y)) + ((GRID_SIZE.y2 - GRID_SIZE.y1) % (MSB_SIZE.y / 2) > 0?1:0)
            );

            Int2[] CELL_BORDER_OFFSETS = new[] {
                new Int2(1, 0),
                new Int2(0, 1),
                new Int2(-1, 0),
                new Int2(0, -1),
                new Int2(1, 1),
                new Int2(-1, -1),
                new Int2(-1, 1),
                new Int2(1, -1)
            };

            int INT_SIZE = 32;
            int NUM_DRAW_GROUPS = 8;
            int MAX_DRAW = INT_SIZE * NUM_DRAW_GROUPS;   // Maximum number of drawgroups 

            /* Gen Lists */
            List<Cell> cells = esm.GetExteriorCells();
            List<Layout> layouts = new();

            /* Helper functions */
            Layout GetLayoutById(int id) {
                for (int i = 0; i < layouts.Count; i++) {
                    if (layouts[i].id == id) { return layouts[i]; }
                }
                Console.WriteLine("FAILED TO FIND MSB BY ID: " + id);
                return null;
            };

            Cell getCellByPosition(Int2 position) {
                for (int i = 0; i < cells.Count; i++) {
                    Cell cell = cells[i];

                    if (position.x == cell.position.x && position.y == cell.position.y) {
                        return cell;
                    }
                }
                return null;
            }

            /* Initialize cell fields */
            for (int i = 0; i < cells.Count; i++) {
                Cell cell = cells[i];
                cell.drawGroups = new uint[NUM_DRAW_GROUPS];
                cell.pairs = new();
                cell.connects = new();
            }

            /* Generate msbs and add cells to msbs */
            int nextId = 0;
            for (int y = MSB_GRID_SIZE.y1; y < MSB_GRID_SIZE.y2; y++) {
                for (int x = MSB_GRID_SIZE.x1; x < MSB_GRID_SIZE.x2 + 1; x++) {
                    int w = MSB_SIZE.x / 2;
                    int h = MSB_SIZE.y / 2;
                    int xoff = (Math.Abs(y % 2) > 0 ? MSB_SIZE.x / 2 : 0);

                    Box bounds = new Box(
                        (x * MSB_SIZE.x) + w - xoff,
                        (y * MSB_SIZE.y) + h,
                        (x * MSB_SIZE.x) + (MSB_SIZE.x - 1) + w - xoff,
                        (y * MSB_SIZE.y) + (MSB_SIZE.y - 1) + h
                    );
                    Layout layout = new Layout(nextId, bounds);

                    for (int i = 0; i < cells.Count; i++) {
                        Cell cell = cells[i];
                        if (
                            cell.position.x >= layout.bounds.x1 &&
                            cell.position.y >= layout.bounds.y1 &&
                            cell.position.x <= layout.bounds.x2 &&
                            cell.position.y <= layout.bounds.y2
                        ) {
                            cell.layout = layout;
                            layout.cells.Add(cell);
                        }
                    }

                    // Test and see if the msb is not empty before we decide to use it
                    if (layout.cells.Count > 0) {
                        nextId++;
                        layouts.Add(layout);
                    }
                }
            }

            /* Merge MSBs that are not using their full cell budget */
            for (int i = 0; i < layouts.Count; i++) {
                Layout layout = layouts[i];

                if (layout.cells.Count >= LAYOUT_CELL_BUDGET) { continue; }

                List<Layout> borders = new();
                for (int j = 0; j < layout.cells.Count; j++) {
                    Cell cell = layout.cells[j];
                    for (int k = 0; k < CELL_BORDER_OFFSETS.Length; k++) {
                        Int2 offset = CELL_BORDER_OFFSETS[k];
                        Int2 position = new Int2(cell.position.x + offset.x, cell.position.y + offset.y);
                        Cell border = getCellByPosition(position);

                        if (border != null && border.layout != layout && !(borders.Contains(border.layout))) {
                            borders.Add(border.layout);
                        }
                    }
                }

                Layout small = null;
                for (int j = 0; j < borders.Count; j++) {
                    Layout border = borders[j];
                    if (border.cells.Count < LAYOUT_CELL_BUDGET) {
                        if (small == null || border.cells.Count < small.cells.Count) {
                            small = border;
                        }
                    }
                }

                if (small != null && layout.cells.Count + small.cells.Count < LAYOUT_CELL_BUDGET_MAX) {
                    Console.WriteLine("Merging msb[" + layout.id + "] and layout[" + small.id + "].");

                    for (int k = 0; k < layout.cells.Count; k++) {
                        Cell cell = layout.cells[k];

                        cell.layout = small;
                        small.cells.Add(cell);
                    }

                    layouts.RemoveAt(i);
                    i = -1;
                }
            }

            /* Sanity Test */
            List<Cell> fuckers = new();
            for (int i = 0; i < cells.Count; i++) {
                if(cells[i].layout == null) {
                    fuckers.Add(cells[i]);
                }
            }
            Console.WriteLine("fuckers: " + fuckers.Count);

            /* Find cell border pairs and connect collisions */
            for (int i = 0; i < layouts.Count; i++) {
                Layout layout = layouts[i];

                for (int j = 0; j < layout.cells.Count; j++) {
                    Cell cell = layout.cells[j];

                    List<Layout> hits = new(); // MSBS we hit in this round of tests, 1 cell per msb, this gives us only direct adjacent cells AND solves corner cases
                    for (int k = 0; k < CELL_BORDER_OFFSETS.Length; k++) {
                        Int2 offset = CELL_BORDER_OFFSETS[k];
                        Int2 position = new Int2(cell.position.x + offset.x, cell.position.y + offset.y);

                        Cell border = getCellByPosition(position);

                        if (border != null && cell.layout != border.layout) {
                            if (hits.Contains(border.layout)) { continue; } // Skip if this is not the first border pair for this msb

                            cell.pairs.Add(border);
                            hits.Add(border.layout);

                            if (!cell.connects.Contains(border.layout)) { cell.connects.Add(border.layout); }
                            if (!layout.connects.Contains(border.layout)) { layout.connects.Add(border.layout); }
                        }
                    }
                }
            }

            /* Generate drawIDs */
            for (int i = 0; i < layouts.Count; i++) {
                Layout layout = layouts[i];

                /* Returns next unused id in this MSB, this is so we don't accidentally double dip on a cell that has a paired id with another msb */
                nextId = 0;
                int getNextId() {
                    List<Layout> localLayouts = new();
                    localLayouts.Add(layout);

                    for (var j = 0; j < layout.connects.Count; j++) {
                        localLayouts.Add(layout.connects[j]);
                    }

                    for (int j = 0; j < localLayouts.Count; j++) {
                        Layout localLayout = localLayouts[j];

                        for (int k = 0; k < localLayout.cells.Count; k++) {
                            Cell localCell = localLayout.cells[k];

                            if (localCell.drawId == nextId) {
                                ++nextId;
                                return getNextId();
                            }
                        }
                    }
                    return nextId++;
                };

                for (int j = 0; j < layout.cells.Count; j++) {
                    Cell cell = layout.cells[j];

                    if (cell.drawId != -1) { continue; } // Pairs may already be generated so we check and skip
                    if (nextId >= MAX_DRAW) { Console.WriteLine("REACHED MAXIMUM DRAW GROUPS FOR MSB!!! VERY BAD!!!"); }

                    /* Generate draw id*/
                    bool pairSet = false; // If a pair already has a generated draw id we YOINK it, otherwise we generate and distribute to our pairSet
                    for (int k = 0; k < cell.pairs.Count; k++) {
                        Cell pair = cell.pairs[k];
                        if (pair.drawId != -1) {
                            cell.drawId = pair.drawId;
                            pairSet = true;
                            break;
                        }
                    }

                    if (!pairSet) {
                        cell.drawId = getNextId();

                        /* Copy generated drawid into pairs */
                        for (int k = 0; k < cell.pairs.Count; k++) {
                            Cell pair = cell.pairs[k];

                            pair.drawId = cell.drawId;
                        }
                    }
                }
            }

            /* Solve drawgroups */
            for (int i = 0; i < layouts.Count; i++) {
                Layout layout = layouts[i];

                for (int j = 0; j < layout.cells.Count; j++) {
                    Cell cell = layout.cells[j];

                    if(cell.drawId < 0) { Log.Error(0, "Invalid drawId found during drawgroup solving!"); }
                    cell.drawGroups[cell.drawId / 32] |= (uint)1 << (cell.drawId % 32);

                    for (int k = 0; k < CELL_BORDER_OFFSETS.Length; k++) {
                        Int2 offset = CELL_BORDER_OFFSETS[k];
                        Int2 position = new Int2(cell.position.x + offset.x, cell.position.y + offset.y);

                        Cell border = getCellByPosition(position);
                        if (border != null) {
                            cell.drawGroups[border.drawId / 32] |= (uint)1 << (border.drawId % 32);
                        }
                    }
                }
            }

            /* Test solution */
            int[] visRes = new int[32];
            for (int i = 0; i < cells.Count; i++) {
                Cell cell = cells[i];

                List<Cell> loaded = new(); // List of cells that would be loaded if you were standing in *this* cell

                for (int j = 0; j < cell.layout.cells.Count; j++) {
                    loaded.Add(cell.layout.cells[j]);
                }

                for (int j = 0; j < cell.connects.Count; j++) {
                    Layout layout = cell.connects[j];

                    for (int k = 0; k < layout.cells.Count; k++) {
                        loaded.Add(layout.cells[k]);
                    }
                }

                int vis = 0; // Count up how many other cells are visible via drawgroups against loaded cells list
                for (int j = 0; j < loaded.Count; j++) {
                    Cell other = loaded[j];

                    if ((cell.drawGroups[other.drawId / 32] & (1 << (other.drawId % 32))) > 0) {
                        vis++;
                    }
                }
                visRes[vis]++;
            }

            /* Write out results */
            Console.WriteLine("----");
            for (int i = 0; i < visRes.Length; i++) {
                if (visRes[i] > 0) {
                    Console.WriteLine(visRes[i] + " drawgroups with " + i + " cells visible.");
                }
            }

            Console.WriteLine("----");
            int[] msbRes = new int[LAYOUT_CELL_BUDGET_MAX];
            for (int i = 0; i < layouts.Count; i++) {
                msbRes[layouts[i].cells.Count]++;
            }

            for (int i = 0; i < msbRes.Length; i++) {
                if (msbRes[i] > 0) {
                    Console.WriteLine(msbRes[i] + " msbs containing " + i + " cells.");
                }
            }

            Console.WriteLine("----");
            for (int i = 0; i < cells.Count; i++) {
                Cell cell = cells[i];
                if (cell.connects.Count > 2) {
                    Console.WriteLine("Cell with more than 2 connections: [" + cell.position.x + ", " + cell.position.y + "]");
                }
            }

            /* Recalculate ID to match block ID */
            for(int i=1;i<layouts.Count;i++) {
                Layout layout = layouts[i];
                layout.id = i;
            }

            return layouts;
        }
           
        public int id;
        public Box bounds;
        public List<Cell> cells;
        public List<Layout> connects;
        public Layout(int id, Box bounds) {
            this.id = id;
            this.bounds = bounds;
            cells = new();
            connects = new();
        }
    }
    
    public class Box {
        public int x1, y1, x2, y2;
        public Box(int x1, int y1, int x2, int y2) {
            this.x1 = x1; this.y1 = y1;
            this.x2 = x2; this.y2 = y2;
        }
    }
}
