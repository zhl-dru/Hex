//边缘检测2
Shader "Unity Shaders Book/Edge Detection Normals And Depth" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_EdgeOnly("Edge Only", Float) = 1.0//仅显示边缘
		_EdgeColor("Edge Color", Color) = (0, 0, 0, 1)//描边颜色
		_BackgroundColor("Background Color", Color) = (1, 1, 1, 1)//背景颜色
		_SampleDistance("Sample Distance", Float) = 1.0//用于控制对深度+法线纹理采样时,使用的采样距离,值越大描边越宽
		_Sensitivity("Sensitivity", Vector) = (1, 1, 1, 1)//x=法线阈值,y=深度阈值
	}
		SubShader{
			CGINCLUDE

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			half4 _MainTex_TexelSize;//纹理纹素大小
			fixed _EdgeOnly;
			fixed4 _EdgeColor;
			fixed4 _BackgroundColor;
			float _SampleDistance;
			half4 _Sensitivity;

			sampler2D _CameraDepthNormalsTexture;//深度+法线纹理

			struct v2f {
				float4 pos : SV_POSITION;
				half2 uv[5]: TEXCOORD0;
			};

			v2f vert(appdata_img v) {//appdata_img是unity内置结构体,包含图像处理时必需的顶点坐标和纹理坐标等变量
				v2f o;
				//将顶点坐标变换至裁剪空间
				o.pos = UnityObjectToClipPos(v.vertex);

				half2 uv = v.texcoord;
				//存储屏幕图像颜色的采样纹理
				o.uv[0] = uv;
				//平台差异化处理
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					uv.y = 1 - uv.y;
				#endif
				//2x2卷积核
				o.uv[1] = uv + _MainTex_TexelSize.xy * half2(1,1) * _SampleDistance;//右上
				o.uv[2] = uv + _MainTex_TexelSize.xy * half2(-1,-1) * _SampleDistance;//左下
				o.uv[3] = uv + _MainTex_TexelSize.xy * half2(-1,1) * _SampleDistance;//左上
				o.uv[4] = uv + _MainTex_TexelSize.xy * half2(1,-1) * _SampleDistance;//右下

				return o;
			}
			//计算对角线上两个纹理值的差值
			half CheckSame(half4 center, half4 sample) {
				half2 centerNormal = center.xy;
				float centerDepth = DecodeFloatRG(center.zw);
				half2 sampleNormal = sample.xy;
				float sampleDepth = DecodeFloatRG(sample.zw);

				//比较法线差异
				//无需解码法线
				half2 diffNormal = abs(centerNormal - sampleNormal) * _Sensitivity.x;
				int isSameNormal = (diffNormal.x + diffNormal.y) < 0.1;
				//比较深度差异
				float diffDepth = abs(centerDepth - sampleDepth) * _Sensitivity.y;
				//按距离缩放所需阈值
				int isSameDepth = diffDepth < 0.1 * centerDepth;

				
				return isSameNormal * isSameDepth ? 1.0 : 0.0;
			}

			fixed4 fragRobertsCrossDepthAndNormal(v2f i) : SV_Target {
				//对深度+法线纹理采样
				half4 sample1 = tex2D(_CameraDepthNormalsTexture, i.uv[1]);
				half4 sample2 = tex2D(_CameraDepthNormalsTexture, i.uv[2]);
				half4 sample3 = tex2D(_CameraDepthNormalsTexture, i.uv[3]);
				half4 sample4 = tex2D(_CameraDepthNormalsTexture, i.uv[4]);

				half edge = 1.0;
				//计算对角线上两个纹理值的差值
				edge *= CheckSame(sample1, sample2);
				edge *= CheckSame(sample3, sample4);

				fixed4 withEdgeColor = lerp(_EdgeColor, tex2D(_MainTex, i.uv[0]), edge);
				fixed4 onlyEdgeColor = lerp(_EdgeColor, _BackgroundColor, edge);

				return lerp(withEdgeColor, onlyEdgeColor, _EdgeOnly);
			}

			ENDCG

			Pass {
				//深度测试必然通过,关闭剔除,关闭深度写入
				ZTest Always Cull Off ZWrite Off

				CGPROGRAM

				#pragma vertex vert  
				#pragma fragment fragRobertsCrossDepthAndNormal

				ENDCG
			}
		}
			FallBack Off
}
