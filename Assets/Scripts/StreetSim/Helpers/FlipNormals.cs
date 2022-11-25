using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FlipNormals : MonoBehaviour
{
    MeshFilter filter;
    private void Awake() {
        filter = GetComponent<MeshFilter>();
        if (filter != null) {
            InvertMesh();
            //FixNormals();
        }
    }

    private void InvertMesh() {
        Vector3[] normals = filter.mesh.normals;
        for(int i = 0; i < normals.Length; i++) {
            normals[i] = -normals[i];
        }
        filter.mesh.normals = normals;

        int[] triangles = filter.mesh.triangles;
        for (int i = 0; i < triangles.Length; i+=3)
        {
            int t = triangles[i];
            triangles[i] = triangles[i + 2];
            triangles[i + 2] = t;
        }           

        filter.mesh.triangles= triangles;

        FixNormals();
    }

    public void FixNormals()
    {
        if(filter.mesh.vertexCount != filter.mesh.normals.Length)
            filter.mesh.RecalculateNormals();

        Vector3[] normals = filter.mesh.normals;
        Vector3[] vertices = filter.mesh.vertices;
        int[] triangles = filter.mesh.triangles;

        Vector3 center = CenterPoint(vertices);

        for(int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];
            
            Vector3 n1 = normals[triangles[i]];
            Vector3 n2 = normals[triangles[i + 1]];
            Vector3 n3 = normals[triangles[i + 2]];

            Vector3 calcNormal = CalculateNormal(v1, v2, v3);
            
            if(!WithinTolerance(n1))
                n1 = calcNormal;
            if(!WithinTolerance(n2))
                n2 = calcNormal;
            if(!WithinTolerance(n3))
                n3 = calcNormal;
            
            Vector3 midpoint = center - ((v1 + v2 + v3) / 3);

            if(IsFacingInwards(calcNormal, midpoint))
                Array.Reverse(triangles, i, 3);
        }

        filter.mesh.normals = normals;
        filter.mesh.triangles = triangles;
    }

    private static Vector3 CenterPoint(Vector3[] vertices)
    {
        Vector3 center = Vector3.zero;

        for(int i = 1; i < vertices.Length; ++i)
            center += vertices[i];

        return center / vertices.Length;
    }

    private static bool WithinTolerance(Vector3 normal) => normal.magnitude > 0.001f;

    private static bool IsFacingInwards(Vector3 normal, Vector3 direction) =>
        Vector3.Dot(direction.normalized, normal.normalized) > 0f;
    
    private static Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v1 = b - a;
        Vector3 v2 = c - a;

        return new Vector3
        (
            (v1.y * v2.z) - (v1.z * v2.y),
            (v1.z * v2.x) - (v1.x * v2.z),
            (v1.x * v2.y) - (v1.y * v2.x)   
        ).normalized;
    }
}
