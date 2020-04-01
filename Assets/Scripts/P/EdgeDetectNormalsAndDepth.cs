using UnityEngine;
using System.Collections;
/// <summary>
/// 边缘检测2
/// </summary>
public class EdgeDetectNormalsAndDepth : PostEffectsBase
{

    public Shader edgeDetectShader2;
    private Material edgeDetectMaterial = null;
    public Material material
    {
        get
        {
            edgeDetectMaterial = CheckShaderAndCreateMaterial(edgeDetectShader2, edgeDetectMaterial);
            return edgeDetectMaterial;
        }
    }
    /// <summary>
    /// 仅显示边缘
    /// </summary>
    [Range(0.0f, 1.0f)]
    public float edgesOnly = 0.0f;
    /// <summary>
    /// 描边颜色
    /// </summary>
    public Color edgeColor = Color.black;
    /// <summary>
    /// 背景颜色
    /// </summary>
    public Color backgroundColor = Color.white;
    /// <summary>
    /// 用于控制对深度+法线纹理采样时,使用的采样距离,值越大描边越宽
    /// </summary>
    public float sampleDistance = 1.0f;
    /// <summary>
    /// 深度阈值
    /// </summary>
    public float sensitivityDepth = 1.0f;
    /// <summary>
    /// 法线阈值
    /// </summary>
    public float sensitivityNormals = 1.0f;

    void OnEnable()
    {
        //设置摄像机的状态生成深度纹理+法线纹理
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;
    }

    [ImageEffectOpaque]//不透明物体渲染完成后立即调用
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (material != null)
        {
            material.SetFloat("_EdgeOnly", edgesOnly);
            material.SetColor("_EdgeColor", edgeColor);
            material.SetColor("_BackgroundColor", backgroundColor);
            material.SetFloat("_SampleDistance", sampleDistance);
            material.SetVector("_Sensitivity", new Vector4(sensitivityNormals, sensitivityDepth, 0.0f, 0.0f));

            Graphics.Blit(src, dest, material);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
