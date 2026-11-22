Shader "Voxel/Pixel cloud banks" {
 Properties{_ShadowCloud("Underside",Color)=(.69,.80,.84,1) _Crown("Sunlit cloud",Color)=(.97,.95,.83,1)}
 SubShader{Tags{"Queue"="Transparent-30" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _ShadowCloud,_Crown;
 struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 shape:TEXCOORD1;};
 struct p{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float seed:TEXCOORD1;};
 p vert(a v){p o;float3 c=UnityObjectToViewPos(v.vertex);c.xy+=(v.uv-.5)*v.shape.xy;o.pos=mul(UNITY_MATRIX_P,float4(c,1));o.uv=v.uv;o.seed=v.shape.z;return o;}
 float lobe(float2 uv,float2 center,float2 radii){return 1-length((uv-center)/radii);}
 half4 frag(p i):SV_Target{
 float2 uv=floor(i.uv*float2(46,21))/float2(46,21);
 float a=frac(i.seed*.624),b=frac(i.seed*.392);
 float d=lobe(uv,float2(.50,.39),float2(.40,.19));
 d=max(d,lobe(uv,float2(.27,.49+a*.04),float2(.18,.24)));
 d=max(d,lobe(uv,float2(.45,.52+b*.09),float2(.21,.32)));
 d=max(d,lobe(uv,float2(.63,.47+a*.07),float2(.20,.25)));
 d=max(d,lobe(uv,float2(.81,.395),float2(.14,.15)));
 clip(d-.008);
 half3 c=lerp(_ShadowCloud.rgb,_Crown.rgb,.30+.62*smoothstep(.32,.66,uv.y+.07*cos(uv.x*13+i.seed)));
 return half4(c,.84);}
 ENDCG}
 }}
