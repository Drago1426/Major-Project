using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public static class RuntimeObjModelLoader
{
    struct ObjVertexKey : IEquatable<ObjVertexKey>
    {
        public int vertexIndex;
        public int uvIndex;
        public int normalIndex;

        public bool Equals(ObjVertexKey other)
        {
            return vertexIndex == other.vertexIndex &&
                uvIndex == other.uvIndex &&
                normalIndex == other.normalIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjVertexKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = vertexIndex;
                hash = (hash * 397) ^ uvIndex;
                hash = (hash * 397) ^ normalIndex;
                return hash;
            }
        }
    }

    public static bool CanLoad(string path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
            string.Equals(Path.GetExtension(path), ".obj", StringComparison.OrdinalIgnoreCase);
    }

    public static GameObject Load(string path, Color tint)
    {
        if (!CanLoad(path) || !File.Exists(path))
        {
            Debug.LogWarning($"Runtime OBJ model path is invalid or missing: {path}");
            return null;
        }

        try
        {
            return LoadObj(path, tint);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load runtime OBJ model '{path}': {e.Message}");
            return null;
        }
    }

    static GameObject LoadObj(string path, Color tint)
    {
        var sourceVertices = new List<Vector3>();
        var sourceUvs = new List<Vector2>();
        var sourceNormals = new List<Vector3>();

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        var vertexMap = new Dictionary<ObjVertexKey, int>();

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("v ", StringComparison.Ordinal))
            {
                string[] parts = Split(line);
                if (parts.Length >= 4)
                    sourceVertices.Add(new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])));
                continue;
            }

            if (line.StartsWith("vt ", StringComparison.Ordinal))
            {
                string[] parts = Split(line);
                if (parts.Length >= 3)
                    sourceUvs.Add(new Vector2(Parse(parts[1]), Parse(parts[2])));
                continue;
            }

            if (line.StartsWith("vn ", StringComparison.Ordinal))
            {
                string[] parts = Split(line);
                if (parts.Length >= 4)
                    sourceNormals.Add(new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])));
                continue;
            }

            if (!line.StartsWith("f ", StringComparison.Ordinal))
                continue;

            string[] faceParts = Split(line);
            if (faceParts.Length < 4)
                continue;

            int[] faceIndices = new int[faceParts.Length - 1];
            for (int i = 1; i < faceParts.Length; i++)
                faceIndices[i - 1] = ResolveFaceVertex(faceParts[i], sourceVertices, sourceUvs, sourceNormals, vertices, uvs, normals, vertexMap);

            for (int i = 1; i < faceIndices.Length - 1; i++)
            {
                triangles.Add(faceIndices[0]);
                triangles.Add(faceIndices[i]);
                triangles.Add(faceIndices[i + 1]);
            }
        }

        if (vertices.Count == 0 || triangles.Count == 0)
        {
            Debug.LogWarning($"Runtime OBJ model has no usable mesh data: {path}");
            return null;
        }

        var mesh = new Mesh
        {
            name = Path.GetFileNameWithoutExtension(path)
        };

        if (vertices.Count > 65535)
            mesh.indexFormat = IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);

        if (uvs.Count == vertices.Count)
            mesh.SetUVs(0, uvs);

        if (normals.Count == vertices.Count && HasUsableNormals(normals))
            mesh.SetNormals(normals);
        else
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();

        var modelObject = new GameObject(Path.GetFileNameWithoutExtension(path));
        var meshFilter = modelObject.AddComponent<MeshFilter>();
        var meshRenderer = modelObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = CreateMaterial(tint);

        return modelObject;
    }

    static int ResolveFaceVertex(
        string token,
        List<Vector3> sourceVertices,
        List<Vector2> sourceUvs,
        List<Vector3> sourceNormals,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Vector3> normals,
        Dictionary<ObjVertexKey, int> vertexMap)
    {
        string[] indices = token.Split('/');
        var key = new ObjVertexKey
        {
            vertexIndex = ResolveObjIndex(indices, 0, sourceVertices.Count),
            uvIndex = ResolveObjIndex(indices, 1, sourceUvs.Count),
            normalIndex = ResolveObjIndex(indices, 2, sourceNormals.Count)
        };

        if (vertexMap.TryGetValue(key, out int existingIndex))
            return existingIndex;

        int newIndex = vertices.Count;
        vertexMap.Add(key, newIndex);

        vertices.Add(GetOrDefault(sourceVertices, key.vertexIndex, Vector3.zero));
        uvs.Add(GetOrDefault(sourceUvs, key.uvIndex, Vector2.zero));
        normals.Add(GetOrDefault(sourceNormals, key.normalIndex, Vector3.zero));
        return newIndex;
    }

    static int ResolveObjIndex(string[] indices, int slot, int sourceCount)
    {
        if (slot >= indices.Length || string.IsNullOrWhiteSpace(indices[slot]))
            return -1;

        if (!int.TryParse(indices[slot], NumberStyles.Integer, CultureInfo.InvariantCulture, out int objIndex))
            return -1;

        if (objIndex > 0)
            return Mathf.Clamp(objIndex - 1, 0, Mathf.Max(0, sourceCount - 1));

        if (objIndex < 0)
            return Mathf.Clamp(sourceCount + objIndex, 0, Mathf.Max(0, sourceCount - 1));

        return -1;
    }

    static T GetOrDefault<T>(List<T> list, int index, T fallback)
    {
        if (index < 0 || index >= list.Count)
            return fallback;

        return list[index];
    }

    static bool HasUsableNormals(List<Vector3> normals)
    {
        for (int i = 0; i < normals.Count; i++)
        {
            if (normals[i].sqrMagnitude > 0.0001f)
                return true;
        }

        return false;
    }

    static Material CreateMaterial(Color tint)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", tint);

        return material;
    }

    static string[] Split(string line)
    {
        return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    static float Parse(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            return parsed;

        return 0f;
    }
}
