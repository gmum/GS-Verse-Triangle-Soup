using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using GaussianSplatting.Runtime;
using GaussianSplatting.Runtime.GaMeS;
using GaussianSplatting.Shared;
using GaussianSplatting.Runtime.Utils;

public class GSVerseSegmented : GSBaseSegmented
{
    #region Initialization

    protected override void ScheduleAssetRebuild()
    {
        _faceVertices = SplatMathUtils.GetMeshFaceSelectedVerticesNative(displacedVertices, _triangles, _originalTriangleIndices, Allocator.Persistent);
        _xyzValues = GaMeSUtils.CreateXYZDataSelected(_decodedAlphasNative, _faceVertices, _originalTriangleIndices, numberPtsPerTriangle);
        (_rotations, _scalings) = GaMeSUtils.CreateScaleRotationDataSelected(_faceVertices, _decodedScalesNative, _originalTriangleIndices, numberPtsPerTriangle);

        var job = new GaMeSUtils.CreateAssetDataJobSelected()
        {
            m_InputPos = _xyzValues,
            m_InputRot = _rotations,
            m_InputScale = _scalings,
            m_Output = _inputSplatsData,
            m_PrevOutput = _runTimeInputSplatsData,
            m_originalTriangleIndices = _originalTriangleIndices,
            m_numberPtsPerTriangle = numberPtsPerTriangle

        };

        createAssetJobHandle = job.Schedule(_originalTriangleIndices.Length * numberPtsPerTriangle, 8192);
        isCreateAssetJobActive = true;
    }

    protected override void InitializeFullMode()
    {
        ComputeVertexSelection();

        // Input allocations
        _inputSplatsData = new NativeArray<InputSplatData>(_originalTriangleIndices.Length * numberPtsPerTriangle, Allocator.Persistent);
        RegisterNativeCleanup(() => { if (_inputSplatsData.IsCreated) _inputSplatsData.Dispose(); });

        _faceVertices = SplatMathUtils.GetMeshFaceSelectedVerticesNative(displacedVertices, _triangles, _originalTriangleIndices, Allocator.Persistent);
        RegisterNativeCleanup(() => { if (_faceVertices.IsCreated) _faceVertices.Dispose(); });

        _xyzValues = GaMeSUtils.CreateXYZDataSelected(_decodedAlphasNative, _faceVertices, _originalTriangleIndices, numberPtsPerTriangle);
        RegisterNativeCleanup(() => { if (_xyzValues.IsCreated) _xyzValues.Dispose(); });

        (_rotations, _scalings) = GaMeSUtils.CreateScaleRotationDataSelected(_faceVertices, _decodedScalesNative, _originalTriangleIndices, numberPtsPerTriangle);
        RegisterNativeCleanup(() => { if (_rotations.IsCreated) _rotations.Dispose(); if (_scalings.IsCreated) _scalings.Dispose(); });

        var job = new GaMeSUtils.CreateAssetDataJobSelected()
        {
            m_InputPos = _xyzValues,
            m_InputRot = _rotations,
            m_InputScale = _scalings,
            m_Output = _inputSplatsData,
            m_PrevOutput = _runTimeInputSplatsData,
            m_originalTriangleIndices = _originalTriangleIndices,
            m_numberPtsPerTriangle = numberPtsPerTriangle
        };

        createAssetJobHandle = job.Schedule(_originalTriangleIndices.Length * numberPtsPerTriangle, 8192);
        createAssetJobHandle.Complete();

        CreateAsset();

    }

    #endregion
}
