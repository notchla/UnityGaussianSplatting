// SPDX-License-Identifier: MIT

using UnityEngine;
using GaussianSplatting.Runtime;

/// <summary>
/// Test script that generates procedural Gaussian splats every frame and renders them
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
    [Range(0.1f, 5f)]
    public float animSpeed = 1f;

    GraphicsBuffer m_PosBuffer;
    GraphicsBuffer m_ColorBuffer;
    GraphicsBuffer m_Cov0Buffer;
    GraphicsBuffer m_Cov1Buffer;

    // base positions (stable seed), animated positions written to GPU each frame
    Vector3[] m_BasePositions;
    Vector3[] m_AnimPositions;
    Vector4[] m_Colors;
    Vector3[] m_Cov0;
    Vector3[] m_Cov1;

    GaussianSplatRenderer m_Renderer;

    void OnEnable()
    {
        m_Renderer = GetComponent<GaussianSplatRenderer>();
        CreateBuffers();
        UpdateBufferData();
        m_Renderer.SetExternalBuffers(m_PosBuffer, m_ColorBuffer, m_Cov0Buffer, m_Cov1Buffer, splatCount);
    }

    void OnDisable()
    {
        DisposeBuffers();
    }

    void Update()
    {
        UpdateBufferData();
    }

    void CreateBuffers()
    {
        DisposeBuffers();

        m_PosBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, splatCount, 12);
        m_ColorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, splatCount, 16);
        m_Cov0Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, splatCount, 12);
        m_Cov1Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, splatCount, 12);

        m_BasePositions = new Vector3[splatCount];
        m_AnimPositions = new Vector3[splatCount];
        m_Colors = new Vector4[splatCount];
        m_Cov0 = new Vector3[splatCount];
        m_Cov1 = new Vector3[splatCount];

        // seed base positions using golden ratio sphere distribution + random radius
        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
        for (int i = 0; i < splatCount; i++)
        {
            float t = (float)i / Mathf.Max(splatCount - 1, 1);
            float inclination = Mathf.Acos(1f - 2f * t);
            float azimuth = goldenAngle * i;
            float r = cloudRadius * Mathf.Pow(Random.value, 1f / 3f);
            m_BasePositions[i] = new Vector3(
                r * Mathf.Sin(inclination) * Mathf.Cos(azimuth),
                r * Mathf.Sin(inclination) * Mathf.Sin(azimuth),
                r * Mathf.Cos(inclination)
            );
        }
    }

    void UpdateBufferData()
    {
        float time = Time.time * animSpeed;
        float variance = splatSize * splatSize;

        for (int i = 0; i < splatCount; i++)
        {
            Vector3 bp = m_BasePositions[i];

            // animate: orbit around Y axis, rate depends on distance from center + vertical bob
            float dist = bp.magnitude;
            float angle = time * (1f + 1f / (dist + 0.5f));
            float cosA = Mathf.Cos(angle);
            float sinA = Mathf.Sin(angle);

            Vector3 pos;
            pos.x = bp.x * cosA - bp.z * sinA;
            pos.y = bp.y + Mathf.Sin(time + dist * 3f) * 0.2f;
            pos.z = bp.x * sinA + bp.z * cosA;
            m_AnimPositions[i] = pos;

            // color based on animated position
            m_Colors[i] = new Vector4(
                pos.x / cloudRadius * 0.5f + 0.5f,
                pos.y / cloudRadius * 0.5f + 0.5f,
                pos.z / cloudRadius * 0.5f + 0.5f,
                1.0f
            );

            // isotropic covariance
            m_Cov0[i] = new Vector3(variance, 0, 0); // xx, xy, xz
            m_Cov1[i] = new Vector3(variance, 0, variance); // yy, yz, zz
        }

        m_PosBuffer.SetData(m_AnimPositions);
        m_ColorBuffer.SetData(m_Colors);
        m_Cov0Buffer.SetData(m_Cov0);
        m_Cov1Buffer.SetData(m_Cov1);
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
