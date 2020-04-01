//边缘检测
Shader "Unity Shaders Book/Edge Detection" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_EdgeOnly("Edge Only", Float) = 1.0//边缘线强度,脚本传递
		_EdgeColor("Edge Color", Color) = (0, 0, 0, 1)//描边颜色,脚本传递
		_BackgroundColor("Background Color", Color) = (1, 1, 1, 1)//背景颜色,脚本传递
	}
		SubShader{
			Pass {
				//深度测试必然通过,关闭剔除,关闭深度写入
				ZTest Always Cull Off ZWrite Off

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert  
				#pragma fragment fragSobel

				sampler2D _MainTex;
				uniform half4 _MainTex_TexelSize;//纹理纹素大小
				fixed _EdgeOnly;
				fixed4 _EdgeColor;
				fixed4 _BackgroundColor;

				struct v2f {
					float4 pos : SV_POSITION;
					half2 uv[9] : TEXCOORD0;
				};

				v2f vert(appdata_img v) {//appdata_img是unity内置结构体,包含图像处理时必需的顶点坐标和纹理坐标等变量
					v2f o;
					//将顶点坐标变换至裁剪空间
					o.pos = UnityObjectToClipPos(v.vertex);

					half2 uv = v.texcoord;
					//3x3的卷积核
					o.uv[0] = uv + _MainTex_TexelSize.xy * half2(-1, -1);
					o.uv[1] = uv + _MainTex_TexelSize.xy * half2(0, -1);
					o.uv[2] = uv + _MainTex_TexelSize.xy * half2(1, -1);
					o.uv[3] = uv + _MainTex_TexelSize.xy * half2(-1, 0);
					o.uv[4] = uv + _MainTex_TexelSize.xy * half2(0, 0);
					o.uv[5] = uv + _MainTex_TexelSize.xy * half2(1, 0);
					o.uv[6] = uv + _MainTex_TexelSize.xy * half2(-1, 1);
					o.uv[7] = uv + _MainTex_TexelSize.xy * half2(0, 1);
					o.uv[8] = uv + _MainTex_TexelSize.xy * half2(1, 1);

					return o;
				}
				//亮度
				fixed luminance(fixed4 color) {
					return  0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
				}
				//计算梯度值
				half Sobel(v2f i) {
					//水平卷积核
					const half Gx[9] = {-1,  0,  1,
										-2,  0,  2,
										-1,  0,  1};
					//竖直卷积核
					const half Gy[9] = {-1, -2, -1,
										0,  0,  0,
										1,  2,  1};

					half texColor;
					half edgeX = 0;
					half edgeY = 0;
					for (int it = 0; it < 9; it++) {
						texColor = luminance(tex2D(_MainTex, i.uv[it]));
						edgeX += texColor * Gx[it];
						edgeY += texColor * Gy[it];
					}

					half edge = 1 - abs(edgeX) - abs(edgeY);

					return edge;
				}

				fixed4 fragSobel(v2f i) : SV_Target {
					//计算当前像素的梯度值
					half edge = Sobel(i);
					//描边颜色与原图插值
					fixed4 withEdgeColor = lerp(_EdgeColor, tex2D(_MainTex, i.uv[4]), edge);
					//描边颜色与背景色插值
					fixed4 onlyEdgeColor = lerp(_EdgeColor, _BackgroundColor, edge);
					return lerp(withEdgeColor, onlyEdgeColor, _EdgeOnly);
				}

				ENDCG
			}
		}
			FallBack Off
}
