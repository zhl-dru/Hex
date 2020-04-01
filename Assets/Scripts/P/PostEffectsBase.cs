using UnityEngine;
using System.Collections;
/// <summary>
/// 屏幕后处理基类
/// </summary>
[ExecuteInEditMode]//在不透明的pass执行完后立即调用
[RequireComponent(typeof(Camera))]
public class PostEffectsBase : MonoBehaviour
{

    //Inspector面板上直接拖入
    public Shader shader = null;
    private Material _material = null;
    public Material _Material
    {
        get
        {
            if (_material == null)
                _material = GenerateMaterial(shader);
            return _material;
        }
    }
    
    /// <summary>
    /// 检查各种资源和条件是否满足,在start中调用
    /// </summary>
    protected void CheckResources()
    {
        bool isSupported = CheckSupport();

        if (isSupported == false)
        {
            NotSupported();
        }
    }

    /// <summary>
    /// 检查此平台上的支持
    /// </summary>
    /// <returns></returns>
    protected bool CheckSupport()
    {
        if (SystemInfo.supportsImageEffects == false || SystemInfo.supportsRenderTextures == false)
        {
            Debug.LogWarning("This platform does not support image effects or render textures.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 当平台不支持此效果时调用
    /// </summary>
    protected void NotSupported()
    {
        enabled = false;
    }

    protected void Start()
    {
        CheckResources();
    }

    /// <summary>
    /// 需要创建此效果所使用的材质时调用
    /// </summary>
    /// <param name="shader"></param>
    /// <param name="material"></param>
    /// <returns></returns>
    protected Material CheckShaderAndCreateMaterial(Shader shader, Material material)
    {
        if (shader == null)
        {
            return null;
        }

        if (shader.isSupported && material && material.shader == shader)
            return material;

        if (!shader.isSupported)
        {
            return null;
        }
        else
        {
            material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;
            if (material)
                return material;
            else
                return null;
        }
    }

    //根据shader创建用于屏幕特效的材质
    protected Material GenerateMaterial(Shader shader)
    {
        if (shader == null)
            return null;
        //需要判断shader是否支持
        if (shader.isSupported == false)
            return null;
        Material material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;
        if (material)
            return material;
        return null;
    }
}
