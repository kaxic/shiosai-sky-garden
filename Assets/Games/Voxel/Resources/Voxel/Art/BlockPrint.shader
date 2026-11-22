Shader "Voxel/Block print" {
 Properties{_Atlas("Hand-painted 4 by 4 block atlas",2D)="white"{} _Air("Horizon",Color)=(.70,.84,.86,1)}
 SubShader { Tags {"RenderType"="Opaque"}
 Pass { Tags {"LightMode"="ForwardBase"}
 CGPROGRAM
 #pragma target 3.0
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fwdbase
 #include "UnityCG.cginc"
 #include "AutoLight.cginc"
 sampler2D _Atlas;half4 _Air;float4 _Atlas_TexelSize;
 struct app{float4 vertex:POSITION;float3 normal:NORMAL;float4 uv:TEXCOORD0;half4 color:COLOR;};
 struct ps{float4 pos:SV_POSITION;float4 uv:TEXCOORD0;half3 n:TEXCOORD1;float dist:TEXCOORD2;half4 color:COLOR;SHADOW_COORDS(3) float3 world:TEXCOORD4;};
 ps vert(app v){ps o;o.pos=UnityObjectToClipPos(v.vertex);o.n=UnityObjectToWorldNormal(v.normal);o.uv=v.uv;o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.dist=distance(o.world,_WorldSpaceCameraPos);TRANSFER_SHADOW(o);return o;}
 half3 page(float id,float2 uv){float2 p=float2(fmod(id,4),3-floor(id/4));float2 pad=_Atlas_TexelSize.xy*2;
  half3 c=tex2D(_Atlas,p*.25+lerp(pad,.25-pad,saturate(uv))).rgb;
  #ifndef UNITY_COLORSPACE_GAMMA
    c=LinearToGammaSpace(c);
  #endif
  return c;}
 float lane(float2 p,float2 a,float2 b){float2 d=b-a;return length(p-a-d*saturate(dot(p-a,d)/dot(d,d)));}
 half4 frag(ps i):SV_Target{
  float2 uv=i.uv.xy;float tile=floor(i.uv.z+.5);
  half3 art=1;
  if(tile>18.5){art=lerp(half3(.78,.88,.91),page(15,frac(i.world.xz*.24))*half3(.84,1.03,1.12),.31);}
  else if(tile>17.5){art=lerp(half3(.84,.64,.41),page(3,frac(i.world.xz*.32)),.36);}
  else if(tile>16.5){art=lerp(half3(.53,.69,.40),page(0,frac(i.world.xz*.355)) *half3(.96,1.03,.85),.37);}
  else if(tile>15.5){
    float h=frac(floor(uv.x*16)*.613)*.11+.72;
    art=uv.y>h?page(0,uv)*half3(.90,.98,.88):page(1,uv)*lerp(.83,1,step(.055,h-uv.y));
  } else if(tile>=0)art=page(tile,uv);
  if(tile>16.5){
   float2 p=i.world.xz;float2 crossroad=float2(.42,-1.64);
   float trail=min(lane(p,float2(.52,-5.47),crossroad),min(lane(p,crossroad,float2(-6.47,3.42)),lane(p,crossroad,float2(7.48,-1.54))));
   float grit=page(3,frac(p*.44)).r;
   float strength=(1-smoothstep(.17,.59,trail+.17*cos(p.y*2+p.x*.7)+.18*(grit-.65)))*.73;
   half3 worn=tile>18.5?half3(.68,.77,.81):tile>17.5?half3(.75,.54,.39):half3(.72,.68,.46);
   art=lerp(art,worn*(.88+.17*grit),strength);
  }
  float3 n=normalize(i.n);half3 sun=normalize(half3(-.55,.91,-.38));
  half diffuse=saturate(dot(n,sun));half shadow=SHADOW_ATTENUATION(i);
  half3 ambient=lerp(half3(.50,.58,.65),half3(.83,.87,.89),n.y*.5+.5);
  half3 light=ambient*lerp(half3(.79,.85,.92),half3(1,1,1),shadow)+diffuse*lerp(.13,.34,shadow)*half3(1,.91,.69);
  half3 rgb=art*i.color.rgb*lerp(light,half3(1.16,1.12,1.03),i.uv.w);
  #ifndef UNITY_COLORSPACE_GAMMA
   rgb=GammaToLinearSpace(saturate(rgb));
  #endif
  rgb=lerp(rgb,_Air.rgb,smoothstep(32,110,i.dist)*.54);
  return half4(rgb,1);
 }
 ENDCG
 } UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 } Fallback "Diffuse"
}
