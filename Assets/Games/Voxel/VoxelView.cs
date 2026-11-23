using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static VoxelFabric;

/// <summary>Draws the same world grid that the construction controller raycasts.</summary>
public sealed class VoxelView : MonoBehaviour
{
    struct Piece { public string id;public int turn; }
    sealed class Chunk { public MeshFilter filter,pane;public Mesh mesh,paneMesh; }
    readonly Dictionary<Vector3Int,Piece> previous=new Dictionary<Vector3Int,Piece>(), next=new Dictionary<Vector3Int,Piece>();
    readonly Dictionary<Vector3Int,string> occupied=new Dictionary<Vector3Int,string>();
    readonly HashSet<Vector2Int> covered=new HashSet<Vector2Int>(), newCover=new HashSet<Vector2Int>();
    readonly Dictionary<Vector2Int,Chunk> chunks=new Dictionary<Vector2Int,Chunk>();
    readonly HashSet<Vector2Int> dirty=new HashSet<Vector2Int>();
    readonly Dictionary<string,string> colors=new Dictionary<string,string>();
    readonly List<Mesh> stock=new List<Mesh>();
    Transform stage,rotor,banks;
    MeshFilter ground,scenery,rim,falls,ghost,hit,guide,clouds;
    Mesh groundMesh,sceneryMesh,rimMesh,fallsMesh,ghostMesh,guideMesh;
    Material blocks,preview,faceInk,guideInk,water,glass,cloudInk,sky,formerSky;
    Camera lens;Light sunlight;
    int island=-1,version=int.MinValue,guideHash=int.MinValue,found=-1;
    string ghostKey;int ghostTurn=-1;
    public void Render(VoxelGame g)
    {
        if(!stage)Setup();
        Frame(g);
        if(island!=g.Island){
            island=g.Island;version=int.MinValue;guideHash=int.MinValue;found=-1;
            foreach(var pair in chunks){Drop(pair.Value.mesh);Drop(pair.Value.paneMesh);Drop(pair.Value.filter.gameObject);}
            chunks.Clear();previous.Clear();colors.Clear();
            foreach(var material in g.Materials)colors[material.id]=material.color;
            Replace(ground,ref groundMesh,"Sculpted island",m=>VoxelBlocksArt.Terrain(m,g));
            covered.Clear();Drop(rimMesh);rimMesh=null;
            ColorOfBiome();BuildFalls(g);rotor.gameObject.SetActive(island==0);
        }
        int discoveries=(g.Found(0)?1:0)|(g.Found(1)?2:0);
        if(found!=discoveries){found=discoveries;Replace(scenery,ref sceneryMesh,"Island monuments and outlying hamlets",m=>new VoxelSkyline(m,island).Make(g));}
        if(version!=g.WorldVersion){version=g.WorldVersion;UpdateChunks(g);}
        rotor.localRotation=Quaternion.Euler(0,0,Time.time*11);
        banks.localPosition=V(Mathf.Sin(Time.time*.029f)*.8f,0,Mathf.Cos(Time.time*.027f)*.53f);
        UpdateGhost(g);
    }
    void Setup()
    {
        stage=new GameObject("Sky garden atelier").transform;stage.SetParent(transform,false);
        Material Mat(string id)=>new Material(Resources.Load<Shader>("Voxel/Art/"+id)){name=id};
        blocks=Mat("BlockPrint");blocks.SetTexture("_Atlas",Resources.Load<Texture2D>("Voxel/Art/BlockFaces"));
        blocks.SetTexture("_EastAtlas",Resources.Load<Texture2D>("Voxel/Art/EastFaces"));
        preview=Mat("BuildOverlay");faceInk=Mat("BuildOverlay");guideInk=Mat("BuildOverlay");
        faceInk.SetColor("_Ink",ColorOf("FFF1A6"));faceInk.SetFloat("_Fill",.12f);faceInk.SetFloat("_Edge",.98f);
        guideInk.SetColor("_Ink",ColorOf("B6ECF3"));guideInk.SetFloat("_Fill",.038f);guideInk.SetFloat("_Edge",.27f);
        water=Mat("WaterRibbon");glass=Mat("Window");sky=Mat("CloudSea");formerSky=RenderSettings.skybox;RenderSettings.skybox=sky;
        lens=Camera.main;if(!lens){lens=new GameObject("Workshop camera").AddComponent<Camera>();lens.transform.SetParent(stage,false);lens.tag="MainCamera";}
        lens.clearFlags=CameraClearFlags.Skybox;lens.orthographic=false;lens.fieldOfView=46;lens.nearClipPlane=.12f;lens.farClipPlane=160;lens.allowHDR=false;
        if(!FindAnyObjectByType<AudioListener>())lens.gameObject.AddComponent<AudioListener>();
        sunlight=new GameObject("Island sunlight").AddComponent<Light>();sunlight.transform.SetParent(stage,false);
        sunlight.type=LightType.Directional;sunlight.transform.rotation=Quaternion.LookRotation(V(.55f,-.91f,.38f));
        sunlight.intensity=1.13f;sunlight.shadows=LightShadows.Soft;sunlight.shadowStrength=.78f;sunlight.shadowNormalBias=.19f;
        ground=Draw("Editable island terrain",blocks);scenery=Draw("Landmarks and surrounding islets",blocks);rim=Draw("Coastal planting",blocks);
        falls=Draw("Water streaming to the cloud sea",water);ghost=Draw("Selected construction piece",preview);
        hit=Draw("Contact face",faceInk);guide=Draw("Blueprint lattice",guideInk);
        var pane=new VoxelFabric();pane.Ink(Plain);pane.Face(V(-.488f,0,-.488f),V(-.488f,0,.488f),V(.488f,0,.488f),V(.488f,0,-.488f));
        hit.sharedMesh=Keep(pane.Build("Block face perimeter"));
        var mill=new VoxelFabric();new VoxelSkyline(mill,0).Rotor();
        rotor=Draw("Windmill sails",blocks).transform;rotor.GetComponent<MeshFilter>().sharedMesh=Keep(mill.Build("Linen sails"));rotor.localPosition=VoxelSkyline.RotorPosition;
        cloudInk=Mat("SkyPuffs");banks=new GameObject("Drifting cloud banks").transform;banks.SetParent(stage,false);
        clouds=Draw("Cloud islands",cloudInk,banks);clouds.sharedMesh=Keep(MakeClouds());
    }
    void Frame(VoxelGame g)
    {
        Quaternion rotation=Quaternion.Euler(g.Pitch,g.Yaw,0);
        lens.transform.SetPositionAndRotation(g.Pivot-rotation*Vector3.forward*g.Zoom,rotation);
    }
    void ColorOfBiome()
    {
        string[] high={"5DABC9","7D98B9","8297C7","A8AFD9","67AEBB","575780"},horizon={"F4E5C6","F8D4AE","DFE0EF","FBE1D8","CDEBDC","E7B99D"},deep={"98C6CB","D5B6A0","9DBBD8","C8ADBC","89B9C4","988DA8"};
        sky.SetColor("_High",ColorOf(high[island]));sky.SetColor("_Horizon",ColorOf(horizon[island]));sky.SetColor("_Deep",ColorOf(deep[island]));
        blocks.SetColor("_Air",ColorOf(horizon[island]));water.SetColor("_Ink",ColorOf(island==1?"A8C7BC":island==2?"9BBBDC":"6DBBC6"));
        cloudInk.SetColor("_Crown",ColorOf(island==2?"F1F0FA":"F8F1DA"));cloudInk.SetColor("_ShadowCloud",ColorOf(island==1?"D7BEAF":island==2?"B6C9DE":"AFCDD0"));
    }
    MeshFilter Draw(string name,Material material,Transform parent=null)
    {
        var node=new GameObject(name);node.transform.SetParent(parent?parent:stage,false);
        var mf=node.AddComponent<MeshFilter>();var mr=node.AddComponent<MeshRenderer>();mr.sharedMaterial=material;
        mr.lightProbeUsage=LightProbeUsage.Off;mr.reflectionProbeUsage=ReflectionProbeUsage.Off;
        if(material!=blocks){mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;}
        return mf;
    }
    Mesh Keep(Mesh source){stock.Add(source);return source;}
    static void Drop(UnityEngine.Object obj){if(!obj)return;if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
    static void Replace(MeshFilter filter,ref Mesh old,string name,Action<VoxelFabric> build)
    {var m=new VoxelFabric();build(m);var fresh=m.Build(name);filter.sharedMesh=fresh;Drop(old);old=fresh;}
    static Vector2Int Bucket(Vector3Int p)=>new Vector2Int(Mathf.FloorToInt(p.x*.20f),Mathf.FloorToInt(p.z*.20f));
    void Mark(Vector3Int p){dirty.Add(Bucket(p));foreach(var d in VoxelBlocksArt.Dir)dirty.Add(Bucket(p+d));}
    void UpdateChunks(VoxelGame g)
    {
        dirty.Clear();next.Clear();occupied.Clear();newCover.Clear();
        foreach(var b in g.Blocks){
            if(b.island!=island)continue;var p=new Vector3Int(b.x,b.y,b.z);
            var value=new Piece{id=b.materialId,turn=b.turns};next[p]=value;occupied[p]=value.id;
            if(p.y==g.GroundTop(p.x,p.z))newCover.Add(new Vector2Int(p.x,p.z));
            if(!previous.TryGetValue(p,out var was)||was.id!=value.id||was.turn!=value.turn)Mark(p);
        }
        foreach(var was in previous)if(!next.ContainsKey(was.Key))Mark(was.Key);
        previous.Clear();foreach(var pair in next)previous.Add(pair.Key,pair.Value);
        if(!rimMesh||!covered.SetEquals(newCover)){
            covered.Clear();covered.UnionWith(newCover);Replace(rim,ref rimMesh,"Island flowers, seed heads and ice crystals",m=>VoxelRim.Build(m,g,covered));}
        foreach(var address in dirty){
            var art=new VoxelFabric();var panes=new VoxelFabric();bool any=false,hasGlass=false;
            foreach(var piece in next){
                if(Bucket(piece.Key)!=address)continue;any=true;colors.TryGetValue(piece.Value.id,out var tint);
                VoxelBlocksArt.Build(art,piece.Key,piece.Value.id,piece.Value.turn,occupied,tint);
                if(VoxelBlocksArt.IsPanel(piece.Value.id)){
                    bool icy=piece.Value.id=="frost";hasGlass=true;panes.Ink(Plain,icy?"BED9EC":"88CBCC",icy?1:0);
                    panes.Box((Vector3)piece.Key+Vector3.one*.5f,Vector3.one*.91f,Quaternion.identity);}
            }
            if(!chunks.TryGetValue(address,out var chunk)){
                if(!any)continue;chunk=new Chunk{filter=Draw("Workshop chunk "+address.x+","+address.y,blocks)};chunks.Add(address,chunk);}
            Drop(chunk.mesh);chunk.mesh=any?art.Build("Placed blocks"):null;chunk.filter.sharedMesh=chunk.mesh;
            if(hasGlass&&!chunk.pane)chunk.pane=Draw("Glass",glass,chunk.filter.transform);
            if(chunk.pane){Drop(chunk.paneMesh);chunk.paneMesh=hasGlass?panes.Build("Window panes"):null;chunk.pane.sharedMesh=chunk.paneMesh;}
        }
    }
    void UpdateGhost(VoxelGame g)
    {
        bool framing=VoxelPostcard.Framing;
        bool select=g.HasTarget&&!framing;
        ghost.gameObject.SetActive(select);hit.gameObject.SetActive(select);guide.gameObject.SetActive(g.Guide&&!framing);
        if(select){
            if(ghostKey!=g.SelectedId||ghostTurn!=g.Turns){
                ghostKey=g.SelectedId;ghostTurn=g.Turns;
                Replace(ghost,ref ghostMesh,"Proposed "+ghostKey,m=>VoxelBlocksArt.Build(m,Vector3Int.zero,ghostKey,ghostTurn,null,"FFFFFF"));
            }
            preview.SetColor("_Ink",ColorOf(g.CanPlace?"B2F3DB":"F49881"));preview.SetFloat("_Fill",g.CanPlace?.21f:.10f);
            ghost.transform.localPosition=(Vector3)g.TargetCell+Vector3.one*.012f;ghost.transform.localScale=Vector3.one*.976f;
            var n=(Vector3)g.FaceNormal;if(n.sqrMagnitude<.5f)n=Vector3.up;
            var contact=g.TargetBlock;
            if(!g.PointerMode&&!occupied.ContainsKey(g.TargetCell)){contact=g.TargetCell+Vector3Int.down;n=Vector3.up;}
            hit.transform.localPosition=(Vector3)contact+Vector3.one*.5f+n*.509f;
            hit.transform.localRotation=Quaternion.FromToRotation(Vector3.up,n);
        }
        if(!guide.gameObject.activeSelf)return;
        int hash=193;unchecked{foreach(var c in g.GuideCells){
            hash=(hash*31+c.cell.GetHashCode())*31+(c.matched?1:0);}}
        if(hash==guideHash)return;guideHash=hash;
        Replace(guide,ref guideMesh,"Unfinished blueprint cells",m=>{
            foreach(var c in g.GuideCells){
                if(c.matched)continue;m.Ink(Plain);m.Box((Vector3)c.cell+Vector3.one*.5f,V(.969f,.969f,.969f),Quaternion.identity);}
        });
    }
    void BuildFalls(VoxelGame g)
    {
        Replace(falls,ref fallsMesh,"Mistfall",m=>{
            void cascade(float x,float y,float z,float w,float length){
                m.Ink(Plain);m.Face(V(x,y-length,z),V(x+w,y-length,z),V(x+w,y,z),V(x,y,z));}
            cascade(-12.70f,-.29f,7.07f,.68f,7.3f);cascade(11.86f,-1.29f,7.03f,.48f,5.9f);
            int x=g.Island==1?7:-5;for(int z=-10;z<8;z++){
                int top=g.GroundTop(x,z);if(top<=-900)continue;
                cascade(x+.11f,top+.014f,z-.014f,.70f,6.4f);break;}
        });
    }
    static Mesh MakeClouds()
    {
        int count=30;var v=new Vector3[count*4];var uv=new Vector2[v.Length];var shape=new List<Vector4>(v.Length);var index=new int[count*6];
        for(int j=0;j<count;j++){
            float a=j*2.399f,r=21+j%6*4.7f,y=-6.8f-j%5*1.25f;var at=V(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);int k=j*4,f=j*6;
            for(int n=0;n<4;n++){v[k+n]=at;uv[k+n]=new Vector2(n==1||n==2?1:0,n>1?1:0);shape.Add(new Vector4(7.4f+j%4,2.75f+j%3*.51f,j+2,0));}
            index[f]=k;index[f+1]=k+1;index[f+2]=k+2;index[f+3]=k;index[f+4]=k+2;index[f+5]=k+3;
        }
        var mesh=new Mesh{name="Cutout pixel cloud banks",vertices=v,uv=uv,triangles=index};mesh.SetUVs(1,shape);
        mesh.bounds=new Bounds(V(0,-8,0),V(119,15,119));return mesh;
    }
    void OnDestroy()
    {
        if(RenderSettings.skybox==sky)RenderSettings.skybox=formerSky;
        foreach(var c in chunks.Values){Drop(c.mesh);Drop(c.paneMesh);}foreach(var m in stock)Drop(m);
        Drop(groundMesh);Drop(sceneryMesh);Drop(rimMesh);Drop(fallsMesh);Drop(ghostMesh);Drop(guideMesh);
        Drop(blocks);Drop(preview);Drop(faceInk);Drop(guideInk);Drop(water);Drop(glass);Drop(cloudInk);Drop(sky);
    }
}
