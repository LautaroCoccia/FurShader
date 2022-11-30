using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading;

[RequireComponent(typeof(MeshFilter))]
public class ShellTextureGeo : MonoBehaviour
{
    [Serializable,StructLayout(LayoutKind.Sequential)]
    public struct InputVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }

    [Serializable,StructLayout(LayoutKind.Sequential)]
    public struct InputTriangle
    {
        public InputVertex[] inputVertices;
    }
    
    public ComputeShader shellTextureGeoCS;
    public Material renderingMaterial;
    [Min(1)]
    public int layers = 1;
    public float heightOffset = 0;

    private int kernelID;
    private int threadGroupSize;
    private int[] indirectArgs = new int[] { 0, 1, 0, 0 };

    private List<InputTriangle> inputTriangles;

    private ComputeBuffer inputTrianglesBuffer;
    private ComputeBuffer drawTrianglesBuffer;
    private ComputeBuffer indirectArgsBuffer;

    private const int INPUTTRIANGLES_STRIDE = (3 * (3 + 3 + 2)) * sizeof(float);
    private const int DRAWTRIANGLES_STRIDE = (3 * (3 + 3 + 2 + 4)) * sizeof(float);
    private const int INDIRECTARGS_STRIDE = 4 * sizeof(int);

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private int triangleCount;
    private bool initialized = false;
    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
        meshRenderer = GetComponent<MeshRenderer>();
        triangleCount = mesh.triangles.Length / 3; //matriz de indices por cada triangulo por eso se divide por 3

    }
    private void SetupBuffers()
    {
        inputTrianglesBuffer = new ComputeBuffer(triangleCount, INPUTTRIANGLES_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        drawTrianglesBuffer = new ComputeBuffer(triangleCount * layers, DRAWTRIANGLES_STRIDE, ComputeBufferType.Append);
        indirectArgsBuffer = new ComputeBuffer(1, INDIRECTARGS_STRIDE, ComputeBufferType.IndirectArguments);
    }
    private void ReleaseBuffers()
    {
        ReleaseBuffers(inputTrianglesBuffer);
        ReleaseBuffers(drawTrianglesBuffer);
        ReleaseBuffers(indirectArgsBuffer);
    }
    private void ReleaseBuffers(ComputeBuffer buffer)
    {
        if(buffer != null)
        {
            buffer.Release();
            buffer = null;
        }
    }
    private void SetupData()
    {
        if(mesh == null)
        {
            return;
        }
        inputTriangles = new List<InputTriangle>();
        for (int i = 0; i < triangleCount; i++)
        {
            InputTriangle inputTriangle = new InputTriangle();
            inputTriangle.inputVertices = new InputVertex[3];
            inputTriangles.Add(inputTriangle);
        }
        for (int i = 0; i < mesh.triangles.Length; i++)
        {
            int triangle = i / 3;
            int vertex = i % 3;
            inputTriangles[triangle].inputVertices[vertex].position = mesh.vertices[i];
            inputTriangles[triangle].inputVertices[vertex].normal = mesh.normals[i];
            inputTriangles[triangle].inputVertices[vertex].uv = mesh.uv[i];
        }
        inputTrianglesBuffer.SetData(inputTriangles);
        drawTrianglesBuffer.SetCounterValue(0);
        indirectArgsBuffer.SetData(indirectArgs);
    }
    private void GenerateGeometry()
    {
        if(mesh == null || shellTextureGeoCS == null || renderingMaterial == null)
        {
            return;
        }

        kernelID = shellTextureGeoCS.FindKernel("ShellTextureGeo");
        shellTextureGeoCS.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out _, out _);
        threadGroupSize = Mathf.CeilToInt((float)triangleCount / threadGroupSizeX);

        shellTextureGeoCS.SetBuffer(kernelID, "_InputTrianglesBuffer", inputTrianglesBuffer);
        shellTextureGeoCS.SetBuffer(kernelID, "_DrawTrianglesBuffer", drawTrianglesBuffer);
        shellTextureGeoCS.SetBuffer(kernelID, "_IndirectArgsBuffer", indirectArgsBuffer);

        shellTextureGeoCS.SetInt("_TriangleCount", triangleCount);

        shellTextureGeoCS.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        shellTextureGeoCS.SetInt("_Layers", layers);
        shellTextureGeoCS.SetFloat("_HeightOffset", heightOffset);

        renderingMaterial.SetBuffer("_DrawTrianglesBuffer", drawTrianglesBuffer);

        shellTextureGeoCS.Dispatch(kernelID, threadGroupSize, 1, 1);
        
        initialized = true;
    }
    private void Update()
    {
        if(initialized)
        {
            Graphics.DrawProceduralIndirect(
                renderingMaterial, 
                meshRenderer.bounds,
                MeshTopology.Triangles,
                indirectArgsBuffer,
                0,
                null,
                null,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                true,
                gameObject.layer);
        }
    }
}
