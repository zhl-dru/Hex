using UnityEngine;
using System.Collections;
/// <summary>
/// 高斯模糊
/// </summary>
public class GaussianBlur : PostEffectsBase
{

    public Shader gaussianBlurShader;
    private Material gaussianBlurMaterial = null;

    public Material material
    {
        get
        {
            gaussianBlurMaterial = CheckShaderAndCreateMaterial(gaussianBlurShader, gaussianBlurMaterial);
            return gaussianBlurMaterial;
        }
    }

    /// <summary>
    /// 迭代次数
    /// </summary>
    [Range(0, 4)]
    public int iterations = 3;
    /// <summary>
    /// 模糊范围
    /// </summary>
    [Range(0.2f, 3.0f)]
    public float blurSpread = 0.6f;
    /// <summary>
    /// 缩放系数
    /// </summary>
    [Range(1, 8)]
    public int downSample = 2;

    /// 1st edition: just apply blur
    //	void OnRenderImage(RenderTexture src, RenderTexture dest) {
    //		if (material != null) {
    //			int rtW = src.width;
    //			int rtH = src.height;
    //			RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0);
    //
    //			// Render the vertical pass
    //			Graphics.Blit(src, buffer, material, 0);
    //			// Render the horizontal pass
    //			Graphics.Blit(buffer, dest, material, 1);
    //
    //			RenderTexture.ReleaseTemporary(buffer);
    //		} else {
    //			Graphics.Blit(src, dest);
    //		}
    //	} 

    /// 2nd edition: scale the render texture
    //	void OnRenderImage (RenderTexture src, RenderTexture dest) {
    //		if (material != null) {
    //			int rtW = src.width/downSample;
    //			int rtH = src.height/downSample;
    //			RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0);
    //			buffer.filterMode = FilterMode.Bilinear;
    //
    //			// Render the vertical pass
    //			Graphics.Blit(src, buffer, material, 0);
    //			// Render the horizontal pass
    //			Graphics.Blit(buffer, dest, material, 1);
    //
    //			RenderTexture.ReleaseTemporary(buffer);
    //		} else {
    //			Graphics.Blit(src, dest);
    //		}
    //	}

    /// 3rd edition: use iterations for larger blur
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (material != null)
        {
            //获取屏幕宽高并降采样
            int rtW = src.width / downSample;
            int rtH = src.height / downSample;
            //中间缓存,分配一块与屏幕大小相同的缓冲区0
            RenderTexture buffer0 = RenderTexture.GetTemporary(rtW, rtH, 0);
            //将临时渲染纹理的滤波模式设置为双线性
            buffer0.filterMode = FilterMode.Bilinear;
            //将图像缩放后存储在缓冲区0中
            Graphics.Blit(src, buffer0);
            //开始迭代
            for (int i = 0; i < iterations; i++)
            {
                material.SetFloat("_BlurSize", 1.0f + i * blurSpread);
                //中间缓存,分配一块与屏幕大小相同的缓冲区1
                RenderTexture buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);

                //渲染垂直通道
                Graphics.Blit(buffer0, buffer1, material, 0);
                //释放缓冲区0
                RenderTexture.ReleaseTemporary(buffer0);
                //将缓冲区1复制给0
                buffer0 = buffer1;
                //重新分配缓冲区1
                buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);

                //渲染水平通道
                Graphics.Blit(buffer0, buffer1, material, 1);
                //释放缓冲区0
                RenderTexture.ReleaseTemporary(buffer0);
                //将缓冲区1复制给0
                buffer0 = buffer1;
            }
            //将结果显示在屏幕上
            Graphics.Blit(buffer0, dest);
            //释放缓冲区
            RenderTexture.ReleaseTemporary(buffer0);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
