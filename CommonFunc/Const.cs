using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonFunc {
    public static class Const {
        #region General
        public static readonly float GLOBAL_SCALE = 0.01f;
        public static string OutputPath => Settings.OutputPath;
        public static string MorrowindPath => Settings.MorrowindPath;
        #endregion

        #region TerrainConverter
        public const int FLVER_VERSION = 0x20014;
        public const byte TPF_ENCODING = 2;
        public const byte TPF_FLAG_2 = 3;

        public const byte FLVER_UNK_0x5C = 0;
        public const int FLVER_UNK_0x68 = 4;

        public const string HARDCODE_TEXTURE_KEY = "g_DetailBumpmap";
        public const string HARDCODE_TEXTURE_VAL = "";
        public const byte HARDCODE_TEXTURE_UNK10 = 0x01;
        public const bool HARDCODE_TEXTURE_UNK11 = true;

        public const bool ABSOLUTE_VERT_POSITIONS = true;

        public const int FACESET_MAX_TRIANGLES = 65535; // Max triangles in a mesh for the DS1 engine.


        public const int EXTERIOR_BOUNDS = 40; // +/- Bounds of the cell grid we consider to be the 'Exterior'
        public const int CELL_THREADS = 16;
        #endregion

        #region Layouts
        public static readonly Box GRID_SIZE = new(-18, -17, 24, 28);
        public static readonly Int2 MSB_SIZE = new(8, 8);
        public static readonly int LAYOUT_CELL_BUDGET = MSB_SIZE.x * MSB_SIZE.y;
        public static readonly int LAYOUT_CELL_BUDGET_MAX = (int)(LAYOUT_CELL_BUDGET * 1.25);
        #endregion

        #region Cell
        public static readonly float CELL_SIZE = 8192f * GLOBAL_SCALE;
        public static readonly int CELL_GRID_SIZE = 64;
        #endregion
    }
}
