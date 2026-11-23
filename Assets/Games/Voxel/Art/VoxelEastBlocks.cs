using System;
using System.Collections.Generic;
using UnityEngine;
using static VoxelFabric;

internal static class VoxelEastBlocks
{
    static readonly string[] Names={"cherry","washi","lattice","petals","lacquer","basin","flume","pipe","wheel","pebble","basalt","emberglass","bracket","lens","mosaic","beacon"};
    public static bool OpenShape(string id)
    {
        int i=Array.IndexOf(Names,id);
        return i>=0 && i!=0&&i!=1&&i!=4&&i!=9&&i!=10&&i!=14;
    }
    public static bool Build(VoxelFabric m,Vector3Int cell,string id,int turns,IDictionary<Vector3Int,string> occupied)
    {
        int index=Array.IndexOf(Names,id);if(index<0)return false;
        var center=(Vector3)cell+Vector3.one*.5f;var q=Quaternion.Euler(0,turns*90,0);
        int page=20+index;
        void b(float x,float y,float z,float w,float h,float d,int tile,string color="FFFFFF",float glow=0)
        {m.Ink(tile,color,glow);m.Box(center+q*V(x,y,z),V(w,h,d),q);}
        void beam(Vector3 a,Vector3 c,float w,float d,int tile,string tint="FFFFFF")
        {m.Ink(tile,tint);m.Beam(center+q*a,center+q*c,w,d);}
        if(!OpenShape(id)){
            for(int f=0;f<6;f++){if(occupied!=null&&occupied.TryGetValue(cell+VoxelBlocksArt.Dir[f],out var adj)&&VoxelBlocksArt.IsFull(adj))continue;
                m.Ink(page);VoxelBlocksArt.Face(m,cell,f,f<2?turns:0);}
            return true;
        }
        switch(id){
            case "lattice":
                foreach(int s in new[]{-1,1}){b(s*.45f,0,0,.10f,1,.13f,20);b(0,s*.45f,0,.81f,.10f,.13f,20);}
                for(int i=-1;i<=1;i++){
                    float x=i*.32f;beam(V(x-.14f,-.39f,0),V(x+.15f,0,0),.046f,.06f,22);
                    beam(V(x+.15f,0,0),V(x-.14f,.39f,0),.046f,.06f,22);
                    beam(V(x+.14f,-.39f,0),V(x-.15f,0,0),.046f,.06f,22);beam(V(x-.15f,0,0),V(x+.14f,.39f,0),.046f,.06f,22);}break;
            case "petals":
                b(0,-.33f,0,.63f,.32f,.56f,25);b(0,-.13f,0,.74f,.09f,.66f,20);
                for(int j=0;j<7;j++){float x=Mathf.Sin(j*2.4f)*.28f,z=Mathf.Cos(j*2.4f)*.24f,y=.10f+.065f*(j%4);
                    beam(V(0,-.13f,0),V(x,y,z),.046f,.044f,Bark);b(x,y+.038f,z,.26f,.085f,.12f,23);
                    b(x,y+.043f,z,.12f,.082f,.25f,23);b(x,y+.087f,z,.062f,.026f,.058f,Brass);}break;
            case "basin":
                b(0,-.38f,0,.68f,.23f,.67f,Ashlar);b(0,-.17f,0,.89f,.26f,.88f,page);
                foreach(int s in new[]{-1,1}){b(s*.41f,.014f,0,.09f,.17f,.91f,page);b(0,.014f,s*.41f,.74f,.17f,.09f,page);}
                b(0,.023f,0,.74f,.06f,.74f,Plain,"81CDD3",.13f);break;
            case "flume":
                b(0,-.18f,0,.81f,.13f,1,26);b(0,-.091f,0,.68f,.043f,1,Plain,"8BCCD0");
                foreach(int s in new[]{-1,1}){b(s*.40f,.02f,0,.10f,.43f,1,26);b(s*.40f,.238f,0,.13f,.04f,1,Brass);}break;
            case "pipe":
                b(0,0,0,.51f,.51f,1,page);foreach(float z in new[]{-.38f,.38f})b(0,0,z,.66f,.66f,.11f,Brass,"9FB999");
                b(0,.30f,.04f,.12f,.18f,.12f,Brass);b(0,.412f,.04f,.35f,.054f,.075f,Enamel);break;
            case "wheel":
                b(0,0,0,.19f,.19f,.92f,Brass);for(int j=0;j<8;j++){float a=j*Mathf.PI/4;Vector3 rim=V(Mathf.Cos(a)*.43f,Mathf.Sin(a)*.43f,0);
                    beam(Vector3.zero,rim,.072f,.11f,page);var next=V(Mathf.Cos(a+Mathf.PI/4)*.43f,Mathf.Sin(a+Mathf.PI/4)*.43f,0);
                    beam(rim,next,.104f,.41f,page);}break;
            case "emberglass":case "beacon":
                b(0,-.40f,0,.72f,.18f,.70f,30);b(0,.42f,0,.63f,.13f,.63f,32);
                for(int j=0;j<3;j++){b(0,-.23f+j*.22f,0,.42f+j*.025f,.19f,.42f+j*.025f,page,"FFFFFF",.75f);
                    b(0,-.119f+j*.22f,0,.53f,.035f,.53f,Brass);}
                foreach(int s in new[]{-1,1})foreach(int t in new[]{-1,1})b(s*.294f,0,t*.294f,.043f,.79f,.043f,32);break;
            case "bracket":
                b(0,0,.32f,.68f,1,.17f,page);b(0,.38f,0,.78f,.18f,.87f,page);
                foreach(float x in new[]{-.25f,.25f})beam(V(x,-.40f,.28f),V(x,.31f,-.36f),.09f,.09f,page);break;
            case "lens":
                b(0,-.42f,0,.68f,.16f,.55f,30);b(0,-.17f,0,.15f,.44f,.18f,Brass);
                beam(V(0,-.02f,.32f),V(0,.20f,-.39f),.316f,.32f,32);b(0,.20f,-.40f,.35f,.31f,.095f,page,"FFFFFF",.29f);
                b(0,.23f,-.46f,.14f,.07f,.032f,Plain,"B4E9E2");break;
        }
        return true;
    }
}
