using System.Collections.Generic;
using UnityEngine;
using static VoxelFabric;

internal static class VoxelBlocksArt
{
    public static readonly Vector3Int[] Dir={Vector3Int.up,Vector3Int.down,Vector3Int.right,Vector3Int.left,Vector3Int.forward,Vector3Int.back};
    static readonly Vector3[,] Corner={
        {V(0,1,0),V(0,1,1),V(1,1,1),V(1,1,0)},{V(0,0,1),V(0,0,0),V(1,0,0),V(1,0,1)},
        {V(1,0,1),V(1,0,0),V(1,1,0),V(1,1,1)},{V(0,0,0),V(0,0,1),V(0,1,1),V(0,1,0)},
        {V(0,0,1),V(1,0,1),V(1,1,1),V(0,1,1)},{V(1,0,0),V(0,0,0),V(0,1,0),V(1,1,0)}};
    public static int Hash(int x,int y,int z){unchecked{uint n=(uint)(x*73856093^y*19349663^z*83492791);n^=n>>13;n*=1274126177u;return (int)((n^(n>>16))&0x7fffffff);}}
    internal static void Face(VoxelFabric m,Vector3Int cell,int f,int turn=0,float shade=1)
    {
        var p=(Vector3)cell;
        m.Face(p+Corner[f,0],p+Corner[f,1],p+Corner[f,2],p+Corner[f,3],turn,shade,shade,1,1);
    }
    public static void Terrain(VoxelFabric m,VoxelGame g)
    {
        for(int z=-9;z<=9;z++)for(int x=-11;x<=11;x++){
            int top=g.GroundTop(x,z),bottom=g.GroundBottom(x,z);if(top<=-900)continue;
            for(int y=bottom;y<top;y++){
                var cell=new Vector3Int(x,y,z);int h=Hash(x,y,z);
                for(int f=0;f<6;f++){
                    var other=cell+Dir[f];bool solid=other.y>=g.GroundBottom(other.x,other.z)&&other.y<g.GroundTop(other.x,other.z);
                    if(solid)continue;
                    bool surface=y==top-1;
                    if(g.Island==0){
                        if(f==0&&surface)m.Ink(17);
                        else if(f>=2&&surface)m.Ink(SodEdge,"D7E4B8");
                        else m.Ink(y>=top-3?Earth:h%11==0?Vein:Rock,y>=top-3?"DDBB95":"B9C1CA");
                    }else if(g.Island==1){
                        if(f==0&&surface)m.Ink(18);
                        else if(y>=top-2)m.Ink(Sandstone,"E4A878");
                        else m.Ink(h%12==0?Brick:Rock,y%3==0?"C5917F":"E7AD89");
                    }else if(g.Island>=3){
                        int tile=g.Island==3?(surface?0:23):g.Island==4?(surface?29:10):(surface?30:31);
                        m.Ink(tile,g.Island==3&&surface?"CDD7B3":"FFFFFF");
                    }else{
                        if(f==0&&surface)m.Ink(19);
                        else if(surface)m.Ink(Marble,"C7E7F1");
                        else m.Ink(h%8==0?Vein:Rock,y%3==0?"7FADD2":"A2BFDE");
                    }
                    Face(m,cell,f,f==0?h%4:0,f>=2?.85f:1);
                }
            }
        }
    }
    public static bool IsPanel(string id)=>id=="glass"||id=="frost";
    public static bool IsFull(string id)=>!VoxelEastBlocks.OpenShape(id)&&id!="steps"&&id!="vane"&&id!="lantern"&&id!="bloom"&&!IsPanel(id)&&id!="crystal"&&id!="star";
    public static void Build(VoxelFabric m,Vector3Int cell,string id,int turns,IDictionary<Vector3Int,string> occupied,string tint)
    {
        Vector3 p=(Vector3)cell+Vector3.one*.5f;Quaternion q=Quaternion.Euler(0,turns*90,0);
        void block(float x,float y,float z,float w,float h,float d,int tile,string color="FFFFFF",float glow=0)
        {m.Ink(tile,color,glow);m.Box(p+q*V(x,y,z),V(w,h,d),q);}
        if(VoxelEastBlocks.Build(m,cell,id,turns,occupied)) return;
        bool has(Vector3Int at,string material)=>occupied!=null&&occupied.TryGetValue(at,out var existing)&&existing==material;
        // A run of steps on parallel planks atop log feet joins into garden seating.
        // Individual cells still belong to their corresponding editable materials.
        Vector3Int shelf=cell+(id=="wood"?Vector3Int.up:id=="steps"?Vector3Int.down:Vector3Int.zero);
        bool stacked(Vector3Int at)=>has(at,"plank")&&has(at+Vector3Int.up,"steps");
        var rowAxis=Vector3Int.right;
        if(stacked(shelf)&&!stacked(shelf+rowAxis)&&!stacked(shelf-rowAxis))rowAxis=Vector3Int.forward;
        bool aligned=id!="steps"||(turns&1)==(rowAxis==Vector3Int.right?0:1);
        if(aligned&&(id=="wood"||id=="plank"||id=="steps")&&stacked(shelf)&&(stacked(shelf+rowAxis)||stacked(shelf-rowAxis))){
            q=id=="steps"?q:rowAxis==Vector3Int.forward?Quaternion.Euler(0,-90,0):Quaternion.identity;
            if(id=="wood"){
                foreach(float z in new[]{-.29f,.29f}){block(0,-.01f,z,.315f,.98f,.25f,Bark,"DFC9A9");block(0,-.43f,z,.38f,.14f,.31f,Boards);}
                block(0,-.15f,0,.17f,.15f,.72f,Boards);return;
            }
            if(id=="plank"){
                foreach(float z in new[]{-.30f,0,.30f})block(0,.365f,z,.997f,.19f,.28f,Boards,"F4D6AA");
                foreach(float z in new[]{-.305f,.305f}){
                    block(0,.163f,z,1,.235f,.11f,Boards,"D3B18E");
                    if(has(cell+Vector3Int.down,"wood"))block(0,-.213f,z,.27f,.60f,.24f,Bark,"DDC8A2");}
                return;
            }
            block(0,-.068f,.34f,.13f,.86f,.11f,Bark);
            block(0,-.105f,.31f,1,.215f,.113f,Boards,"EDCFA6");block(0,.256f,.31f,1,.221f,.114f,Boards,"F0D5A7");
            var right=Vector3Int.RoundToInt(q*Vector3.right);
            foreach(int s in new[]{-1,1})if(!stacked(shelf+right*s)){
                block(.37f*s,-.255f,-.17f,.103f,.48f,.115f,Bark);block(.37f*s,-.024f,-.007f,.151f,.105f,.75f,Boards,"E5C79E");
                block(.37f*s,.013f,-.31f,.145f,.037f,.094f,Brass);}
            return;
        }
        if(id=="steps"){
            for(int i=0;i<3;i++){float h=(i+1)/3f;block(0,-.5f+h*.5f,(i-1)/3f,1,h,1/3f,Boards,"D1B695");}
            return;
        }
        if(id=="vane"){
            block(0,-.43f,0,.57f,.14f,.57f,Ashlar);block(0,-.07f,0,.12f,.72f,.12f,Brass);
            block(0,.20f,-.02f,.105f,.11f,.83f,Brass);block(0,.28f,-.31f,.11f,.18f,.20f,Enamel);
            block(0,.22f,.31f,.12f,.19f,.21f,Brass);block(0,.22f,.45f,.086f,.088f,.09f,Brass);
            block(0,.36f,0,.16f,.16f,.16f,Brass);return;
        }
        if(id=="lantern"){
            block(0,-.40f,0,.69f,.20f,.69f,Ashlar);block(0,-.21f,0,.50f,.18f,.50f,Brass);
            block(0,.045f,0,.45f,.43f,.45f,Plain,"FFD68A",.88f);
            foreach(float a in new[]{-.251f,.251f})foreach(float b in new[]{-.251f,.251f})block(a,.044f,b,.063f,.47f,.063f,Plain,"715E54");
            block(0,.311f,0,.67f,.101f,.67f,Enamel);block(0,.414f,0,.38f,.105f,.38f,Brass);return;
        }
        if(IsPanel(id)){
            bool icy=id=="frost";string frame=icy?"BFD8E1":"6CA4A7";
            foreach(float x in new[]{-.466f,.466f})foreach(float z in new[]{-.466f,.466f})block(x,0,z,.068f,1,.068f,Plain,frame);
            foreach(float y in new[]{-.466f,.466f})foreach(float x in new[]{-.466f,.466f}){block(x,y,0,.07f,.07f,.94f,Plain,frame);block(0,y,x,.94f,.07f,.07f,Plain,frame);}
            block(.01f,.09f,-.450f,.18f,.05f,.02f,Plain,"DCF7EB");block(-.10f,.03f,-.452f,.09f,.06f,.022f,Plain,"DCF7EB");return;
        }
        if(id=="star"){
            block(0,-.415f,0,.58f,.17f,.56f,Enamel);block(0,-.205f,0,.14f,.28f,.14f,Brass);
            for(int side=0;side<2;side++)for(int i=0;i<10;i++){
                Vector3 tip(int n){float a=n*Mathf.PI*.20f,r=n%2==0?.427f:.186f;return V(Mathf.Sin(a)*r,.064f+Mathf.Cos(a)*r,0);}
                var a=p+q*tip(i);var b=p+q*tip(i+1);var center=p+q*V(0,.064f,side==0?-.143f:.143f);
                m.Ink(Plain,(i+side)%2==0?"F5CD69":"DEA556",.3f);
                if(side==0)m.Face(center,a,b,b);else m.Face(center,b,a,a);
            }return;
        }
        if(id=="crystal"){
            block(0,-.40f,0,.86f,.20f,.82f,Vein);
            for(int i=0;i<3;i++){float x=(i-1)*.20f,h=i==1?.72f:.42f;block(x,-.3f+h*.5f,i%2*.12f,.205f,h,.21f,Plain,i==1?"9AA7DF":"81BACF",.32f);block(x,-.27f+h,i%2*.12f,.127f,.10f,.13f,Plain,"D5D6FA",.4f);}return;
        }
        if(id=="bloom"){
            block(0,-.32f,0,.78f,.35f,.76f,Brick,"D8AB96");block(0,-.13f,0,.87f,.10f,.84f,Boards,"BD986E");
            for(int i=0;i<3;i++){float x=(i-1)*.23f,z=i%2*.24f-.1f,h=.28f+i*.085f;
                block(x,-.065f+h*.5f,z,.042f,h,.043f,Plain,"528051");
                block(x,h-.064f,z,.24f,.093f,.11f,Plain,i==1?"F4CA6E":"E99FA0");block(x,h-.064f,z,.11f,.087f,.237f,Plain,i==1?"F4CA6E":"E99FA0");
                block(x,h-.011f,z,.067f,.036f,.071f,Plain,"F3D385");
            }return;
        }
        for(int f=0;f<6;f++){
            if(occupied!=null&&occupied.TryGetValue(cell+Dir[f],out var adjacent)&&IsFull(adjacent))continue;
            int t;string shade="FFFFFF";
            switch(id){
                case "turf":t=f==0?Meadow:f==1?Earth:SodEdge;break;
                case "wood":t=f<2?Endgrain:Bark;break;
                case "stone":t=Ashlar;break;
                case "plank":t=Boards;break;
                case "sandstone":t=Sandstone;break;
                case "copper":t=Brass;shade="DF986F";break;
                case "slate":t=Ashlar;shade="8EA4C2";break;
                case "tile":t=f==0?Roof:Brick;break;
                default:t=Plain;shade=string.IsNullOrEmpty(tint)?"E6CA9B":tint;break;
            }m.Ink(t,shade,id=="star"?.12f:0);Face(m,cell,f,f<2?turns:0);
        }
    }
}
