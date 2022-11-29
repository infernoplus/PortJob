using CommonFunc;
using Newtonsoft.Json;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortJob {
    /* Adjusting how we handle things a bit here! */
    /* Instead of converting models/textures/collision on demand we are now going to convert all of them at once a single time */
    /* The converted models will be put into a temp folder and a json file will be generated that contains information about each file */
    /* The main conversion program will then just reference that json and then copy the files over and bnd them as nessacary. */
    class MassConvert {
        public static Cache Convert(ESM esm, List<Layout> layouts, List<Layint> layints) {
            string inputRoot = Const.MorrowindPath + "Data Files\\meshes\\";
            string outputRoot = Const.OutputPath + "cache\\";
            string outputCache = outputRoot + "cache.json";
            string outputMesh = outputRoot + "meshes\\";
            string outputTerrain = outputRoot + "terrain\\";
            string outputTex = outputRoot + "textures\\";

            /* Cache Exists */
            if (File.Exists(outputCache)) {
                Log.Info(0, $"Using cache: {outputCache}", "test");
                Log.Info(1, $"Delete this file if you want to regenerate models/textures/collision and cache!", "test");
            }
            /* Generate new cache */
            else {
                List<FBXInfo> fbxList = new();
                void add(string inFile, string outFile) {
                    foreach(FBXInfo fbxi in fbxList) {
                        if(fbxi.FBXPath == inFile) { return; }
                    }
                    fbxList.Add(new FBXInfo(inFile, outFile, outputTex));
                }

                /* Exterior Cells */
                Log.Info(0, $"Mass Converter searching exterior cells...", "test");
                foreach(Layout layout in layouts) {
                    if (!Const.DEBUG_GEN_EXT_LAYOUT(layout.id)) { continue; } //for rapid debugging 

                    for (int j = 0; j < layout.cells.Count; j++) {
                        if (j > Const.DEBUG_MAX_EXT_CELLS) { break; }
                        Cell cell = layout.cells[j];
                        cell.Load(esm);

                        for (int k = 0; k < cell.content.Count; k++) {
                            Content content = cell.content[k];

                            if (content.mesh == null || !content.mesh.Contains("\\")) { continue; } // Skip invalid or top level placeholder meshes
                            if (!PortJob.CONVERT_ALL.Contains(content.type)) { continue; } // Skip stuff we aren't converting

                            string inPath = inputRoot + content.mesh.Substring(0, content.mesh.Length - 3) + "fbx";
                            string outPath = outputMesh + content.mesh.Substring(0, content.mesh.Length - 3) + "flver";

                            add(inPath, outPath);
                        }
                    }
                }

                /* Interior Cells */
                Log.Info(0, $"Mass Converter searching interior cells...", "test");
                foreach(Layint layint in layints) {
                    if (!Const.DEBUG_GEN_INT_LAYINT(layint.id)) { continue; } //for rapid debugging 

                    for (int j = 0; j < layint.cells.Count; j++) {
                        if (j > Const.DEBUG_MAX_INT_CELLS) { break; }
                        Cell cell = layint.cells[j];
                        cell.Load(esm);

                        for (int k = 0; k < cell.content.Count; k++) {
                            Content content = cell.content[k];

                            if (content.mesh == null || !content.mesh.Contains("\\")) { continue; } // Skip invalid or top level placeholder meshes
                            if (!PortJob.CONVERT_ALL.Contains(content.type)) { continue; } // Skip stuff we aren't converting

                            string inPath = inputRoot + content.mesh.Substring(0, content.mesh.Length - 3) + "fbx";
                            string outPath = outputMesh + content.mesh.Substring(0, content.mesh.Length - 3) + "flver";

                            add(inPath, outPath);
                        }
                    }
                }

                /* Call workers to convert FBX files */
                Log.Info(0, $"Mass Converter processing [{fbxList.Count}] files...", "test");
                _workers.Add(new FBXConverterWorker(outputCache, Const.MorrowindPath, Const.GLOBAL_SCALE, fbxList));
                WaitForWorkers();

                /* Load cache output from FBXConverter so we can add to it */
                Log.Info(0, $"Mass Converter reading cache...", "test");
                string tempRawJson = File.ReadAllText(outputCache);
                Cache tempCache = Newtonsoft.Json.JsonConvert.DeserializeObject<Cache>(tempRawJson);

                /* Generate terrain for exterior cells */
                Log.Info(0, $"Mass Converter generating exterior cell terrain...", "test");
                for (int i = 0; i < layouts.Count; i++) {
                    if (!Const.DEBUG_GEN_EXT_LAYOUT(i)) { continue; } //for rapid debugging 
                    Layout layout = layouts[i];

                    for (int j = 0; j < layout.cells.Count; j++) {
                        if (j > Const.DEBUG_MAX_EXT_CELLS) { break; }
                        Cell cell = layout.cells[j];
                        cell.Generate(esm);

                        TerrainInfo terrainInfo = TerrainConverter.convert(cell, outputTerrain + $"{cell.position.x},{cell.position.y}.flver", outputTex);
                        tempCache.terrains.Add(terrainInfo);
                    }
                }

                /* Convert HKX files */
                string[] meshDirs = Directory.GetDirectories(outputMesh);
                foreach(string dir in meshDirs) {
                    Log.Info(0, $"Mass Converter processing collision: {dir}", "test");
                    ColConverter.Run(dir);
                }
                Log.Info(0, $"Mass Converter processing collision: {outputTerrain}", "test");
                ColConverter.Run(outputTerrain);

                /* Exterior Cell Objects */
                Log.Info(0, $"Mass Converter processing objects in exterior cells...", "test");
                foreach(Layout layout in layouts) {
                    if (!Const.DEBUG_GEN_EXT_LAYOUT(layout.id)) { continue; } //for rapid debugging 

                    for (int j = 0; j < layout.cells.Count; j++) {
                        if (j > Const.DEBUG_MAX_EXT_CELLS) { break; }
                        Cell cell = layout.cells[j];
                        cell.Load(esm);

                        for (int k = 0; k < cell.content.Count; k++) {
                            Content content = cell.content[k];

                            if (content.mesh == null || !content.mesh.Contains("\\")) { continue; } // Skip invalid or top level placeholder meshes
                            if (!PortJob.CONVERT_TO_OBJ.Contains(content.type)) { continue; } // Skip stuff we aren't converting

                            ModelInfo modelInfo = tempCache.GetModelInfo(content.mesh);
                            if (content.door != null && content.door.type == DoorContent.DoorType.Decoration) {
                                if (tempCache.GetObjActInfo(content.id) != null) { continue; }
                                ObjActInfo objActInfo = new(content.id, modelInfo);
                                tempCache.objActs.Add(objActInfo);
                            } else {
                                if (tempCache.GetObjectInfo(content.id) != null) { continue; }
                                ObjectInfo objectInfo = new(content.id, modelInfo);
                                tempCache.objects.Add(objectInfo);
                            }
                        }
                    }
                }

                /* Interior Cell Objects */
                Log.Info(0, $"Mass Converter processing objects in interior cells...", "test");
                foreach (Layint layint in layints) {
                    if (!Const.DEBUG_GEN_INT_LAYINT(layint.id)) { continue; } //for rapid debugging 

                    for (int j = 0; j < layint.cells.Count; j++) {
                        if (j > Const.DEBUG_MAX_INT_CELLS) { break; }
                        Cell cell = layint.cells[j];
                        cell.Load(esm);

                        for (int k = 0; k < cell.content.Count; k++) {
                            Content content = cell.content[k];

                            if (content.mesh == null || !content.mesh.Contains("\\")) { continue; } // Skip invalid or top level placeholder meshes
                            if (!PortJob.CONVERT_TO_OBJ.Contains(content.type)) { continue; } // Skip stuff we aren't converting

                            ModelInfo modelInfo = tempCache.GetModelInfo(content.mesh);
                            if (content.door != null && content.door.type == DoorContent.DoorType.Decoration) {
                                if(tempCache.GetObjActInfo(content.id) != null) { continue; }
                                ObjActInfo objActInfo = new(content.id, modelInfo);
                                tempCache.objActs.Add(objActInfo);
                            } else {
                                if (tempCache.GetObjectInfo(content.id) != null) { continue; }
                                ObjectInfo objectInfo = new(content.id, modelInfo);
                                tempCache.objects.Add(objectInfo);
                            }
                        }
                    }
                }

                /* Assign resource ID numbers */
                Log.Info(0, $"Mass Converter assigning IDs...", "test");
                int nextMId = 0, nextCId = 0, nextOId = 5000;
                foreach (ModelInfo modelInfo in tempCache.models) {
                    modelInfo.id = nextMId++;
                    foreach(CollisionInfo collisionInfo in modelInfo.collisions) { collisionInfo.id = nextCId++; }
                }
                foreach (TerrainInfo terrainInfo in tempCache.terrains) {
                    terrainInfo.id = nextMId++;
                    terrainInfo.collision.id = nextCId++;
                }
                foreach(ObjectInfo objectInfo in tempCache.objects) {
                    objectInfo.id = nextOId++;
                }
                foreach (ObjActInfo objActInfo in tempCache.objActs) {
                    objActInfo.id = nextOId++;
                }

                /* Generate Object Files */
                Log.Info(0, $"Mass Converter writing [{tempCache.objects.Count}] obj files...");
                foreach (ObjectInfo objectInfo in tempCache.objects) {
                    /* Generate objBnd */
                    TPF tpf = TPF.Read(objectInfo.model.textures[0].path); // Merge all used tpfs into a single tpf
                    tpf.Compression = DCX.Type.None;
                    tpf.Encoding = 0x1;
                    for (int i = 1; i < objectInfo.model.textures.Count; i++) {
                        TextureInfo textureInfo = objectInfo.model.textures[i];
                        TPF mortpf = TPF.Read(textureInfo.path);
                        foreach (TPF.Texture tex in mortpf.Textures) {
                            tpf.Textures.Add(tex);
                        }
                    }
                    FLVER2 flver = FLVER2.Read(objectInfo.model.path);
                    byte[] hkxdcx = File.ReadAllBytes(objectInfo.model.GetCollision(1f).path);
                    byte[] hkx = DCX.Decompress(hkxdcx);

                    BND4 objBnd = new BND4();
                    string objPath = $"obj\\o{0:D2}\\o{0:D2}{objectInfo.id:D4}\\o{0:D2}{objectInfo.id:D4}";
                    objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 100, $"{objPath}.tpf", tpf.Write()));
                    objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 200, $"{objPath}.flver", flver.Write()));
                    objBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 300, $"{objPath}.hkx", hkx));
                    objBnd.Write($"{Const.OutputPath}obj\\o{0:D2}{objectInfo.id:D4}.objbnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
                }

                /* Generate ObjAct Files */
                Log.Info(0, $"Mass Converter writing [{tempCache.objActs.Count}] objact files...");
                foreach (ObjActInfo objActInfo in tempCache.objActs) {
                    DoorMake.Convert(objActInfo);
                }

                /* Write updated cache to file */
                Log.Info(0, $"Mass Converter writing cache: " + outputCache, "test");
                string cacheJson = JsonConvert.SerializeObject(tempCache);
                System.IO.File.WriteAllText(outputCache, cacheJson);
            }

            /* Load cache that fbxconverter generated */
            string rawJson = File.ReadAllText(outputCache);
            Cache cache = Newtonsoft.Json.JsonConvert.DeserializeObject<Cache>(rawJson);
            Log.Info(0, "", "test");
            return cache;
        }

        private static List<Worker> _workers = new();
        private static void WaitForWorkers() {
            while (_workers.Count > 0) {
                for (int i = _workers.Count - 1; i >= 0; i--) {
                    if (_workers[i].IsDone) {
                        if (_workers[i].ExitCode != 0) {
                            Console.WriteLine($"Worker exited with error code {_workers[i].ExitCode}");
                            Console.WriteLine(_workers[i].ErrorMessage);
                        }
                        _workers.RemoveAt(i);
                    }
                }
            }
        }
    }
}
