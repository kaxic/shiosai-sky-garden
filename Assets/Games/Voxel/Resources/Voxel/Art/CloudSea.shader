Shader "Voxel/Cloud sea" {
 Properties{_High("Zenith",Color)=(.28,.59,.78,1) _Horizon("Horizon",Color)=(.97,.88,.67,1) _Deep("Lower air",Color)=(.53,.76,.83,1)}
 SubShader {Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _High,_Horizon,_Deep;
 struct p{float4 pos:SV_POSITION;float3 ray:TEXCOORD0;};
 p vert(float4 vertex:POSITION){p o;o.pos=UnityObjectToClipPos(vertex);o.ray=vertex.xyz;return o;}
 float hash(float2 n){return frac(sin(dot(n,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 k=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(k),hash(k+float2(1,0)),f.x),lerp(hash(k+float2(0,1)),hash(k+1),f.x),f.y);}
 half4 frag(p i):SV_Target{
 float3 d=normalize(i.ray);float h=d.y;
 half3 c=lerp(_Horizon.rgb,_High.rgb,smoothstep(-.065,.72,h));
 c=lerp(c,_Deep.rgb,smoothstep(0,.81,-h));
 float2 uv=d.xz/(.085+abs(h))*.86;
 float billows=noise(uv*1.31+float2(_Time.y*.009,0))*.69+noise(uv*3.13)*.31;
 float clouds=smoothstep(.43,.73,billows)*smoothstep(-.025,.23,-h);
 c=lerp(c,lerp(_Horizon.rgb,half3(.94,.97,.99),.56),clouds*.76);
 return half4(c,1);}
 ENDCG}
 }
}
