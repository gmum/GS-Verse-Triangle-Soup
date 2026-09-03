using System;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using GaussianSplatting.Runtime;
using GaussianSplatting.Runtime.GaMeS;
using GaussianSplatting.Runtime.Utils;
using GaussianSplatting.Shared;

public class GSVersePseudomesh : GSBase
{
    private GaussianPseudomeshSplatAsset _pmAsset;
    private NativeArray<float3> _v2Temp;
    private NativeArray<float3> _v3Temp;

    private void Awake()
    {
        InitializePseudomeshSafely();
    }

    #region Initialization

    private void InitializePseudomeshSafely()
    {
        _splatRenderer = gameObject.GetComponent<GaussianSplatRenderer>();
        if (_splatRenderer == null)
            throw new InvalidOperationException("GaussianSplatRenderer component not found.");

        if (!(_splatRenderer.asset is GaussianPseudomeshSplatAsset pmAsset))
            throw new InvalidOperationException(
                "GaussianSplatRenderer.asset is not a GaussianPseudomeshSplatAsset. " +
                "Make sure the asset was created with the GaMeS-Pseudomesh (gs_flat) pipeline.");

        _pmAsset = pmAsset;
        numberPtsPerTriangle = 1;

        LoadPseudomesh();

        CreateRuntimeBuffers();

        // 7) Create runtime input splats data creator (validate pointCloudPath)
        _creator = new GaussianSplatRuntimeAssetCreator();
        if (string.IsNullOrEmpty(_pmAsset.pointCloudPath))
            throw new InvalidOperationException("pointCloudPath on GaussianPseudomeshSplatAsset is null or empty.");

        _runTimeInputSplatsData = _creator.CreateAsset(_pmAsset.pointCloudPath);
        RegisterNativeCleanup(() => { if (_runTimeInputSplatsData.IsCreated) _runTimeInputSplatsData.Dispose(); });

        InitializePseudomeshMode();

        InvokeOnInitVertices(_mesh.vertices);
    }

    #endregion

    private void LoadPseudomesh()
    {
        LoadMeshFromResourcesAndAttach(_pmAsset.objPath, _pmAsset.useMeshLeftHandedCS);
    }

    private void InitializePseudomeshMode()
    {
        _inputSplatsData = new NativeArray<InputSplatData>(_splatRenderer.asset.splatCount, Allocator.Persistent);
        RegisterNativeCleanup(() => { if (_inputSplatsData.IsCreated) _inputSplatsData.Dispose(); });

        _faceVertices = SplatMathUtils.GetMeshFaceVerticesNative(gameObject, _displacedVertices, _triangles, Allocator.Persistent);
        RegisterNativeCleanup(() => { if (_faceVertices.IsCreated) _faceVertices.Dispose(); });

        var (v1, v2, v3) = GaMeSUtils.SplitPackedTriangleSoup(_faceVertices);
        _xyzValues = v1;
        RegisterNativeCleanup(() => { if (_xyzValues.IsCreated) _xyzValues.Dispose(); });

        (_rotations, _scalings) = GaMeSUtils.CreateScaleRotationDataFromTriangleSoup(v1, v2, v3);
        v2.Dispose();
        v3.Dispose();
        RegisterNativeCleanup(() => { if (_rotations.IsCreated) _rotations.Dispose(); if (_scalings.IsCreated) _scalings.Dispose(); });

        var job = new GaMeSUtils.CreateAssetDataJob
        {
            m_InputPos = _xyzValues,
            m_InputRot = _rotations,
            m_InputScale = _scalings,
            m_PrevOutput = _runTimeInputSplatsData,
            m_Output = _inputSplatsData,
        };
        createAssetJobHandle = job.Schedule(_xyzValues.Length, 8192);
        createAssetJobHandle.Complete();

        CreateAsset();
    }

    protected override void ScheduleAssetRebuild()
    {
        _faceVertices = SplatMathUtils.GetMeshFaceVerticesNative(
            gameObject, _displacedVertices, _triangles, Allocator.Persistent);

        JobHandle h1;
        NativeArray<float3> v1;
        (v1, _v2Temp, _v3Temp, h1) = GaMeSUtils.ScheduleSplitPackedTriangleSoup(_faceVertices);
        _xyzValues = v1;

        JobHandle h2;
        (_rotations, _scalings, h2) = GaMeSUtils.ScheduleCreateScaleRotationDataFromTriangleSoup(_xyzValues, _v2Temp, _v3Temp, h1);

        var mergeJob = new GaMeSUtils.CreateAssetDataJob
        {
            m_InputPos = _xyzValues,
            m_InputRot = _rotations,
            m_InputScale = _scalings,
            m_Output = _inputSplatsData,
            m_PrevOutput = _runTimeInputSplatsData,
        };

        createAssetJobHandle = mergeJob.Schedule(_xyzValues.Length, 8192, h2);
        isCreateAssetJobActive = true;
    }

    protected override unsafe void CreateAsset()
    {
        DisposeIfCreated(ref _v2Temp);
        DisposeIfCreated(ref _v3Temp);

        if (_creator != null && _pmAsset != null)
        {
            var newAsset = _creator.CreateAsset(
                "new pseudomesh asset",
                _inputSplatsData,
                null,
                null,
                _pmAsset.pointCloudPath
            );
            _splatRenderer.InjectAsset(newAsset);
        }
    }

    protected override void OnDestroyCleanup()
    {
        DisposeIfCreated(ref _v2Temp);
        DisposeIfCreated(ref _v3Temp);
        base.OnDestroyCleanup();
    }
}
