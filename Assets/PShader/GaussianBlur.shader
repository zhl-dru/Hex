//高斯模糊
/*
	二维5x5
	0.0030, 0.0133, 0.0219, 0.0133, 0.0030
	0.0133, 0.0596, 0.0983, 0.0596, 0.0133
	0.0219, 0.0983, 0.1621, 0.0983, 0.0219
	0.0133, 0.0596, 0.0983, 0.0596, 0.0133
	0.0030, 0.0133, 0.0219, 0.0133, 0.0030
	一维=行列和
	0.0545, 0.2442, 0.4026, 0.2442, 0.0545
	0.0545, 0.2442, 0.4026, 0.2442, 0.0545
*/
Shader "Unity Shaders Book/Gaussian Blur" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_BlurSize("Blur Size", Float) = 1.0//模糊范围,脚本传递
	}
		SubShader{
			CGINCLUDE

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			half4 _MainTex_TexelSize;//纹理纹素大小
			float _BlurSize;

			struct v2f {
				float4 pos : SV_POSITION;
				half2 uv[5]: TEXCOORD0;
			};
			//竖直
			v2f vertBlurVertical(appdata_img v) {//appdata_img是unity内置结构体,包含图像处理时必需的顶点坐标和纹理坐标等变量
				v2f o;
				//将顶点坐标变换至裁剪空间
				o.pos = UnityObjectToClipPos(v.vertex);

				half2 uv = v.texcoord;
				//一维高斯卷积核
				o.uv[0] = uv;
				o.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
				o.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
				o.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
				o.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;

				return o;
			}
			//水平
			v2f vertBlurHorizontal(appdata_img v) {//appdata_img是unity内置结构体,包含图像处理时必需的顶点坐标和纹理坐标等变量
				v2f o;
				//将顶点坐标变换至裁剪空间
				o.pos = UnityObjectToClipPos(v.vertex);

				half2 uv = v.texcoord;
				//一维高斯卷积核
				o.uv[0] = uv;
				o.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
				o.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
				o.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
				o.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;

				return o;
			}

			fixed4 fragBlur(v2f i) : SV_Target {
				//一个5x5的高斯卷积核可以拆分成两个大小为5的一维高斯核,由于它的对称性,只需记录三个高斯权重
				float weight[3] = {0.4026, 0.2442, 0.0545};

				fixed3 sum = tex2D(_MainTex, i.uv[0]).rgb * weight[0];

				for (int it = 1; it < 3; it++) {
					sum += tex2D(_MainTex, i.uv[it * 2 - 1]).rgb * weight[it];
					sum += tex2D(_MainTex, i.uv[it * 2]).rgb * weight[it];
				}

				return fixed4(sum, 1.0);
			}

			ENDCG
			//深度测试必然通过,关闭剔除,关闭深度写入
			ZTest Always Cull Off ZWrite Off

			Pass {
				NAME "GAUSSIAN_BLUR_VERTICAL"

				CGPROGRAM

				#pragma vertex vertBlurVertical  
				#pragma fragment fragBlur

				ENDCG
			}

			Pass {
				NAME "GAUSSIAN_BLUR_HORIZONTAL"

				CGPROGRAM

				#pragma vertex vertBlurHorizontal  
				#pragma fragment fragBlur

				ENDCG
			}
		}
			FallBack "Diffuse"
}