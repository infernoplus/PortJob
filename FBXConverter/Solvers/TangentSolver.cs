﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBXConverter.Solvers {
    /* Code by Meowmartius, borrowed from FBX2FLVER <3 */
    public class TangentSolver {
        public static Vector3 RotatePoint(Vector3 p, float pitch, float roll, float yaw) {

            Vector3 ans = new(0, 0, 0);


            double cosa = Math.Cos(yaw);
            double sina = Math.Sin(yaw);

            double cosb = Math.Cos(pitch);
            double sinb = Math.Sin(pitch);

            double cosc = Math.Cos(roll);
            double sinc = Math.Sin(roll);

            double Axx = cosa * cosb;
            double Axy = cosa * sinb * sinc - sina * cosc;
            double Axz = cosa * sinb * cosc + sina * sinc;

            double Ayx = sina * cosb;
            double Ayy = sina * sinb * sinc + cosa * cosc;
            double Ayz = sina * sinb * cosc - cosa * sinc;

            double Azx = -sinb;
            double Azy = cosb * sinc;
            double Azz = cosb * cosc;

            float px = p.X;
            float py = p.Y;
            float pz = p.Z;

            ans.X = (float)(Axx * px + Axy * py + Axz * pz);
            ans.Y = (float)(Ayx * px + Ayy * py + Ayz * pz);
            ans.Z = (float)(Azx * px + Azy * py + Azz * pz);


            return ans;
        }

        public static List<Vector4> SolveTangents(SoulsFormats.FLVER2.Mesh mesh,
            List<int> vertexIndices,
            List<Vector3> highQualityVertexNormals,
            List<Vector3> hqVertPositions,
            List<Vector2> hqVertUVs) {
            int triangleCount = vertexIndices.Count;

            int vertexCount = mesh.Vertices.Count;

            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            List<Vector4> tangentList = new();

            for (int a = 0; a < vertexIndices.Count; a += 3) {

                int i1 = vertexIndices[a];
                int i2 = vertexIndices[a + 1];
                int i3 = vertexIndices[a + 2];

                if (i1 != i2 || i2 != i3) {
                    Vector3 v1 = hqVertPositions[i1];
                    Vector3 v2 = hqVertPositions[i2];
                    Vector3 v3 = hqVertPositions[i3];

                    Vector2 w1 = new(hqVertUVs[i1].X, hqVertUVs[i1].Y);
                    Vector2 w2 = new(hqVertUVs[i2].X, hqVertUVs[i2].Y);
                    Vector2 w3 = new(hqVertUVs[i3].X, hqVertUVs[i3].Y);

                    float x1 = v2.X - v1.X;
                    float x2 = v3.X - v1.X;
                    float y1 = v2.Y - v1.Y;
                    float y2 = v3.Y - v1.Y;
                    float z1 = v2.Z - v1.Z;
                    float z2 = v3.Z - v1.Z;

                    float s1 = w2.X - w1.X;
                    float s2 = w3.X - w1.X;
                    float t1 = w2.Y - w1.Y;
                    float t2 = w3.Y - w1.Y;

                    float r = 1.0f / (s1 * t2 - s2 * t1);

                    Vector3 sdir = new((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                    Vector3 tdir = new((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                    tan1[i1] += sdir;
                    tan1[i2] += sdir;
                    tan1[i3] += sdir;

                    tan2[i1] += tdir;
                    tan2[i2] += tdir;
                    tan2[i3] += tdir;
                }
            }
            for (int i = 0; i < vertexCount; i++) {
                Vector3 n = highQualityVertexNormals[i];
                Vector3 t = tan1[i];

                float w = ((!(Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0f)) ? 1 : (-1));

                Vector3 outTanVec3 = Vector3.Normalize(t - n * Vector3.Dot(n, t));

                mesh.Vertices[i].Tangents[0] = (new System.Numerics.Vector4(outTanVec3.X, outTanVec3.Y, outTanVec3.Z, w));

                if (mesh.Vertices[i].Tangents.Count == 2) {
                    Vector3 ghettoTan = RotatePoint(new Vector3(mesh.Vertices[i].Normal.X, mesh.Vertices[i].Normal.Y, mesh.Vertices[i].Normal.Z), 0, MathHelper.PiOver2, 0);
                    mesh.Vertices[i].Tangents[1] = new System.Numerics.Vector4(ghettoTan.X, ghettoTan.Y, ghettoTan.Z, 0);
                }


                tangentList.Add(new Vector4(Vector3.Normalize(t - n * Vector3.Dot(n, t)), w));
            }

            return tangentList;
        }


    }
}
