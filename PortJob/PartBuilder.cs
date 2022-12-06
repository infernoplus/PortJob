using CommonFunc;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    class PartBuilder {
        /* Moved many repetivie boilerplate MSB part generation code blocks into this class */
        public static MapColPair MakeTerrain(Layout layout, Cell cell, TerrainInfo terrainInfo) {
            int area = Const.EXT_AREA; int block = layout.id;
            uint[] drawGroup = layout.drawGroups[cell];
            uint[] displayGroup = layout.displayGroups[cell];

            /* Terrain Map Piece */
            MSB3.Part.MapPiece terrain = new();
            terrain.ModelName = $"m{terrainInfo.idHigh:D6}";
            terrain.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\layout_{terrainInfo.idHigh:D6}.SIB";
            terrain.Position = cell.area.center;
            terrain.Rotation = Vector3.Zero;
            terrain.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < drawGroup.Length; k++) {
                terrain.DrawGroups[k] = drawGroup[k];
                terrain.DispGroups[k] = 0;
                terrain.BackreadGroups[k] = 0;
            }
            terrain.ShadowSource = true;
            terrain.DrawByReflectCam = true;
            terrain.Name = terrain.ModelName + "_0000";   // Terrains are unique so no need to worry about counts
            terrain.UnkE0E = -1;
            terrain.LodParamID = 19; //Param for: Don't switch to LOD models 

            /* Terrain collision */
            MSB3.Part.Collision terrainCol = new();
            terrainCol.HitFilterID = 8;
            terrainCol.ModelName = $"h{terrainInfo.collision.id:D6}";
            terrainCol.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB";
            terrainCol.Position = terrain.Position;
            terrainCol.Rotation = terrain.Rotation;
            terrainCol.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < displayGroup.Length; k++) {
                terrainCol.DrawGroups[k] = displayGroup[k];
                terrainCol.DispGroups[k] = displayGroup[k];
                terrainCol.BackreadGroups[k] = displayGroup[k];
            }
            terrainCol.Name = terrainCol.ModelName + "_0000"; // Same as above
            terrainCol.LodParamID = -1;
            terrainCol.UnkE0E = -1;
            terrainCol.Gparam.LightSetID = 100;

            return new MapColPair(terrain, terrainCol);
        }

        public static MSB3.Part.MapPiece MakeLowTerrain(Layout layout, Cell cell, TerrainInfo terrainInfo) {
            int area = Const.EXT_AREA; int block = layout.id;
            uint[] inverseGroup = layout.inverseGroups[cell];

            /* Terrain Map Piece */
            MSB3.Part.MapPiece terrain = new();
            terrain.ModelName = $"m{terrainInfo.idLow:D6}";
            terrain.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\layout_{terrainInfo.idLow:D6}.SIB";
            terrain.Position = cell.area.center;
            terrain.Rotation = Vector3.Zero;
            terrain.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < inverseGroup.Length; k++) {
                terrain.DrawGroups[k] = inverseGroup[k];
                terrain.DispGroups[k] = 0;
                terrain.BackreadGroups[k] = 0;
            }
            terrain.ShadowSource = true;
            terrain.DrawByReflectCam = true;
            terrain.Name = terrain.ModelName + "_0000";   // Terrains are unique so no need to worry about counts
            terrain.UnkE0E = -1;
            terrain.LodParamID = 19; //Param for: Don't switch to LOD models

            return terrain;
        }

        public static MSB3.Part.MapPiece MakeWater(Layout layout, WaterInfo waterInfo) {
            uint[] drawGroup = new uint[] { uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue };
            Vector3 position = new Vector3(layout.center.x, 0, layout.center.y) * Const.CELL_SIZE;
            return MakeWater(Const.EXT_AREA, layout.id, waterInfo, drawGroup, -0.025f, position); // The -0.025f is to avoid zfighting on terrain at exactly 0f
        }

        public static MSB3.Part.MapPiece MakeWater(Layint layint, Cell cell, WaterInfo waterInfo, Bounds bounds) {
            uint[] drawGroup = layint.drawGroups[cell];
            return MakeWater(Const.INT_AREA, layint.id, waterInfo, drawGroup, cell.water.height, Vector3.Zero, bounds);
        }

        private static MSB3.Part.MapPiece MakeWater(int area, int block, WaterInfo waterInfo, uint[] drawGroup, float height, Vector3 position, Bounds? bounds = null) {
            /* Static Mesh Map Piece */
            MSB3.Part.MapPiece water = new();
            water.ModelName = $"m{waterInfo.id:D6}";
            water.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\layout_{waterInfo.id:D6}.SIB";
            water.Position = (bounds != null ? bounds.center : position) + new Vector3(0, height, 0);
            water.Rotation = Vector3.Zero;
            water.Scale = Vector3.One;
            water.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < drawGroup.Length; k++) {
                water.DrawGroups[k] = drawGroup[k];
                water.DispGroups[k] = 0;
                water.BackreadGroups[k] = 0;
            }
            water.ShadowSource = false;
            water.ShadowDest = true;
            water.DrawByReflectCam = true;
            water.Name = $"{water.ModelName}_0000";
            water.UnkE0E = -1;
            water.LodParamID = 19; //Param for: Don't switch to LOD models

            return water;
        }

        public static MapColPair MakeStatic(Layout layout, Cell cell, Content content, ModelInfo modelInfo, Counters counters) {
            uint[] drawGroup = layout.GetDrawGroup(cell, modelInfo, content.scale);
            uint[] displayGroup = layout.displayGroups[cell];
            return MakeStatic(Const.EXT_AREA, layout.id, cell, content, modelInfo, counters, drawGroup, displayGroup);
        }

        public static MapColPair MakeStatic(Layint layint, Cell cell, Content content, ModelInfo modelInfo, Counters counters, Bounds bounds) {
            uint[] drawGroup = layint.drawGroups[cell];
            uint[] displayGroup = layint.displayGroups[cell];
            return MakeStatic(Const.INT_AREA, layint.id, cell, content, modelInfo, counters, drawGroup, displayGroup, bounds);
        }

        private static MapColPair MakeStatic(int area, int block, Cell cell, Content content, ModelInfo modelInfo, Counters counters, uint[] drawGroup, uint[] displayGroup, Bounds? bounds = null) {
            /* Static Mesh Map Piece */
            MSB3.Part.MapPiece mp = new();
            mp.ModelName = $"m{modelInfo.id:D6}";
            mp.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\layout_{modelInfo.id:D6}.SIB";
            mp.Position = bounds != null ? Vector3.Add(Vector3.Add(content.position, bounds.offset), bounds.center) : content.position;
            mp.Rotation = content.rotation;
            mp.Scale = new Vector3(content.scale);
            mp.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < drawGroup.Length; k++) {
                mp.DrawGroups[k] = drawGroup[k];
                mp.DispGroups[k] = 0;
                mp.BackreadGroups[k] = 0;
            }
            mp.ShadowSource = true;
            mp.DrawByReflectCam = true;
            mp.Name = $"{mp.ModelName}_{counters.GetMapPiece(modelInfo):D4}";
            mp.UnkE0E = -1;
            mp.LodParamID = 19; //Param for: Don't switch to LOD models 

            /* Static Mesh Collision - (If it exists) */
            CollisionInfo collisionInfo = modelInfo.GetCollision(content.scale);
            if (collisionInfo != null) {
                MSB3.Part.Collision col = new();
                col.HitFilterID = 8;
                col.ModelName = $"h{collisionInfo.id:D6}";
                col.SibPath = $"N:\\FDP\\data\\Model\\map\\m{area:D2}_{block:D2}_00_00\\sib\\h_layout.SIB";
                col.Position = mp.Position;
                col.Rotation = mp.Rotation;
                col.MapStudioLayer = uint.MaxValue;
                for (int k = 0; k < displayGroup.Length; k++) {
                    col.DrawGroups[k] = displayGroup[k];
                    col.DispGroups[k] = displayGroup[k];
                    col.BackreadGroups[k] = displayGroup[k];
                }

                col.Name = $"{col.ModelName}_{counters.GetCollision(collisionInfo):D4}";
                col.LodParamID = -1;
                col.UnkE0E = -1;
                col.Gparam.LightSetID = 100;

                return new MapColPair(mp, col);
            }
            return new MapColPair(mp, null);
        }

        public static MSB3.Part.Object MakeStaticObject(Layout layout, Cell cell, Content content, ObjectInfo objectInfo, Counters counters) {
            uint[] drawGroup = layout.GetDrawGroup(cell, objectInfo.model, content.scale);
            return MakeStaticObject(Const.EXT_AREA, layout.id, cell, content, objectInfo, counters, drawGroup);
        }

        public static MSB3.Part.Object MakeStaticObject(Layint layint, Cell cell, Content content, ObjectInfo objectInfo, Counters counters, Bounds bounds) {
            uint[] drawGroup = layint.drawGroups[cell];
            return MakeStaticObject(Const.INT_AREA, layint.id, cell, content, objectInfo, counters, drawGroup, bounds);
        }

        private static MSB3.Part.Object MakeStaticObject(int area, int block, Cell cell, Content content, ObjectInfo objectInfo, Counters counters, uint[] drawGroup, Bounds? bounds = null) {
            /* Object */
            MSB3.Part.Object obj = new();
            obj.ModelName = $"o{objectInfo.id:D6}";
            obj.SibPath = "";
            obj.Position = bounds!=null? Vector3.Add(Vector3.Add(content.position, bounds.offset), bounds.center):content.position;
            obj.Rotation = content.rotation;
            obj.Scale = new Vector3(content.scale);
            obj.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < drawGroup.Length; k++) {
                obj.DrawGroups[k] = drawGroup[k];
                obj.DispGroups[k] = 0;
                obj.BackreadGroups[k] = 0;
            }
            //obj.CollisionName = terrainCol.Name;
            obj.ShadowDest = true;
            obj.DrawByReflectCam = true;
            obj.Name = $"{obj.ModelName}_{counters.GetObject(objectInfo):D4}";
            obj.UnkE0E = -1;
            obj.LodParamID = 19; //Param for: Don't switch to LOD models 

            return obj;
        }

        public static MSB3.Part.Object MakeLight(Layout layout, Cell cell, Content content, ObjectInfo objectInfo, Counters counters) {
            uint[] drawGroup = layout.GetDrawGroup(cell, objectInfo.model, content.scale);
            return MakeLight(Const.EXT_AREA, layout.id, cell, content, objectInfo, counters, drawGroup);
        }

        public static MSB3.Part.Object MakeLight(Layint layint, Cell cell, Content content, ObjectInfo objectInfo, Counters counters, Bounds bounds) {
            uint[] drawGroup = layint.drawGroups[cell];
            return MakeLight(Const.INT_AREA, layint.id, cell, content, objectInfo, counters, drawGroup, bounds);
        }

        private static MSB3.Part.Object MakeLight(int area, int block, Cell cell, Content content, ObjectInfo objectInfo, Counters counters, uint[] drawGroup, Bounds? bounds = null) {
            /* Object */
            MSB3.Part.Object obj = new();
            obj.ModelName = $"o{objectInfo.id:D6}";
            obj.SibPath = "";
            obj.Position = bounds != null ? Vector3.Add(Vector3.Add(content.position, bounds.offset), bounds.center) : content.position;
            obj.Rotation = content.rotation;
            obj.Scale = new Vector3(content.scale);
            obj.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < drawGroup.Length; k++) {
                obj.DrawGroups[k] = drawGroup[k];
                obj.DispGroups[k] = 0;
                obj.BackreadGroups[k] = 0;
            }
            //obj.CollisionName = terrainCol.Name;
            obj.ShadowDest = true;
            obj.DrawByReflectCam = true;
            obj.Name = $"{obj.ModelName}_{counters.GetObject(objectInfo):D4}";
            obj.UnkE0E = -1;
            obj.LodParamID = 19; //Param for: Don't switch to LOD models 
            obj.ModelSfxParamRelativeIDs[0] = 0; // object id + this number = sfx. -1 is null?

            return obj;
        }

        public static ObjActPair MakeActDoor(Layout layout, Cell cell, Content content, ObjActInfo objActInfo, Counters counters) {
            uint[] drawGroup = layout.GetDrawGroup(cell, objActInfo.model, content.scale);
            return MakeActDoor(Const.EXT_AREA, layout.id, cell, content, objActInfo, counters, drawGroup);
        }

        public static ObjActPair MakeActDoor(Layint layint, Cell cell, Content content, ObjActInfo objActInfo, Counters counters, Bounds bounds) {
            uint[] drawGroup = layint.drawGroups[cell];
            return MakeActDoor(Const.INT_AREA, layint.id, cell, content, objActInfo, counters, drawGroup, bounds);
        }

        private static ObjActPair MakeActDoor(int area, int block, Cell cell, Content content, ObjActInfo objActInfo, Counters counters, uint[] drawGroup, Bounds? bounds = null) {
            /* Object */
            const float radian = (float)Math.PI / 180f;
            const float degree = 180f / (float)Math.PI;

            MSB3.Part.Object obj = new();
            obj.ModelName = $"o{objActInfo.id:D6}";
            obj.SibPath = "";

            float cosDegrees = (float)Math.Cos((content.rotation.Y * radian) + objActInfo.orientation);
            float sinDegrees = (float)Math.Sin((content.rotation.Y * radian) + objActInfo.orientation);

            float x = (objActInfo.offset.X * cosDegrees) + (objActInfo.offset.Z * sinDegrees);
            float z = (objActInfo.offset.X * -sinDegrees) + (objActInfo.offset.Z * cosDegrees);

            Vector3 rotatedOffset = new Vector3(x, objActInfo.offset.Y, z); // @TODO: we are only accounting for rotation on Y (up). this may break for XZ rotations

            obj.Position = (bounds != null ? Vector3.Add(Vector3.Add(content.position, bounds.offset), bounds.center) : content.position) + rotatedOffset;

            obj.Rotation = new Vector3(content.rotation.X, content.rotation.Y - (objActInfo.orientation * degree) * (objActInfo.invert?-1f:1f), content.rotation.Z); // technically invert might need to be +180d
            obj.Scale = new Vector3(content.scale);
            obj.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < drawGroup.Length; k++) {
                obj.DrawGroups[k] = drawGroup[k];
                obj.DispGroups[k] = 0;
                obj.BackreadGroups[k] = 0;
            }
            obj.EntityID = Script.NewEntID();
            //obj.CollisionName = terrainCol.Name;
            obj.ShadowDest = true;
            obj.DrawByReflectCam = true;
            obj.Name = $"{obj.ModelName}_{counters.GetObjAct(objActInfo):D4}";
            obj.UnkE0E = -1;
            obj.LodParamID = 19; //Param for: Don't switch to LOD models 

            MSB3.Event.ObjAct objAct = new();
            objAct.Name = $"door {obj.Name}";
            objAct.EventID = Script.NewEvtID();
            objAct.PartName = obj.CollisionName;
            objAct.ObjActEntityID = obj.EntityID;
            objAct.ObjActPartName = obj.Name;
            objAct.ObjActParamID = 310311;     // hardcoded door lol
            objAct.ObjActStateType = MSB3.Event.ObjAct.ObjActState.DoorState;
            objAct.EventFlagID = -1;           // temp

            return new ObjActPair(obj, objAct);
        }

        /* Create player spawn point with entityID for load doors to warp to */
        public static MSB3.Part.Player MakeMarker(int area, int block, Cell cell, DoorMarker marker, Counters counters, Bounds? bounds = null) {
            MSB3.Part.Player player = new();
            player.ModelName = "c0000";
            player.Position = bounds != null ? Vector3.Add(Vector3.Add(marker.position, bounds.offset), bounds.center) : marker.position;
            player.Rotation = marker.rotation;
            player.EntityID = marker.entityID;
            player.Name = $"c0000_{counters.GetPlayer():D4}";
            return player;
        }

        public static MSB3.Part.Object MakeSky(Layout layout) {
            MSB3.Part.Object sky = new();
            sky.ModelName = "o004900"; // Dragon peak sky repack. Hard resource. Temporary till we create more skys
            sky.SibPath = "";
            sky.Position = (new Vector3(layout.center.x, 0, layout.center.y) * Const.CELL_SIZE) + new Vector3(0, -500, 0);
            sky.Rotation = new Vector3(0, 5, 0);
            sky.MapStudioLayer = uint.MaxValue;
            for (int k = 0; k < sky.DrawGroups.Length; k++) {
                sky.DrawGroups[k] = uint.MaxValue;
                sky.DispGroups[k] = 0;
                sky.BackreadGroups[k] = 0;
            }
            sky.Name = $"{sky.ModelName}_0000";
            sky.UnkE0E = -1;
            sky.LodParamID = -1;
            sky.DrawByReflectCam = true;
            sky.ShadowDest = false;
            sky.ShadowSource = false;
            sky.Gparam.LightSetID = 0;
            sky.Gparam.FogParamID = 350;
            sky.Gparam.EnvMapID = -1;
            sky.Gparam.LightScatteringID = -1;
            return sky;
        }

        public class Counters {
            Dictionary<ModelInfo, int> mapPieces;
            Dictionary<CollisionInfo, int> collisions;
            Dictionary<ObjectInfo, int> objects;
            Dictionary<ObjActInfo, int> objActs;
            int players;
            public Counters() {
                mapPieces = new();
                collisions = new();
                objects = new();
                objActs = new();
                players = 1;      // Start at 1 since 0 is always the default debug spawn point
            }

            public int GetPlayer() {
                return players++;
            }

            public int GetMapPiece(ModelInfo modelInfo) {
                if (mapPieces.ContainsKey(modelInfo)) { return mapPieces[modelInfo]++; } else { mapPieces.Add(modelInfo, 1); return 0; }
            }

            public int GetCollision(CollisionInfo collisionInfo) {
                if (collisions.ContainsKey(collisionInfo)) { return collisions[collisionInfo]++; } else { collisions.Add(collisionInfo, 1); return 0; }
            }

            public int GetObject(ObjectInfo objectInfo) {
                if (objects.ContainsKey(objectInfo)) { return objects[objectInfo]++; } else { objects.Add(objectInfo, 1); return 0; }
            }

            public int GetObjAct(ObjActInfo objActInfo) {
                if (objActs.ContainsKey(objActInfo)) { return objActs[objActInfo]++; } else { objActs.Add(objActInfo, 1); return 0; }
            }
        }

        public class MapColPair {
            public MSB3.Part.MapPiece mapPiece;
            public MSB3.Part.Collision collision;
            public MapColPair(MSB3.Part.MapPiece mapPiece, MSB3.Part.Collision collision) {
                this.mapPiece = mapPiece;
                this.collision = collision;
            }
        }

        public class ObjActPair {
            public MSB3.Part.Object obj;
            public MSB3.Event.ObjAct objAct;
            public ObjActPair(MSB3.Part.Object obj, MSB3.Event.ObjAct objAct) {
                this.obj = obj;
                this.objAct = objAct;
            }
        }
    }
}
