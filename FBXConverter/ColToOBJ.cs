using Microsoft.Xna.Framework;
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
        public static Obj convert(NodeContent fbx) {
            /* Grab all collision mesh content from FBX */
            Dictionary<ObjG, MeshContent> FBX_Meshes = new();
            Vector3 rootPosition = fbx.Transform.Translation;

            bool collisionNodeExists = false;
            bool nco = false, nc = false;
            void FBXHierarchySearch(NodeContent node, bool isCollisionChild) {
                foreach (NodeContent fbxComponent in node.Children) {
                    if (fbxComponent.Name.ToLower() == "nc") { nc = true; }
                    if (fbxComponent.Name.ToLower() == "nco") { nco = true; }
                        if (fbxComponent.Name.ToLower() == "collision") {
                        collisionNodeExists = true;
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

            /* If the fbx has an NCO or NC dummy node then we don't generate collision */
            if(nc || nco) { return null; }

            /* If we don't find a collision node then we will instead use the visual mesh for collsion. Thanks todd... */
            if(!collisionNodeExists) { FBXHierarchySearch(fbx, true); }

            /* Discard if empty */
            if(FBX_Meshes.Count < 1) { return null; }

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
                        g.fs.Add(new ObjF(v[0], v[1], v[2]));
                    }

                    /* Add vertex data */
                    Vector3 textureCoordinate = Vector3.Zero; // We don't need texture coordinates in collision data, so we just write a single zero and point to that
                    obj.vts.Add(textureCoordinate.ToNumerics());

                    for (int i = 0; i < geometryNode.Vertices.Positions.Count; i++) {
                        // Get position and transform it
                        Vector3 vertex = geometryNode.Vertices.Positions[i];
                        Vector3 vertexTransformed = Vector3.Transform(
                            vertex
                            , (ABSOLUTE_VERT_POSITIONS ? meshContent.AbsoluteTransform : fbx.Transform) * Matrix.CreateScale(GLOBAL_SCALE)
                        );
                        vertexTransformed.X = -vertexTransformed.X; // X is flipped. Don't know why but it is correct and we do it in all other model conversions.

                        /* Rotate Y 180 degrees because... */
                        float cosDegrees = (float)Math.Cos(Math.PI);
                        float sinDegrees = (float)Math.Sin(Math.PI);

                        float x = (vertexTransformed.X * cosDegrees) + (vertexTransformed.Z * sinDegrees);
                        float z = (vertexTransformed.X * -sinDegrees) + (vertexTransformed.Z * cosDegrees);

                        vertexTransformed.X = x;
                        vertexTransformed.Z = z;  // @TODO: note that we didn't rotate the normals so this probably very bad and wrong rn and should be fixed at some point


                        // Get normal and rotate it (x is flipped so normals have to be rotated to match)
                        VertexChannel channel = geometryNode.Vertices.Channels[0];  // Assuming channel 0 will always be normals because... it's collision data... should be correct lol
                        Vector3 vertexNormal = (Vector3)channel[i];
                        Matrix normalRotMatrix = Matrix.CreateRotationX(-MathHelper.PiOver2);
                        Vector3 normalInputVector = new(-vertexNormal.X, vertexNormal.Y, vertexNormal.Z);

                        Vector3 rotatedNormal = Vector3.Normalize(
                            Vector3.TransformNormal(normalInputVector, normalRotMatrix)
                        );

                        obj.vs.Add(vertexTransformed.ToNumerics());
                        obj.vns.Add(rotatedNormal.ToNumerics());
                    }
                }
                obj.gs.Add(g);
            }
            return obj;
        }
    }
}
