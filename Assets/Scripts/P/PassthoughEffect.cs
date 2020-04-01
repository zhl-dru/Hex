using UnityEngine;
using System.Collections;
/// <summary>
/// 屏幕收缩
/// </summary>
public class PassthoughEffect : PostEffectsBase
{
    public Shader pShader;
    private Material PassthoughEffectMaterial = null;

    public Material material
    {
        get
        {
            PassthoughEffectMaterial = CheckShaderAndCreateMaterial(pShader, PassthoughEffectMaterial);
            return PassthoughEffectMaterial;
        }
    }
    //收缩强度
    [Range(0, 0.15f)]
    public float distortFactor = 1.0f;
    //扭曲中心（0-1）屏幕空间，默认为中心点
    public Vector2 distortCenter = new Vector2(0.5f, 0.5f);
    //噪声图
    public Texture NoiseTexture = null;
    //屏幕扰动强度
    [Range(0, 2.0f)]
    public float distortStrength = 1.0f;
    //屏幕收缩总时间
    public float passThoughTime = 4.0f;
    //当前时间
    private float currentTime = 0.0f;
    //曲线控制权重
    public float curveFactor = 0.2f;
    //屏幕收缩效果曲线控制
    public AnimationCurve curve;
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material)
        {
            material.SetTexture("_NoiseTex", NoiseTexture);
            material.SetFloat("_DistortFactor", distortFactor);
            material.SetVector("_DistortCenter", distortCenter);
            material.SetFloat("_DistortStrength", distortStrength);
            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
    //ContexMenu，可以直接在Component上右键调用该函数
    [ContextMenu("Play")]
    public void StartPassThoughEffect()
    {
        currentTime = 0.0f;
        StartCoroutine(UpdatePassthoughEffect());
    }
    private IEnumerator UpdatePassthoughEffect()
    {
        while(currentTime<passThoughTime)
        {
            currentTime += Time.deltaTime;
            //根据时间占比在曲线（0，1）区间采样，再乘以权重作为收缩系数
            distortFactor = curve.Evaluate(currentTime / passThoughTime) * curveFactor;
            yield return null;
            //结束时强行设置为0
            distortFactor = 0.0f;
        }
    }
}
