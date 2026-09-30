// SPDX-License-Identifier: MIT
using UnityEngine;

namespace GaussianSplatting.Runtime
{
    public class GaussianPseudomeshSplatAsset : GaussianSplatAsset
    {
        [SerializeField] string m_ObjPath;
        [SerializeField] string m_PointCloudPath;
        [SerializeField] bool m_UseMeshLeftHandedCS;

        public string objPath => m_ObjPath;
        public string pointCloudPath => m_PointCloudPath;
        public bool useMeshLeftHandedCS => m_UseMeshLeftHandedCS;

        public void Initialize(
            int splats,
            VectorFormat formatPos,
            VectorFormat formatScale,
            ColorFormat formatColor,
            SHFormat formatSh,
            Vector3 bMin,
            Vector3 bMax,
            CameraInfo[] cameraInfos,
            string pointCloudPath,
            bool useMeshLeftHandedCS
        ) {
            m_SplatCount = splats;
            m_FormatVersion = kCurrentVersion;
            m_PosFormat = formatPos;
            m_ScaleFormat = formatScale;
            m_ColorFormat = formatColor;
            m_SHFormat = formatSh;
            m_Cameras = cameraInfos;
            m_BoundsMin = bMin;
            m_BoundsMax = bMax;
            m_PointCloudPath = pointCloudPath;
            m_UseMeshLeftHandedCS = useMeshLeftHandedCS;
        }

        public void SetObjPath(string path)
        {
            m_ObjPath = path;
        }

        public void SetAssetFiles(
            TextAsset dataChunk,
            TextAsset dataPos,
            TextAsset dataOther,
            TextAsset dataColor,
            TextAsset dataSh
        ) {
            m_ChunkData = dataChunk;
            m_PosData   = dataPos;
            m_OtherData = dataOther;
            m_ColorData = dataColor;
            m_SHData    = dataSh;
        }
    }
}
