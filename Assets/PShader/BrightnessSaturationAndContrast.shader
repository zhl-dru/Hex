//调整屏幕的亮度,饱和度和对比度
Shader "Unity Shaders Book/Brightness Saturation And Contrast" {
	Properties{//可省略
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Brightness("Brightness", Float) = 1//亮度,脚本传递
		_Saturation("Saturation", Float) = 1//饱和度,脚本传递
		_Contrast("Contrast", Float) = 1//对比度,脚本传递
	}
		SubShader{
			Pass {
				//深度测试必然通过,关闭剔除,关闭深度写入
				ZTest Always Cull Off ZWrite Off

				CGPROGRAM
				#pragma vertex vert  
				#pragma fragment frag  

				#include "UnityCG.cginc"  

				sampler2D _MainTex;
				half _Brightness;
				half _Saturation;
				half _Contrast;

				struct v2f {
					float4 pos : SV_POSITION;
					half2 uv: TEXCOORD0;
				};

				v2f vert(appdata_img v) {//appdata_img是unity内置结构体,包含图像处理时必需的顶点坐标和纹理坐标等变量
					v2f o;
					//将顶点坐标变换至裁剪空间
					o.pos = UnityObjectToClipPos(v.vertex);

					o.uv = v.texcoord;

					return o;
				}

				fixed4 frag(v2f i) : SV_Target {
					//原屏幕图像的采样
					fixed4 renderTex = tex2D(_MainTex, i.uv);

					//改变亮度
					fixed3 finalColor = renderTex.rgb * _Brightness;

					//改变饱和度
					//计算亮度值,每个分量*系数
					fixed luminance = 0.2125 * renderTex.r + 0.7154 * renderTex.g + 0.0721 * renderTex.b;
					//根据亮度值构建颜色,饱和度为0
					fixed3 luminanceColor = fixed3(luminance, luminance, luminance);
					//使用饱和度在构建的颜色与改变亮度后的颜色之间插值
					finalColor = lerp(luminanceColor, finalColor, _Saturation);

					//改变对比度
					//先创建一个对比度为0的颜色(0.5,0.5,0.5)
					fixed3 avgColor = fixed3(0.5, 0.5, 0.5);
					//使用对比度在对比度为0的颜色与上一个颜色之间插值
					finalColor = lerp(avgColor, finalColor, _Contrast);

					return fixed4(finalColor, renderTex.a);
				}

				ENDCG
			}
		}

			Fallback Off
}
