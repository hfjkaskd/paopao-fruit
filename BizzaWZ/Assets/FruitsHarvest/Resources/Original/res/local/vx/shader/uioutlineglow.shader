Shader "UI/Custom/UiOutlineGlow" {
	Properties {
		_MainTex ("透明PNG主图", 2D) = "white" {}
		_EdgeThreshold ("边缘识别阈值", Range(0.0001, 0.2)) = 0.04
		_SourceOpacity ("原图透明度", Range(0, 1)) = 1
		_GlowRange ("外发光扩散宽度", Range(0, 10)) = 3
		[HDR] _GlowColor ("HDR外发光颜色", Vector) = (1,0.6,0.2,1)
		_InnerGlowToggle ("开启内发光(0关/1开)", Range(0, 1)) = 0
		_InnerGlowRange ("内发光扩散宽度", Range(0, 10)) = 2
		[HDR] _InnerGlowColor ("HDR内发光颜色", Vector) = (0.2,0.8,1,1)
		_GlowCycleTime ("亮度变化时长(秒)", Range(0.1, 10)) = 2
		_GlowAlphaMin ("发光透明度最小值", Range(0, 1)) = 0
		_GlowAlphaMax ("发光透明度最大值", Range(0, 1)) = 1
		_GlowCycleInterval ("循环停顿间隔(秒)", Range(0, 5)) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendMode ("发光混合模式", Float) = 0
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			float _EdgeThreshold;
			float _SourceOpacity;
			float _GlowRange;
			fixed4 _GlowColor;
			float _InnerGlowToggle;
			float _InnerGlowRange;
			fixed4 _InnerGlowColor;
			float _GlowCycleTime;
			float _GlowAlphaMin;
			float _GlowAlphaMax;
			float _GlowCycleInterval;

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			// 周边最大alpha采样，用于近似距离场式发光
			float SampleMaxAlpha(float2 uv, float radius)
			{
				float2 t = _MainTex_TexelSize.xy * radius;
				float m = 0;
				m = max(m, tex2D(_MainTex, uv + float2( t.x, 0)).a);
				m = max(m, tex2D(_MainTex, uv + float2(-t.x, 0)).a);
				m = max(m, tex2D(_MainTex, uv + float2(0,  t.y)).a);
				m = max(m, tex2D(_MainTex, uv + float2(0, -t.y)).a);
				m = max(m, tex2D(_MainTex, uv + float2( t.x,  t.y) * 0.707).a);
				m = max(m, tex2D(_MainTex, uv + float2(-t.x,  t.y) * 0.707).a);
				m = max(m, tex2D(_MainTex, uv + float2( t.x, -t.y) * 0.707).a);
				m = max(m, tex2D(_MainTex, uv + float2(-t.x, -t.y) * 0.707).a);
				return m;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 src = tex2D(_MainTex, i.texcoord);

				// 呼吸亮度
				float cycle = max(_GlowCycleTime, 0.01);
				float total = cycle + _GlowCycleInterval;
				float t = fmod(_Time.y, total);
				float phase = saturate(t / cycle);
				float pulse = lerp(_GlowAlphaMin, _GlowAlphaMax, 0.5 - 0.5 * cos(phase * 6.2831853));

				// 外发光: 自身透明但邻域不透明
				float outerNear = SampleMaxAlpha(i.texcoord, _GlowRange);
				float outerNear2 = SampleMaxAlpha(i.texcoord, _GlowRange * 0.5);
				float outerGlow = saturate(max(outerNear, outerNear2) - src.a) * pulse;

				// 内发光: 自身不透明但邻域存在透明(靠近边缘)
				float innerNear = SampleMaxAlpha(i.texcoord, _InnerGlowRange);
				float innerEdge = saturate(src.a - SampleMaxAlpha(i.texcoord, -_InnerGlowRange));
				float innerGlow = _InnerGlowToggle * saturate(1 - innerNear + src.a) * src.a * pulse;

				fixed4 col;
				col.rgb = src.rgb * _SourceOpacity * src.a
					+ _GlowColor.rgb * outerGlow * _GlowColor.a
					+ _InnerGlowColor.rgb * innerGlow * _InnerGlowColor.a * innerEdge;
				col.a = max(src.a * _SourceOpacity, outerGlow * _GlowColor.a);
				col *= i.color;
				return col;
			}
			ENDCG
		}
	}
}
