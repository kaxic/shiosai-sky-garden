Shader "Voxel/Build overlay" {
 Properties{_Ink("Ink",Color)=(.6,1,.84,1) _Fill("Face fill",Range(0,1))=.13 _Edge("Edge alpha",Range(0,1))=.90}
 SubShader {Tags {"Queue"="Transparent+20" "RenderType"="Transparent"} Cull Back ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 half4 _Ink;half _Fill,_Edge;
 struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 struct p{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
 p vert(a i){p o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.c=i.color;return o;}
 half4 frag(p i):SV_Target{float2 edges=min(i.uv,1-i.uv);float d=min(edges.x,edges.y);float rim=1-smoothstep(.018,.040,d);
 return half4(_Ink.rgb*i.c.rgb,lerp(_Fill,_Edge,rim));}
 ENDCG}
 }
}
