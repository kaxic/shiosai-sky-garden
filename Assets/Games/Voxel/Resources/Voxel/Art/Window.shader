Shader "Voxel/Blown glass" {
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"} Cull Back ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct a{float4 vertex:POSITION;float3 normal:NORMAL;float4 uv:TEXCOORD0;half4 color:COLOR;};
 struct p{float4 pos:SV_POSITION;float4 uv:TEXCOORD0;float incidence:TEXCOORD1;half4 col:COLOR;};
 p vert(a v){p o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.col=v.color;float3 ray=normalize(WorldSpaceViewDir(v.vertex));o.incidence=abs(dot(ray,UnityObjectToWorldNormal(v.normal)));return o;}
 half4 frag(p i):SV_Target{float2 uv=floor(i.uv.xy*32)/32;float diagonal=frac(uv.x*.79+uv.y*.94);
 float glint=smoothstep(.74,.79,diagonal)*(1-smoothstep(.79,.82,diagonal));
 float ice=i.uv.w;float pattern=ice*smoothstep(.78,.97,cos((uv.x-uv.y)*23+floor(uv.y*9)))*.37;
 half3 rgb=lerp(i.col.rgb,half3(.89,.98,1),glint*.77+pattern);return half4(rgb,.19+pow(1-i.incidence,2)*.27+glint*.20+ice*.14+pattern);}
 ENDCG}
 }}
