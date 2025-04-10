
Shader "Unlit/DecalShader"
{
	Properties{
		[HDR] _Color ("Tint", Color) = (0, 0, 0, 1)
		_MainTex ("Texture", 2D) = "white" {}
	}
 
	SubShader{
		Tags{ "RenderType"="Transparent" "Queue"="Transparent-400" "DisableBatching"="True"}//选择透明渲染，要在所有透明物体渲染完成之后再渲染
 
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite off //关闭深度写入，投影不需要
 
		Pass{
			CGPROGRAM
 
			#include "UnityCG.cginc"
 
			//定义顶点和片元着色器函数
			#pragma vertex vert
			#pragma fragment frag
 
			sampler2D _MainTex;
			float4 _MainTex_ST;
 
			fixed4 _Color;
 
			//相加深度图，要用C#代码开启相机深度图Camera.main.depthTextureMode = DepthTextureMode.Depth
			sampler2D_float _CameraDepthTexture;
 
			struct appdata{
				float4 vertex : POSITION;
			};
 
			struct v2f{
				float4 position : SV_POSITION;
				float4 screenPos : TEXCOORD0;
				float3 ray : TEXCOORD1;
			};
 
			v2f vert(appdata v){
				v2f o;
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.position = UnityWorldToClipPos(worldPos);
				o.ray = worldPos - _WorldSpaceCameraPos;//这个点的世界坐标减去相机的世界坐标，得到从相机到这个点的方向向量
				o.screenPos = ComputeScreenPos (o.position);//计算这个点A在屏幕空间的位置
				return o;
			}
 
			float3 getProjectedObjectPos(float2 screenPos, float3 worldRay){
				//根据屏幕目标采样相机深度纹理得到一个深度，注意这个深度是其他物体的，我们叫点B的深度，我们只是用了点A在屏幕坐标
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenPos);
				depth = Linear01Depth (depth) * _ProjectionParams.z;
				
				worldRay = normalize(worldRay);
				worldRay /= dot(worldRay, -UNITY_MATRIX_V[2].xyz);//这一步不可少，如果仅用worldRay * depth，计算出来的深度是相机到点的直线距离，而在Unity中，有near plane和far plane，plane上的任意一点到相机的深度都是一样的，这里用相机朝向和相机到这个点的方向得到这个点的实际单位长度
				//with that reconstruct world and object space positions
				float3 worldPos = _WorldSpaceCameraPos + worldRay * depth; //计算得到了B点的世界坐标
				float3 objectPos =  mul (unity_WorldToObject, float4(worldPos,1)).xyz; //将点B变换到A所在物体的坐标系中
				clip(0.5 - abs(objectPos));//Cube的世界坐标归一化后，有效值是从-0.5到0.5，其他的需要舍弃
				objectPos += 0.5;//采样纹理时的坐标范围为0~1,要加0.5
				return objectPos;
			}
 
			fixed4 frag(v2f i) : SV_TARGET{
				float2 screenUv = i.screenPos.xy / i.screenPos.w;
				float2 uv = getProjectedObjectPos(screenUv, i.ray).xz;//计算出来的uv不是0就是1，直接当作uv坐标来用
				fixed4 col = tex2D(_MainTex, uv);
				col *= _Color;
				return col;
			}
 
			ENDCG
		}
	}
}
