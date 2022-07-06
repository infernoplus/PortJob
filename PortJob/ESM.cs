using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;
using System.Numerics;
using SoulsFormats;
using System.Threading;

namespace PortJob {
    /* Loads and handles the JSON file of the morrowind.esm that tes3conv outputs */
    public class ESM {
        private JArray json; // Full unfiltered json of the morrowind.json
        private Dictionary<Type, List<JObject>> recordsMap;
        public List<Cell> exteriorCells, interiorCells;

        public enum Type {
            Header, GameSetting, GlobalVariable, Class, Faction, Race, Sound, Skill, MagicEffect, Script, Region, Birthsign, LandscapeTexture, Spell, Static, Door,
            MiscItem, Weapon, Container, Creature, Bodypart, Light, Enchantment, Npc, Armor, Clothing, RepairTool, Activator, Apparatus, Lockpick, Probe, Ingredient,
            Book, Alchemy, LevelledItem, LevelledCreature, Cell, Landscape, PathGrid, SoundGen, Dialogue, Info
        }

        public ESM(string path) {
            DateTime startParse = DateTime.Now;
            JsonSerializer serializer = new();
            using (FileStream s = File.Open(path, FileMode.Open))
            using (StreamReader sr = new StreamReader(s))
            using (JsonReader reader = new JsonTextReader(sr)) {
                while (!sr.EndOfStream) {
                    json = serializer.Deserialize<JArray>(reader);
                }
            }

            //string data = File.ReadAllTextAsync(path).Result;
            //json = JArray.Parse(data);
            Log.Info(0, $"Parse Json time: {DateTime.Now - startParse}");

            recordsMap = new Dictionary<Type, List<JObject>>();
            foreach (string name in Enum.GetNames(typeof(Type))) {
                Enum.TryParse(name, out Type type);
                recordsMap.Add(type, new List<JObject>());
            }

            for (int i = 0; i < json.Count; i++) {
                JObject record = (JObject)json[i];
                foreach (string name in Enum.GetNames(typeof(Type))) {
                    if (record["type"].ToString() == name) {
                        Enum.TryParse(name, out Type type);
                        recordsMap[type].Add(record);
                    }
                }
            }

            foreach (string name in Enum.GetNames(typeof(Type))) {
                Enum.TryParse(name, out Type type);
                Console.WriteLine(name + ": " + recordsMap[type].Count);
            }

            DateTime startLoadCells = DateTime.Now;
            LoadCells();
            Log.Info(0, $"Load Cells time: {DateTime.Now - startLoadCells}");
        }

        const int EXTERIOR_BOUNDS = 40; // +/- Bounds of the cell grid we consider to be the 'Exterior'

        private void LoadCells() {
            exteriorCells = new();
            interiorCells = new();

            List<JObject> cells = recordsMap[Type.Cell];
            int partitionSize = (int)Math.Ceiling(cells.Count / 16f);
            List<CellFactory> cellFactories = new();

            for (int i = 0; i < 16; i++) {
                int start = i * partitionSize;
                int end = start + partitionSize;
                CellFactory cellFactory = new(this,cells, start, end);
                cellFactories.Add(cellFactory);
                cellFactory.Start();
            }


            while (true) {
                bool workerThreadsDone = true;
                foreach (CellFactory factory in cellFactories) {
                    workerThreadsDone &= factory.IsDone;
                }

                if (workerThreadsDone)
                    break;
            }

            int count = 0;
            foreach (CellFactory cellFactory in cellFactories) {
                foreach (Cell cell in cellFactory.ProcessedCells) {
                    AddCell(cell);
                    count++;
                }
            }

            Log.Info(0,$"Processed: {count} == Json: {cells.Count}");
        }

        private void AddCell(Cell genCell) {
            if (genCell.content.Count < 1) return; // Cull cells with nothing in them. Removes most blank ocean cells which we really don't need.

            if (genCell.position.x >= -EXTERIOR_BOUNDS &&
                genCell.position.x <= EXTERIOR_BOUNDS &&
                genCell.position.y >= -EXTERIOR_BOUNDS &&
                genCell.position.y <= EXTERIOR_BOUNDS)
            {
                exteriorCells.Add(genCell);
            }
            else
            {
                interiorCells.Add(genCell);
            }
        }

        public List<Cell> GetExteriorCells() {
            return exteriorCells;
        }

        /* List of types that we should search for references */
        public readonly Type[] VALID_CONTENT_TYPES = {
            Type.Sound, Type.Skill, Type.Region, Type.Static, Type.Door, Type.MiscItem, Type.Weapon, Type.Container, Type.Creature, Type.Bodypart, Type.Light, Type.Npc,
            Type.Armor, Type.Clothing, Type.RepairTool, Type.Activator, Type.Apparatus, Type.Lockpick, Type.Probe, Type.Ingredient, Type.Book, Type.Alchemy, Type.LevelledItem,
            Type.LevelledCreature, Type.PathGrid, Type.SoundGen
        };

        /* References don't contain any explicit 'type' data so... we just gotta go find it lol */
        public TypedRecord FindRecordByID(string id) {
            foreach (Type type in VALID_CONTENT_TYPES) {
                List<JObject> records = recordsMap[type];

                for (int i = 0; i < records.Count; i++) {
                    JObject record = records[i];
                    if (record["id"] != null && record["id"].ToString() == id) {
                        return new TypedRecord(type, record);
                    }
                }
            }
            return null; // Not found!
        }
    }

    public class Cell {
        public static readonly float CELL_SIZE = 81.92f;

        public readonly string name;
        public readonly string region;
        public readonly Int2 position;  // Position on the cell grid

        public readonly Vector3 center; // Float center point of cell

        public readonly int flag;
        public readonly int[] flags;

        public readonly List<Content> content;

        /* These fields are used by Layout for stuff */
        public Layout layout;         // Parent layout
        public int drawId;            // Drawgroup ID, value also correponds to the bitwise (1 << id)
        public uint[] drawGroups;
        public List<Cell> pairs;      // Cells that it borders in other msbs, these will have 'paired draw ids'
        public List<Layout> connects; // Connect collisions we need to generate

        public Cell(ESM esm, JObject data) {
            name = data["id"].ToString();
            region = data["region"] != null ? data["region"].ToString() : "null";

            int x = int.Parse(data["data"]["grid"][0].ToString());
            int y = int.Parse(data["data"]["grid"][1].ToString());
            position = new Int2(x, y);

            center = new Vector3((CELL_SIZE * position.x) + (CELL_SIZE * 0.5f), 0.0f, (CELL_SIZE * position.y) + (CELL_SIZE * 0.5f));

            flag = int.Parse(data["data"]["flags"].ToString());

            JArray flc = (JArray)(data["flags"]);
            flags = new int[flc.Count];
            for (int i = 0; i < flc.Count; i++) {
                flags[i] = int.Parse(flc[i].ToString());
            }

            content = new List<Content>();

            JArray refc = (JArray)(data["references"]);
            for (int i = 0; i < refc.Count; i++) {
                JObject reference = (JObject)(refc[i]);
                content.Add(new Content(esm, reference));
            }

            /* These fields are used by Layout for stuff */
            layout = null;
            drawId = -1;
            drawGroups = null;
            pairs = null;
            connects = null;
        }
    }

    public class CellFactory {
        public bool IsDone { get; private set; }
        private ESM _esm { get; }
        private List<JObject> _cells { get; }
        private int _start { get; }
        private int _end { get; }
        private Thread _thread { get; }
        private List<Cell> _processedCells { get; }
        public List<Cell> ProcessedCells {
            get {
                _thread.Join();
                return _processedCells;
            }
        }

        public CellFactory(ESM esm, List<JObject> cells, int start, int end) {
            _processedCells = new();
            _esm = esm;
            _cells = cells;
            _start = start;
            _end = end;
            _thread = new(ProcessCell);

        }

        public void Start() {
            _thread.Start();
        }

        private void ProcessCell()
        {
            for (int i = _start; i < _cells.Count && i < _end; i++)
            {
                Cell genCell = new(_esm, _cells[i]);
                _processedCells.Add(genCell);
            }
            IsDone = true;
        }
    }

    public class Content {
        public readonly string id;
        public readonly ESM.Type type;

        public readonly string mesh;

        public readonly Vector3 position, rotation;

        public Content(ESM esm, JObject data) {
            id = data["id"].ToString();

            TypedRecord tr = esm.FindRecordByID(id);
            type = tr.type;

            if (tr.record["mesh"] != null) { mesh = tr.record["mesh"].ToString(); }

            float x = float.Parse(((JArray)(data["translation"]))[0].ToString());
            float z = float.Parse(((JArray)(data["translation"]))[1].ToString());
            float y = float.Parse(((JArray)(data["translation"]))[2].ToString());

            float i = float.Parse(((JArray)(data["rotation"]))[0].ToString());
            float k = float.Parse(((JArray)(data["rotation"]))[1].ToString());
            float j = float.Parse(((JArray)(data["rotation"]))[2].ToString()) - (float)Math.PI;

            position = new Vector3(x, y, z) * FBXConverter.GLOBAL_SCALE;
            rotation = new Vector3(i, j, k) * (float)(180 / Math.PI);
        }
    }

    public class Int2 {
        public readonly int x, y;
        public Int2(int x, int y) {
            this.x = x; this.y = y;
        }
    }

    public class TypedRecord {
        public ESM.Type type;
        public JObject record;

        public TypedRecord(ESM.Type t, JObject r) {
            type = t;
            record = r;
        }
    }
}
