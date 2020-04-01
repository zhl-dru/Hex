using UnityEngine;
using System.Collections;
/// <summary>
/// 调整屏幕的亮度,饱和度和对比度
/// </summary>
public class BrightnessSaturationAndContrast : PostEffectsBase
{

    public Shader briSatConShader;
    private Material briSatConMaterial;
    public Material material
    {
        get
        {
            briSatConMaterial = CheckShaderAndCreateMaterial(briSatConShader, briSatConMaterial);
            return briSatConMaterial;
        }
    }
    /// <summary>
    /// 亮度
    /// </summary>
    [Range(0.0f, 3.0f)]
    public float brightness = 1.0f;
    /// <summary>
    /// 饱和度
    /// </summary>
    [Range(0.0f, 3.0f)]
    public float saturation = 1.0f;
    /// <summary>
    /// 对比度
    /// </summary>
    [Range(0.0f, 3.0f)]
    public float contrast = 1.0f;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (material != null)
        {
            material.SetFloat("_Brightness", brightness);
            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_Contrast", contrast);

            Graphics.Blit(src, dest, material);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
