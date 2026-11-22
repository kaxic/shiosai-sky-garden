using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// UV.z stores the page of the hand-painted atlas. Fixed face normals keep pixel art crisp.
internal sealed class VoxelFabric
{
    internal const int Plain=-1,Meadow=0,Earth=1,Rock=2,Sand=3,Bark=4,Endgrain=5,
        Boards=6,Leaf=7,Brick=8,Ashlar=9,Sandstone=10,Roof=11,Vein=12,Enamel=13,Brass=14,Marble=15,SodEdge=16;
    public static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    public static Color32 ColorOf(string hex){ColorUtility.TryParseHtmlString(hex[0]=='#'?hex:"#"+hex,out var c);return c;}
    readonly List<Vector3> pos=new List<Vector3>(),normals=new List<Vector3>();
    readonly List<Vector4> uv=new List<Vector4>();readonly List<Color32> colors=new List<Color32>();readonly List<int> tri=new List<int>();
    public Color32 tint=new Color32(255,255,255,255);
    public int tile=Plain;public float glow;
    public void Ink(int atlas,string shade="FFFFFF",float emission=0){tile=atlas;tint=ColorOf(shade);glow=emission;}
    public void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int turn=0,float ao0=1,float ao1=1,float ao2=1,float ao3=1)
    {
        var normal=Vector3.Cross(b-a,c-a).normalized;int first=pos.Count;
        void Put(Vector3 p,int n,float k){
            pos.Add(p);normals.Add(normal);int t=(n+turn)&3;
            uv.Add(new Vector4(t==1||t==2?1:0,t>=2?1:0,tile,glow));
            colors.Add(new Color32((byte)(tint.r*k),(byte)(tint.g*k),(byte)(tint.b*k),tint.a));
        }
        Put(a,0,ao0);Put(b,1,ao1);Put(c,2,ao2);Put(d,3,ao3);
        tri.Add(first);tri.Add(first+1);tri.Add(first+2);tri.Add(first);tri.Add(first+2);tri.Add(first+3);
    }
    public void Box(Vector3 center,Vector3 size,Quaternion rotation,int top=-100,int side=-100,int bottom=-100)
    {
        int initial=tile;Vector3 P(float x,float y,float z)=>center+rotation*Vector3.Scale(V(x,y,z),size)*.5f;
        var a=P(-1,-1,-1);var b=P(1,-1,-1);var c=P(1,-1,1);var d=P(-1,-1,1);
        var e=P(-1,1,-1);var f=P(1,1,-1);var g=P(1,1,1);var h=P(-1,1,1);
        tile=side==-100?initial:side;
        Face(b,a,e,f);Face(d,c,g,h);Face(a,d,h,e);Face(c,b,f,g);
        tile=top==-100?initial:top;Face(e,h,g,f);
        tile=bottom==-100?initial:bottom;Face(d,a,b,c);tile=initial;
    }
    public void B(float x,float y,float z,float w,float h,float d)=>Box(V(x,y,z),V(w,h,d),Quaternion.identity);
    public void Beam(Vector3 a,Vector3 b,float w,float d)=>Box((a+b)*.5f,V(w,(b-a).magnitude,d),Quaternion.FromToRotation(Vector3.up,b-a));
    public Mesh Build(string id)
    {
        var mesh=new Mesh{name=id};if(pos.Count>65530)mesh.indexFormat=IndexFormat.UInt32;
        mesh.SetVertices(pos);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(tri,0);mesh.RecalculateBounds();return mesh;
    }
}
