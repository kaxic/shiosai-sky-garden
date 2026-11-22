Shader "Voxel/Water ribbons" {
 Properties{_Ink("Water",Color)=(.43,.79,.82,1)}
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Ink;struct p{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 p vert(float4 vertex:POSITION,float4 uv:TEXCOORD0){p o;o.pos=UnityObjectToClipPos(vertex);o.uv=uv.xy;return o;}
 half4 frag(p i):SV_Target{float x=floor(i.uv.x*16),flow=frac(i.uv.y+_Time.y*(.31+frac(x*.618)*.14)+frac(x*.27));float glint=smoothstep(.83,.99,flow)*(.4+.6*frac(x*.271));
 return half4(lerp(_Ink.rgb,half3(.86,.98,.99),glint*.79),(.40+.39*glint)*smoothstep(0,.18,i.uv.y));}
 ENDCG}
 }}
