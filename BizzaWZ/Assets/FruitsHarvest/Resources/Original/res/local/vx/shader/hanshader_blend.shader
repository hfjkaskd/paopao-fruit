Shader "Han/HanShader_blend" {
	Properties {
		_Brightness ("Brightness", Float) = 1
		_Contrast ("Contrast", Float) = 1
		_MainColor ("Main Color", Vector) = (1,1,1,1)
		_MainTex ("Main Tex (A)", 2D) = "white" {}
		_MainPannerX ("Main Panner X", Float) = 0
		_MainPannerY ("Main Panner Y", Float) = 0
		_MaskTex ("Mask Tex", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		Lighting Off
		ZWrite Off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _MaskTex;
			float4 _MaskTex_ST;
			float _Brightness;
			float _Contrast;
			fixed4 _MainColor;
			float _MainPannerX;
			float _MainPannerY;

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 maskcoord : TEXCOORD1;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex) + float2(_MainPannerX, _MainPannerY) * _Time.y;
				o.maskcoord = TRANSFORM_TEX(v.texcoord, _MaskTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 tex = tex2D(_MainTex, i.texcoord);
				fixed4 mask = tex2D(_MaskTex, i.maskcoord);
				fixed4 col = tex * _MainColor * i.color;
				col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;
				col.rgb *= _Brightness;
				col.rgb = max(col.rgb, 0);
				col.a *= mask.a;
				return col;
			}
			ENDCG
		}
	}
}
