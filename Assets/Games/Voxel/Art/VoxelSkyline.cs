using UnityEngine;
using static VoxelFabric;

// Coastal views around the editable grid. The three monuments have separate models;
// the distant islands and weather can be seen while orbiting the work.
internal sealed class VoxelSkyline
{
    readonly VoxelFabric m; readonly int biome; Vector3 o;
    public VoxelSkyline(VoxelFabric mesh,int island){m=mesh;biome=island;}
    void B(float x,float y,float z,float w,float h,float d,int tile,string ink="FFFFFF",float glow=0)
    {m.Ink(tile,ink,glow);m.Box(o+V(x,y,z),V(w,h,d),Quaternion.identity);}
    void Beam(Vector3 a,Vector3 b,float w,float d,int tile,string tint="FFFFFF"){
        m.Ink(tile,tint);m.Beam(o+a,o+b,w,d);}
    public static Vector3 MillAt=>V(-11.9f,-.3f,8.7f);
    public void Make(VoxelGame g)
    {
        for(int i=0;i<2;i++){
            var key=g.Landmark(i);o=(Vector3)key+V(.5f,0,.5f);
            B(0,.048f,0,.99f,.094f,.99f,Enamel);B(0,.22f,0,.58f,.29f,.58f,biome==0?Boards:biome==1?Sandstone:Ashlar);
            if(i==0){
                if(biome==0){B(0,.69f,0,.14f,.81f,.14f,Brass);m.Ink(Enamel);m.Box(o+V(0,1.14f,0),V(.58f,.16f,.28f),Quaternion.Euler(0,33,0));}
                if(biome==1){B(0,.57f,0,.46f,.48f,.47f,Sandstone);B(0,.88f,0,.30f,.19f,.30f,Enamel);B(0,1.06f,0,.13f,.20f,.13f,Brass);}
                if(biome==2){Crystal(V(0,.37f,0),.23f,1.15f,28,"80C5DC");Crystal(V(.23f,.35f,.12f),.14f,.61f,-12,"AD96D3");}
            }else{
                foreach(float x in new[]{-.22f,.22f})B(x,.68f,0,.15f,.72f,.21f,biome==0?Bark:biome==1?Sandstone:Marble);
                B(0,1.00f,0,.70f,.17f,.28f,biome==0?Boards:Enamel);B(0,.76f,0,.25f,.30f,.25f,Plain,g.Found(i)?"FBD786":"A8CFC7",.5f);
            }
        }
        var dock=g.Dock;o=(Vector3)dock+V(.5f,0,.5f);
        B(0,.049f,0,.98f,.09f,.97f,Boards);B(-.35f,.40f,.19f,.10f,.70f,.10f,Bark);
        B(-.35f,.97f,.19f,.095f,.50f,.098f,Bark);B(-.35f,1.24f,.19f,.125f,.08f,.14f,Brass);
        B(-.07f,1.11f,.19f,.51f,.23f,.055f,Enamel);B(-.35f,.36f,-.34f,.26f,.54f,.23f,Boards);
        B(-.35f,.65f,-.34f,.33f,.05f,.27f,Plain,"CD9A6D");B(-.35f,.70f,-.34f,.12f,.08f,.14f,Brass);
        Artisan(V(.20f,.085f,.08f));
        o=MillAt;Islet(5.4f,4.0f,2.5f,11);
        if(biome==0)Mill();else if(biome==1)Observatory();else Pavilion();
        o=V(12.8f,-1.3f,8.9f);Islet(4.7f,4.5f,2.8f,9);
        if(biome==2){Crystal(V(-.61f,0,0),.47f,3.22f,-9,"95C2E1");Crystal(V(.35f,0,.23f),.40f,2.12f,18,"AD94CD");}
        else {Tree(-.72f,0,.30f,1.25f,9);Tree(.73f,0,-.39f,.87f,21);}
        o=V(-1.22f,.03f,12.36f);Airship();
        for(int j=0;j<9;j++){
            float a=j*Mathf.PI*2/9+.23f,r=27+j%3*6;
            o=V(Mathf.Cos(a)*r,-4+j%3*1.6f,Mathf.Sin(a)*r);
            Islet(4+j%3*1.1f,4.2f,3.4f,23+j);
            if(j%3==0&&biome==1)Observatory();else if(j%3==2&&biome==2)Pavilion();
            else {Tree(-.65f,0,.27f,1.0f+j%2*.32f,j+23);Tree(.87f,0,.18f,.65f+j%3*.11f,j+31);}
        }
    }
    void Airship()
    {
        // Moored above the northern channel, clear of the editable grid at every height.
        string trim=biome==0?"709E9F":biome==1?"BC8271":"9C96B9";
        B(0,.18f,0,1.75f,.35f,.74f,Boards,"C69E79");B(.04f,.37f,0,2.13f,.15f,.85f,Bark,"D7BC91");
        B(.03f,.47f,0,1.70f,.075f,.62f,Boards);
        for(int j=0;j<9;j++){
            float x=(j-4)*.44f;float radius=Mathf.Sqrt(Mathf.Max(.09f,1f-Mathf.Pow((j-4)/4.35f,2)));
            float h=radius*1.65f,w=radius*1.52f;
            B(x,2.24f,0,.458f,h,w*.66f,Plain,j==2||j==6?"C49159":"E4C890");
            B(x,2.24f,0,.461f,h*.69f,w,Plain,j==2||j==6?trim:"EED5A1");
            B(x,2.24f+h*.40f,0,.460f,h*.25f,w*.48f,Plain,j==2||j==6?trim:"F1DEB2");
        }
        for(int j=0;j<2;j++)foreach(float side in new[]{-1f,1f}){
            float x=(j==0?-1:1)*.90f;Beam(V(x,.49f,side*.35f),V(x,1.99f,side*.66f),.038f,.038f,Bark,"BFA88B");}
        m.Ink(Plain,trim);m.Box(o+V(-1.88f,2.39f,0),V(.68f,.68f,.096f),Quaternion.Euler(0,0,18));
        B(-1.80f,2.21f,0,.67f,.09f,1.24f,Plain,trim);B(.52f,.64f,0,.48f,.35f,.50f,Enamel);
    }
    void Islet(float width,float depth,float height,int seed)
    {
        string rim=biome==0?"AEC991":biome==1?"E9B98A":"C3D5E2";int top=biome==0?17:biome==1?18:19;
        m.Ink(Rock,rim);m.Box(o+V(0,-.36f,0),V(width,.72f,depth*.70f),Quaternion.identity,top,biome==0?SodEdge:biome==1?Sandstone:Marble);
        m.Box(o+V(.21f,-.37f,.09f),V(width*.73f,.70f,depth),Quaternion.identity,top,biome==0?SodEdge:biome==1?Sandstone:Marble);
        for(int j=0;j<3;j++){
            float frac=1-j*.21f;B(j*.11f-.12f,-.78f-j*height*.24f,0,width*frac*.88f,height*.38f,depth*frac*.65f,Rock,biome==2?"9EB4CB":"ABB7AE");}
        for(int j=0;j<4;j++){float a=(j+seed)*2.34f;
            B(Mathf.Sin(a)*width*.28f,-height*.77f-j*.18f,Mathf.Cos(a)*depth*.21f,.56f+j*.12f,height*.52f,.60f+j*.09f,j==2?Vein:Rock,biome==1?"D8AF95":"AEC3C4");}
    }
    void Tree(float x,float y,float z,float s,int k)
    {
        B(x,y+s*.75f,z,.32f*s,1.5f*s,.34f*s,Bark);
        for(int j=0;j<4;j++){
            float a=(k+j)*2.43f,size=(1.39f-j*.105f)*s;
            float xx=x+Mathf.Cos(a)*s*.29f,zz=z+Mathf.Sin(a)*s*.29f;
            if(biome==2){B(x,y+1.4f*s+j*.33f*s,z,size,.40f*s,size,Leaf,"85ABAF");if(j%2==0)B(x,y+1.66f*s+j*.33f*s,z,size*.91f,.11f*s,size*.91f,Marble,"DEF0EE");}
            else B(xx,y+(1.36f+j*.20f)*s,zz,size,.71f*s,size*.90f,Leaf,biome==0?(j%2==0?"CDDBA2":"C0D4A7"):(j%2==0?"F0A16E":"C78B6B"));
        }
    }
    void Mill()
    {
        B(0,.18f,0,2.24f,.36f,1.79f,Ashlar);B(0,1.28f,0,1.66f,2.00f,1.40f,Marble,"F6E2B8");
        B(0,2.47f,.05f,1.39f,.73f,1.20f,Marble,"F9DFB8");
        foreach(float x in new[]{-.77f,.77f}){B(x,1.22f,-.69f,.12f,1.88f,.12f,Bark);B(x,1.22f,.69f,.12f,1.88f,.12f,Bark);}
        B(0,.72f,-.72f,.52f,.92f,.085f,Boards);B(0,.91f,-.775f,.19f,.18f,.05f,Enamel);
        B(0,1.60f,0,1.74f,.12f,1.48f,Boards);
        for(int j=0;j<4;j++)B(0,2.90f+j*.17f,.04f,1.79f-j*.35f,.225f,1.61f-j*.30f,Roof);
        B(.43f,2.94f,.46f,.28f,.73f,.28f,Brick);
        B(-1.23f,.39f,.24f,.78f,.73f,.82f,Boards);B(-1.23f,.79f,.24f,.88f,.12f,.92f,Roof);
    }
    public static Vector3 RotorPosition=>MillAt+V(0,2.16f,-.82f);
    public void Rotor()
    {
        for(int j=0;j<4;j++){
            Quaternion q=Quaternion.Euler(0,0,j*90+25);void box(Vector3 local,Vector3 size,int t,string c){m.Ink(t,c);m.Box(q*local,size,q);}
            box(V(0,.79f,0),V(.105f,1.58f,.093f),Boards,"E4BE94");
            for(int n=0;n<4;n++){
                box(V(.14f,.57f+n*.205f,-.062f),V(.34f,.175f,.045f),Plain,n%2==0?"F5E0B2":"E8CA99");}
            box(V(.30f,.94f,-.018f),V(.041f,.92f,.061f),Boards,"D9BD92");
        }B(0,0,-.09f,.29f,.29f,.25f,Brass);
    }
    void Observatory()
    {
        for(int j=0;j<3;j++)B(0,j*.219f+.109f,0,2.70f-j*.39f,.218f,2.27f-j*.30f,Sandstone);
        foreach(float x in new[]{-.68f,.68f})foreach(float z in new[]{-.47f,.47f}){
            B(x,1.39f,z,.30f,1.50f,.31f,Sandstone);B(x,1.96f,z,.38f,.22f,.39f,Enamel);}
        B(0,2.21f,0,1.86f,.28f,1.43f,Sandstone);
        B(0,2.40f,0,1.33f,.15f,.98f,Brass);
        m.Ink(Enamel);m.Box(o+V(0,2.87f,0),V(.62f,.62f,.62f),Quaternion.Euler(31,28,43));
    }
    void Pavilion()
    {
        B(0,.135f,0,2.67f,.27f,2.10f,Marble,"CAEBED");
        foreach(float x in new[]{-.93f,.93f})foreach(float z in new[]{-.69f,.69f}){B(x,1.07f,z,.17f,1.86f,.17f,Enamel);B(x,.37f,z,.29f,.34f,.28f,Marble);}
        foreach(float x in new[]{-.54f,.54f}){
            m.Ink(Marble,"8EB8D0");m.Box(o+V(x,2.20f,0),V(1.40f,.13f,1.99f),Quaternion.Euler(0,0,x<0?32:-32));}
        B(0,2.53f,0,.16f,.19f,2.06f,Brass);Crystal(V(0,.27f,0),.26f,1.22f,7,"9BB2E1");
    }
    void Crystal(Vector3 at,float r,float h,float angle,string shade)
    {
        var q=Quaternion.Euler(0,26,angle);
        m.Ink(Plain,shade,.26f);m.Box(o+at+q*V(0,h*.40f,0),V(r*1.42f,h*.80f,r*1.42f),q);
        m.Ink(Plain,"C3EDF2",.4f);m.Box(o+at+q*V(0,h*.85f,0),V(r*.93f,h*.17f,r*.93f),q);
        m.Box(o+at+q*V(0,h*.96f,0),V(r*.48f,h*.07f,r*.48f),q);
    }
    void Artisan(Vector3 at)
    {
        o+=at;B(0,.17f,0,.24f,.32f,.16f,Plain,"455F6A");
        foreach(float x in new[]{-.073f,.073f})B(x,.038f,-.035f,.124f,.075f,.226f,Plain,"71564E");
        B(0,.41f,0,.31f,.30f,.20f,Plain,"548F91");B(0,.42f,-.115f,.22f,.25f,.045f,Plain,"C19366");
        B(.176f,.35f,.01f,.09f,.22f,.115f,Plain,"EAC299");B(-.176f,.35f,.01f,.09f,.22f,.115f,Plain,"EAC299");
        B(0,.665f,0,.268f,.25f,.255f,Plain,"E8BC91");B(0,.780f,.015f,.29f,.10f,.26f,Plain,"9F755B");
        B(0,.801f,.01f,.41f,.057f,.34f,Plain,"D9BD85");B(0,.863f,.026f,.26f,.118f,.225f,Plain,"E2C38B");
        B(0,.831f,.025f,.264f,.039f,.231f,Plain,"6C9998");B(0,.452f,.154f,.24f,.28f,.118f,Boards);o-=at;
    }
}
