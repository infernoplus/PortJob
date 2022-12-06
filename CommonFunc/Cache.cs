﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CommonFunc {
    /* Contains data about files converted by MassConvert */
    /* Written as JSON by FBXConverter and read back in by PortJob */
    public class Cache {
        public List<ObjectInfo> objects;  // Static objects
        public List<ObjActInfo> objActs;  // Animated/activatable objects (doors n' such)
        public List<ModelInfo> models;
        public List<TerrainInfo> terrains;
        public List<WaterInfo> waters;
        public Cache() {
            objects = new();
            objActs = new();
            models = new();
            terrains = new();
            waters = new();
        }

        public ObjectInfo GetObjectInfo(string name) {
            foreach (ObjectInfo objectInfo in objects) {
                if (objectInfo.name == name.ToLower()) { return objectInfo; }
            }
            return null;
        }

        public ObjActInfo GetObjActInfo(string name) {
            foreach (ObjActInfo objActInfo in objActs) {
                if (objActInfo.name == name.ToLower()) { return objActInfo; }
            }
            return null;
        }

        public ModelInfo GetModelInfo(string name) {
            foreach (ModelInfo modelInfo in models) {
                if (modelInfo.name == name.ToLower()) { return modelInfo; }
            }
            return null;
        }

        public TerrainInfo GetTerrainInfo(Int2 position) {
            foreach(TerrainInfo terrainInfo in terrains) {
                if(terrainInfo.position == position) { return terrainInfo; }
            }
            return null;
        }

        public WaterInfo GetWaterInfo(int layout) {
            foreach (WaterInfo waterInfo in waters) {
                if (waterInfo.layout == layout) { return waterInfo; }
            }
            return null;
        }

        public WaterInfo GetWaterInfo(string cell) {
            foreach (WaterInfo waterInfo in waters) {
                if (waterInfo.cell == cell) { return waterInfo; }
            }
            return null;
        }
    }

    public class ObjActInfo {
        public string name; // Original esm ref id
        public ModelInfo model;

        public int id;
        public Vector3 offset;
        public float orientation;  // Radians
        public bool invert;
        public ObjActInfo(string name, ModelInfo model) {
            this.name = name.ToLower();
            this.model = model;
            offset = Vector3.Zero;
            orientation = 0f;
            invert = false;
        }
    }

    public class ObjectInfo {
        public string name; // Original esm ref id
        public ModelInfo model;

        public int id;
        public ObjectInfo(string name, ModelInfo model) {
            this.name = name.ToLower();
            this.model = model;
        }
    }

    public class TerrainInfo {
        public Int2 position;   // Location in world cell grid
        public string high, low; // Relative path from the 'cache' folder to the converted flver file. High is high detail (real shit), low is low detail (for distant lod)
        public CollisionInfo collision;
        public List<TextureInfo> textures; // All generated tpf files

        public float min;          // Lowest point of this terrain. Used for deciding if water will be placed in a cell.
        public int idLow, idHigh;  // Model ID number, the last 6 digits in a model filename. EXAMPLE: m30_00_00_00_005521.mapbnd.dcx or h30_00_00_00_000228.hkx.dcx
        public TerrainInfo(Int2 position, string high, string low, CollisionInfo collision) {
            this.position = position;
            this.high = high;
            this.low = low;
            this.collision = collision;
            textures = new();

            min = 0;
            idLow = -1;
            idHigh = -1;
        }
    }

    public class WaterInfo {
        public string path; // Relative path from the 'cache' folder to the converted flver file
        public List<TextureInfo> textures; // All generated tpf files

        public int id;
        public string cell; // if this is interior water we identify it by the cell name
        public int layout; // if this is an exterior water we identify it by the layout id
        public WaterInfo(string path) {
            this.path = path;
            textures = new();

            id = -1;
            cell = null;
            layout = -1;
        }
    }

    public class ModelInfo {
        public string name; // Original nif name, for lookup from ESM records
        public string path; // Relative path from the 'cache' folder to the converted flver file
        public List<CollisionInfo> collisions; // All generated HKX collision files
        public List<TextureInfo> textures; // All generated tpf files

        public int id;  // Model ID number, the last 6 digits in a model filename. EXAMPLE: m30_00_00_00_005521.mapbnd.dcx

        public float radius; // Bounding radius, for use in distant LOD generation
        public ModelInfo(string name, string path) {
            this.name = name.ToLower();
            this.path = path;
            collisions = new();
            textures = new();

            id = -1;
            radius = -1f;
        }

        public CollisionInfo GetCollision(float scale) {
            int stepScale = (int)(Math.Round((scale * 100f) / 10)*10);  // Scale is rounded to nearest 10% to reduce the number of collision files we have to generate
            foreach (CollisionInfo collisionInfo in collisions) {
                if (collisionInfo.scale == stepScale) { return collisionInfo; }
            }
            return null;
        }
    }

    public class CollisionInfo {
        public string name; // Original nif name, for lookup from ESM records
        public string obj;  // Relative path from the 'cache' folder to the converted obj file
        public string path; // Relative path from the 'cache' folder to the converted hkx file
        public int scale;   // Scale value of this collision. HKX collision can't be scaled in engine so we hard scale the models and have multiple versions. Using ints for accuracy. 100 = 1.0f

        public int id; // Collision ID number, the last 6 digits in a collision filename. EXAMPLE: h30_00_00_00_000228.hkx.dcx
        public CollisionInfo(string name, string path, int scale) {
            this.name = name.ToLower();
            this.obj = path;
            this.path = path.Replace(".obj", ".hkx.dcx");
            this.scale = scale;

            id = -1;
        }
    }

    public class TextureInfo {
        public string name; // Original dds texture name for lookup
        public string path; // Relative path from the 'cache' folder to the converted tpf file
        public TextureInfo(string name, string path) {
            this.name = name.ToLower();
            this.path = path;
        }
    }
}
