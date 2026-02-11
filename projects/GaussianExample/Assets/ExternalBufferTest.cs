// SPDX-License-Identifier: MIT

using UnityEngine;
using GaussianSplatting.Runtime;

/// <summary>
/// Test script that generates procedural Gaussian splats and renders them
/// through GaussianSplatRenderer's external buffer API.
/// Attach to a GameObject that also has a GaussianSplatRenderer component
/// (with shader/compute references assigned but no GaussianSplatAsset).
/// </summary>
[RequireComponent(typeof(GaussianSplatRenderer))]
public class ExternalBufferTest : MonoBehaviour
{
    [Range(1, 100000)]
    public int splatCount = 1000;
    [Range(0.001f, 0.1f)]
    public float splatSize = 0.01f;
    [Range(0.5f, 10f)]
    public float cloudRadius = 2f;

    GraphicsBuffer m_PosBuffer;
    GraphicsBuffer m_ColorBuffer;
    GraphicsBuffer m_Cov0Buffer;
    GraphicsBuffer m_Cov1Buffer;

    void OnEnable()
    {
        CreateBuffers();

        var renderer = GetComponent<GaussianSplatRenderer>();
        renderer.SetExternalBuffers(m_PosBuffer, m_ColorBuffer, m_Cov0Buffer, m_Cov1Buffer, splatCount);
    }

    void OnDisable()
    {
        DisposeBuffers();
    }

    void CreateBuffers()
    {
        DisposeBuffers();

        m_PosBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, splatCount, 12);
        m_ColorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, splatCount, 16);
        m_Cov0Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, splatCount, 12);
        m_Cov1Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, splatCount, 12);

        var positions = new Vector3[splatCount];
        var colors = new Vector4[splatCount];
        var cov0 = new Vector3[splatCount];
        var cov1 = new Vector3[splatCount];

        for (int i = 0; i < splatCount; i++)
        {
            // random position in a sphere
            Vector3 pos = Random.insideUnitSphere * cloudRadius;
            positions[i] = pos;

            // color based on position (normalized to 0..1 range)
            float r = pos.x / cloudRadius * 0.5f + 0.5f;
            float g = pos.y / cloudRadius * 0.5f + 0.5f;
            float b = pos.z / cloudRadius * 0.5f + 0.5f;
            colors[i] = new Vector4(r, g, b, 1.0f);

            // isotropic covariance: diagonal matrix with splatSize^2
            float variance = splatSize * splatSize;
            cov0[i] = new Vector3(variance, 0, 0); // xx, xy, xz
            cov1[i] = new Vector3(variance, 0, variance); // yy, yz, zz
        }

        m_PosBuffer.SetData(positions);
        m_ColorBuffer.SetData(colors);
        m_Cov0Buffer.SetData(cov0);
        m_Cov1Buffer.SetData(cov1);
    }

    void DisposeBuffers()
    {
        m_PosBuffer?.Dispose();
        m_PosBuffer = null;
        m_ColorBuffer?.Dispose();
        m_ColorBuffer = null;
        m_Cov0Buffer?.Dispose();
        m_Cov0Buffer = null;
        m_Cov1Buffer?.Dispose();
        m_Cov1Buffer = null;
    }
}
