using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public abstract class GSBaseSegmented : GSBase
{
    public GameObject boundingBoxObject;

    protected NativeArray<int> _selectedVertexIndices;
    protected NativeArray<float> _selectedVertexWeights;
    protected NativeArray<int> _originalTriangleIndices;

    private Vector3 _minPointLocal;
    private Vector3 _maxPointLocal;
    private Vector3 _minPointWorld;
    private Vector3 _maxPointWorld;

    public NativeArray<int> selectedVertexIndices => _selectedVertexIndices;
    public NativeArray<float> selectedVertexWeights => _selectedVertexWeights;
    public Vector3 minPointLocal => _minPointLocal;
    public Vector3 maxPointLocal => _maxPointLocal;
    public Vector3 minPointWorld => _minPointWorld;
    public Vector3 maxPointWorld => _maxPointWorld;

    #region Unity lifecycle

    protected override void OnDestroyCleanup()
    {
        base.OnDestroyCleanup();

        DisposeIfCreated(ref _originalTriangleIndices);
        DisposeIfCreated(ref _selectedVertexIndices);
        DisposeIfCreated(ref _selectedVertexWeights);
    }

    #endregion

    protected void ComputeVertexSelection()
    {
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        Transform meshTransform = transform;
        Bounds bounds = GetWorldBounds(boundingBoxObject);

        var vertexSet = new HashSet<int>();
        var originalTriangleIndicesList = new List<int>();

        for (int i = 0; i < _triangles.Length; i += 3)
        {
            int i0 = _triangles[i];
            int i1 = _triangles[i + 1];
            int i2 = _triangles[i + 2];

            Vector3 v0 = meshTransform.TransformPoint(_originalVertices[i0]);
            Vector3 v1 = meshTransform.TransformPoint(_originalVertices[i1]);
            Vector3 v2 = meshTransform.TransformPoint(_originalVertices[i2]);

            var v0y = _originalVertices[i0].y;
            var v1y = _originalVertices[i1].y;
            var v2y = _originalVertices[i2].y;

            if (IsPointInsideOBB(boundingBoxObject.transform, v0) ||
                IsPointInsideOBB(boundingBoxObject.transform, v1) ||
                IsPointInsideOBB(boundingBoxObject.transform, v2))
            {
                vertexSet.Add(i0); vertexSet.Add(i1); vertexSet.Add(i2);
                originalTriangleIndicesList.Add(i / 3);
                UpdateMinMaxMeshMargins(v0, v1, v2, _originalVertices[i0], _originalVertices[i1], _originalVertices[i2], ref _minPointWorld, ref _maxPointWorld, ref _minPointLocal, ref _maxPointLocal, ref minY, ref maxY);
            }
        }

        _selectedVertexIndices = new NativeArray<int>(vertexSet.Count, Allocator.Persistent);
        _selectedVertexWeights = new NativeArray<float>(vertexSet.Count, Allocator.Persistent);

        _originalTriangleIndices = new NativeArray<int>(originalTriangleIndicesList.ToArray(), Allocator.Persistent);

        SetVertexWeights(
            vertexSet,
            originalVertices,
            _minPointLocal,
            _maxPointLocal,
            ref _selectedVertexIndices,
            ref _selectedVertexWeights
        );
    }

    #region Helpers

    protected Bounds GetWorldBounds(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null)
            return col.bounds;

        Vector3 center = go.transform.position;
        Vector3 size = Vector3.Scale(go.transform.localScale, Vector3.one);
        return new Bounds(center, size);
    }

    protected bool IsPointInsideOBB(Transform boxTransform, Vector3 point)
    {
        MeshFilter meshFilter = boxTransform.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return false;

        Bounds localBounds = meshFilter.sharedMesh.bounds;

        Vector3 localPoint = boxTransform.InverseTransformPoint(point);

        Vector3 halfSize = localBounds.extents;
        return Mathf.Abs(localPoint.x) <= halfSize.x &&
               Mathf.Abs(localPoint.y) <= halfSize.y &&
               Mathf.Abs(localPoint.z) <= halfSize.z;
    }

    protected void UpdateMinMaxMeshMargins(
        Vector3 v0, Vector3 v1, Vector3 v2,
        Vector3 local0, Vector3 local1, Vector3 local2,
        ref Vector3 minPointWorld, ref Vector3 maxPointWorld,
        ref Vector3 minPointLocal, ref Vector3 maxPointLocal, ref float minY, ref float maxY)
    {

        if (v0.y < minY)
        {
            minPointWorld = v0;
            minPointLocal = local0;
            minY = v0.y;
        }
        if (v1.y < minY)
        {
            minPointWorld = v1;
            minPointLocal = local1;
            minY = v1.y;
        }
        if (v2.y < minY)
        {
            minPointWorld = v2;
            minPointLocal = local2;
            minY = v2.y;
        }

        if (v0.y > maxY)
        {
            maxPointWorld = v0;
            maxPointLocal = local0;
            maxY = v0.y;
        }
        if (v1.y > maxY)
        {
            maxPointWorld = v1;
            maxPointLocal = local1;
            maxY = v1.y;
        }
        if (v2.y > maxY)
        {
            maxPointWorld = v2;
            maxPointLocal = local2;
            maxY = v2.y;
        }
    }

    protected void SetVertexWeights(
        HashSet<int> vertexSet,
        NativeArray<float3> originalVertices,
        float3 minPointLocal,
        float3 maxPointLocal,
        ref NativeArray<int> selectedVertexIndices,
        ref NativeArray<float> selectedVertexWeights)
    {
        int idx = 0;
        int idx2 = 0;
        float denom = maxPointLocal.y - minPointLocal.y;
        if (math.abs(denom) < 1e-6f) denom = 1f;

        foreach (int i in vertexSet)
        {
            selectedVertexIndices[idx++] = i;
            float y = originalVertices[i].y;

            float t = math.saturate((y - minPointLocal.y) / denom);

            const float exponent = 0.5f;
            float weight = math.pow(t, exponent);

            selectedVertexWeights[idx2++] = weight;
        }
    }

    #endregion
}
