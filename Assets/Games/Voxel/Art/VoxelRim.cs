using System.Collections.Generic;
using UnityEngine;
using static VoxelFabric;

// Shallow ground-cover only. Writing to a decorated column clears its planting.
internal static class VoxelRim
{
    static readonly Vector2[] Beds={new Vector2(-6.3f,-4.42f),new Vector2(-2.78f,-5.55f),new Vector2(4.5f,-5.12f),
        new Vector2(7.52f,1.44f),new Vector2(5.39f,5.13f),new Vector2(-1.16f,5.34f),new Vector2(-7.41f,.24f)};
    public static void Build(VoxelFabric m,VoxelGame g,HashSet<Vector2Int> covered)
    {
        int island=g.Island;var west=g.Landmark(0);var east=g.Landmark(1);
        for(int bed=0;bed<Beds.Length;bed++){
            var center=Beds[bed];float azimuth=bed%2==0?.24f:-.61f;
            for(int j=0;j<17;j++){
                float angle=j*2.39996f,r=Mathf.Sqrt((j+.5f)/17);
                float a=Mathf.Cos(angle)*r*1.63f,b=Mathf.Sin(angle)*r*.69f;
                float x=center.x+a*Mathf.Cos(azimuth)-b*Mathf.Sin(azimuth),z=center.y+a*Mathf.Sin(azimuth)+b*Mathf.Cos(azimuth);
                var cell=new Vector2Int(Mathf.FloorToInt(x),Mathf.FloorToInt(z));int y=g.GroundTop(cell.x,cell.y);
                if(y<=-900||covered.Contains(cell))continue;
                if(Mathf.Abs(cell.x-g.Dock.x)+Mathf.Abs(cell.y-g.Dock.z)==0)continue;
                if(cell==new Vector2Int(west.x,west.z)||cell==new Vector2Int(east.x,east.z))continue;
                var p=V(x,y+.017f,z);int hash=VoxelBlocksArt.Hash(j,island,bed);
                void box(Vector3 delta,Vector3 size,string shade,Quaternion rot,int tile=Plain,float glow=0){
                    m.Ink(tile,shade,glow);m.Box(p+delta,size,rot);}
                void grass(float h,float tilt,string color){
                    box(V(0,h*.48f,0),V(.041f,h,.067f),color,Quaternion.Euler(7,hash%175,tilt));
                    box(V(.064f,h*.31f,-.033f),V(.044f,h*.69f,.069f),color,Quaternion.Euler(5,hash%161,-tilt));
                }
                if(island==0){
                    float h=.19f+hash%21*.009f;grass(h,17,"699458");
                    if(j%4!=0){
                        string petals=bed%3==0?(j%2==0?"E8A3A6":"F5CAB4"):bed%3==1?(j%2==0?"EABE78":"F8DB92"):(j%2==0?"A5B6D2":"E5DAF0");
                        box(V(0,h,0),V(.228f,.061f,.097f),petals,Quaternion.identity);
                        box(V(0,h,0),V(.100f,.056f,.232f),petals,Quaternion.identity);
                        box(V(0,h+.042f,0),V(.069f,.037f,.071f),"E2B44E",Quaternion.identity);
                    }else box(V(.061f,.075f,.05f),V(.23f,.080f,.14f),"7FA86B",Quaternion.Euler(0,hash%130,0));
                }else if(island==1){
                    float h=.28f+(hash%20)*.011f;grass(h,11,"AA8B5C");
                    if(j%3!=0){
                        for(int k=0;k<4;k++)box(V(k*.017f,h*.69f+.061f*k,0),V(k==3?.047f:.088f,.081f,.068f),
                            k%2==0?"E4B47B":"D2A064",Quaternion.Euler(0,hash%155,-13));
                    }else{
                        box(V(.044f,.063f,.01f),V(.30f,.13f,.21f),"DAAC8F",Quaternion.Euler(0,hash%110,0),Sandstone);
                        box(V(-.10f,.036f,.121f),V(.13f,.07f,.09f),"BD987D",Quaternion.identity,Rock);
                    }
                }else{
                    float h=.17f+hash%16*.016f;var q=Quaternion.Euler(0,hash%160,j%2==0?14:-12);
                    string ice=j%3==0?"AD9FCB":"85BBCF";
                    box(q*V(0,h*.43f,0),V(.108f,h*.86f,.13f),ice,q,Plain,.12f);
                    box(q*V(0,h*.92f,0),V(.066f,h*.19f,.084f),"D4EBF0",q,Plain,.32f);
                    box(V(.091f,.043f,.049f),V(.226f,.087f,.155f),"CCDDE7",Quaternion.Euler(0,hash%77,0),Marble);
                }
            }
        }
    }
}
