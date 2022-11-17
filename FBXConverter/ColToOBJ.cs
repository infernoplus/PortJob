﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonFunc.Const;
using CommonFunc;
using FBXConverter.Solvers;

namespace FBXConverter {
    class ColToOBJ {
        /* Converts all the MeshContent children of an FBX NodeContent THAT IS SPECIFICALLY NAMED 'collision' into an OBJ */
        /* Used to convert the collision data of a nif into OBJ so it can then be converted into an hkx by an external program */
        public static void convert(string fbxPath, string objPath, NodeContent fbx) {
            /* Grab all collision mesh content from FBX */
            Dictionary<ObjG, MeshContent> FBX_Meshes = new();
            Vector3 rootPosition = fbx.Transform.Translation;

            void FBXHierarchySearch(NodeContent node, bool isCollisionChild) {
                foreach (NodeContent fbxComponent in node.Children) {
                    if (fbxComponent.Name.ToLower() == "collision") {
                        FBXHierarchySearch(fbxComponent, true);
                    }
                    if (fbxComponent is MeshContent meshContent && isCollisionChild) {
                        FBX_Meshes.Add(new ObjG(), meshContent);
                    }
                    if (fbxComponent.Children.Count > 0) {
                        FBXHierarchySearch(fbxComponent, isCollisionChild);
                    }
                }
            }
            FBXHierarchySearch(fbx, false);

            /* If we don't find any collision meshes then we don't do nothing */
            if(FBX_Meshes.Count < 1) { return; }

            /* Convert meshes into an obj */
            Obj obj = new();
            foreach (KeyValuePair<ObjG, MeshContent> kvp in FBX_Meshes) {
                ObjG g = kvp.Key;
                MeshContent meshContent = kvp.Value;

                g.name = meshContent.Name;
                g.mtl = "hkm_Cobblestone_Safe1";    // Not sure how we are going to define this yet. Just using this material type as a default for now

                foreach (GeometryContent geometryNode in meshContent.Geometry) {
                    /* Add indices first so we can use vertex array lenghts as offsets for indices */
                    for (int i = 0; i < geometryNode.Indices.Count; i+=3) {
                        IndexCollection indices = geometryNode.Indices;
                        ObjV[] v = new ObjV[3];
                        for(int j=0;j<3;j++) {
                            int vi = indices[i + j] + obj.vs.Count;
                            int vti = 0 + obj.vts.Count;
                            int vni = indices[i + j] + obj.vns.Count;
                            v[j] = new ObjV(vi, vti, vni);
                        }
                        g.fs.Add(new ObjF(v[2], v[1], v[0]));  // Reverse order because idk, either obj or fbx is backwards. don't know which
                    }

                    /* Add vertex data */
                    Vector3 textureCoordinate = Vector3.Zero; // We don't need texture coordinates in collision data, so we just write a single zero and point to that
                    obj.vts.Add(textureCoordinate.ToNumerics());

                    for (int i = 0; i < geometryNode.Vertices.Positions.Count; i++) {
                        Vector3 vertex = geometryNode.Vertices.Positions[i];
                        VertexChannel channel = geometryNode.Vertices.Channels[0];  // Assuming channel 0 will always be normals because... it's collision data... should be correct lol
                        Vector3 vertexNormal = (Vector3)channel[i];

                        Vector3 vertexTransformed = Vector3.Transform(
                            vertex
                            , (ABSOLUTE_VERT_POSITIONS ? meshContent.AbsoluteTransform : fbx.Transform) * Matrix.CreateScale(GLOBAL_SCALE)
                        );

                        obj.vs.Add(vertexTransformed.ToNumerics());
                        obj.vns.Add(vertexNormal.ToNumerics());
                    }
                }
                obj.gs.Add(g);
            }

            /* Write to file */
            obj.write(objPath);
        }
    }
}