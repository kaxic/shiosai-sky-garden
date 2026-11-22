using System;
using System.Collections.Generic;
using UnityEngine;

public enum VoxelMode { Build, Palette, Plans, Travel, Journal }
[Serializable] public sealed class VoxelBlock
{
    public int island,x,y,z,turns;
    public string materialId;
    public bool unlimitedOrigin;
    public Vector3Int Cell => new Vector3Int(x,y,z);
}
[Serializable] public sealed class VoxelEdit { public VoxelBlock block; public bool placed,finite; }
[Serializable] public sealed class VoxelStock { public string id; public int count; }
[Serializable] public sealed class VoxelTourMark {public Vector3Int cell;public string materialId;public int turns;}
[Serializable] public sealed class VoxelBest {public string planId;public int edits;}
public struct VoxelGuideCell { public Vector3Int cell; public string materialId; public int turns; public bool matched; }
[Serializable] public sealed class VoxelRecord
{
    public int version,island,visited=1,discoveries,repeats,turns,planTurn;
    public bool guide=true,creative,repeating,freeEarned;
    public string selectedId,planId,tourPlanId,log;
    public float yaw=22,pitch=40,zoom=19;
    public Vector3 pivot;
    public Vector3Int cursor;
    public List<VoxelBlock> blocks=new List<VoxelBlock>();
    public List<VoxelStock> stocks=new List<VoxelStock>();
    public List<VoxelEdit> undo=new List<VoxelEdit>(),redo=new List<VoxelEdit>();
    public List<string> stamps=new List<string>();
    public int tourEdits;
    public List<VoxelTourMark> tourMarks=new List<VoxelTourMark>();
    public List<VoxelBest> best=new List<VoxelBest>();
}

public sealed class VoxelWorkshop
{
    public readonly VoxelCatalog Catalog;
    public readonly VoxelRecord State;
    public int Version {get;private set;}
    public readonly List<VoxelGuideCell> GuideCells=new List<VoxelGuideCell>();
    readonly Dictionary<Vector3Int,VoxelBlock>[] islands={new Dictionary<Vector3Int,VoxelBlock>(),new Dictionary<Vector3Int,VoxelBlock>(),new Dictionary<Vector3Int,VoxelBlock>()};
    readonly Dictionary<string,VoxelStock> stocks=new Dictionary<string,VoxelStock>(StringComparer.Ordinal);
    static readonly Vector3Int[] neighbors={Vector3Int.down,Vector3Int.up,Vector3Int.left,Vector3Int.right,Vector3Int.forward,Vector3Int.back};
    public int Island=>State.island;
    public int RepeatPlan
    {
        get
        {
            int accepted=State.repeating?Catalog.Plan(State.tourPlanId):-1;
            return accepted>=0?accepted:State.repeats%Catalog.plans.Length;
        }
    }
    public int ActivePlan=>State.repeating?Catalog.plans.Length:Catalog.Plan(State.planId);
    public VoxelPlan Plan=>Catalog.plans[State.repeating?RepeatPlan:ActivePlan];
    public int Stamps {get;private set;}
    public int Matched {get;private set;}
    public Vector3Int Anchor=>State.repeating?Surface(Plan.island,0,-3):Surface(Plan.island,Plan.anchorX,Plan.anchorZ);
    public bool CanSubmit=>Island==Plan.island && Matched==Plan.parts.Length && (State.repeating || !State.stamps.Contains(Plan.id));
    public bool CreativeAvailable=>State.freeEarned;
    public VoxelWorkshop(VoxelCatalog catalog,VoxelRecord record=null)
    {
        Catalog=catalog;State=record??new VoxelRecord();
        bool fresh=State.version==0;
        State.version=1;State.island=Mathf.Clamp(State.island,0,2);
        State.visited|=1|(1<<Island);
        State.repeats=Mathf.Max(0,State.repeats);
        State.stocks??=new List<VoxelStock>(); State.stamps??=new List<string>();
        State.tourMarks??=new List<VoxelTourMark>();State.best??=new List<VoxelBest>();
        State.tourMarks.RemoveAll(m=>m==null);State.best.RemoveAll(b=>b==null||b.edits<=0||string.IsNullOrEmpty(b.planId));
        State.blocks??=new List<VoxelBlock>(); State.undo??=new List<VoxelEdit>();State.redo??=new List<VoxelEdit>();
        State.stocks.RemoveAll(s=>s==null||string.IsNullOrEmpty(s.id)||!stocks.TryAdd(s.id,s));
        foreach(var s in stocks.Values)s.count=Mathf.Max(0,s.count);
        foreach(var m in Catalog.materials)
            if(!stocks.ContainsKey(m.id)){var s=new VoxelStock{id=m.id,count=m.initial};State.stocks.Add(s);stocks.Add(m.id,s);}
        State.blocks.RemoveAll(b=>!ValidBlock(b)||!islands[b.island].TryAdd(b.Cell,b));
        State.undo.RemoveAll(e=>e==null||!ValidBlock(e.block));
        State.redo.RemoveAll(e=>e==null||!ValidBlock(e.block));
        CountStamps();
        if(State.creative||State.repeats>0)State.freeEarned=true;
        if(Catalog.Plan(State.planId)<0)
        {
            var starter=Array.Find(Catalog.plans,p=>p.island==0&&Array.TrueForAll(p.parts,part=>{
                var m=Catalog.materials[Catalog.Material(part.materialId)];return m.island==0&&m.site<0;
            }));
            State.planId=(starter??Array.Find(Catalog.plans,p=>p.island==0)??Catalog.plans[0]).id;
        }
        if(!CreativeAvailable){State.repeating=false;State.creative=false;}
        if(State.repeating&&Catalog.Plan(State.tourPlanId)<0)
        {
            State.tourPlanId=Catalog.plans[RepeatPlan].id;State.tourEdits=0;State.tourMarks.Clear();
        }
        if(Catalog.Material(State.selectedId)<0 || !Unlocked(Catalog.Material(State.selectedId)))
            State.selectedId=Array.Find(Catalog.materials,m=>Unlocked(Catalog.Material(m.id)))?.id??Catalog.materials[0].id;
        State.turns=Turn(State.turns);State.planTurn=Turn(State.planTurn);
        if(fresh)
        {
            State.guide=true;State.cursor=Anchor;State.pivot=Center(Anchor);
            string firstPart=Plan.parts[0].materialId;
            if(Unlocked(Catalog.Material(firstPart)))State.selectedId=firstPart;
            State.log="N で最初の部位を選び、Space で置きましょう。";
        }
        if(float.IsNaN(State.pivot.sqrMagnitude)||float.IsInfinity(State.pivot.sqrMagnitude))State.pivot=Center(Anchor);
        State.zoom=Mathf.Clamp(State.zoom,7,32);State.pitch=Mathf.Clamp(State.pitch,22,76);
        Changed();
    }
    static bool ValidBlock(VoxelBlock b)=>b!=null && b.island>=0 && b.island<3 && !string.IsNullOrEmpty(b.materialId) && InBounds(b.island,b.Cell);
    public static int Turn(int n)=>((n%4)+4)%4;
    public static Vector3 Center(Vector3Int v)=>(Vector3)v+Vector3.one*.5f;
    public static int GroundTop(int island,int x,int z)
    {
        if(x< -11||x>11||z< -9||z>9)return -999;
        if(Math.Abs(x)<=7&&Math.Abs(z)<=5 || x==0&&z==-6)return 0;
        float rim=x*x/115f+z*z/76f;
        float edge=.97f+.07f*Mathf.Sin(x*1.2f+z*.59f+island*2.1f);
        if(rim>edge)return -999;
        return rim>.76f?(island==2 && z>2?1:-1):0;
    }
    public static int GroundBottom(int island,int x,int z)
    {
        int top=GroundTop(island,x,z);
        return top==-999?-999:top-Mathf.Max(2,Mathf.RoundToInt(6f-x*x*.035f-z*z*.05f));
    }
    static Vector3Int Surface(int island,int x,int z)=>new Vector3Int(x,GroundTop(island,x,z),z);
    public Vector3Int Dock=>Surface(Island,0,-6);
    public Vector3Int Landmark(int site)=>site==0?Surface(Island,-7,3):Surface(Island,7,-2);
    public static bool InBounds(int island,Vector3Int p)
    {
        int top=GroundTop(island,p.x,p.z);
        return top!=-999 && p.y>=top && p.y<=8 && !(p.x==0&&p.z==-6 || p.x==-7&&p.z==3 || p.x==7&&p.z==-2);
    }
    public VoxelBlock At(Vector3Int cell)=>islands[Island].TryGetValue(cell,out var b)?b:null;
    public bool Solid(Vector3Int cell)
    {
        if(At(cell)!=null)return true;
        int top=GroundTop(Island,cell.x,cell.z);
        return top!=-999 && cell.y<top && cell.y>=GroundBottom(Island,cell.x,cell.z);
    }
    public bool CanAttach(Vector3Int cell)
    {
        if(!InBounds(Island,cell)||At(cell)!=null)return false;
        foreach(var n in neighbors)if(Solid(cell+n))return true;
        return false;
    }
    public bool HasStamp(int i)=>i>=0&&i<Catalog.plans.Length&&State.stamps.Contains(Catalog.plans[i].id);
    public bool CanTravel(int i)=>i>=0&&i<3&&((State.visited&(1<<i))!=0||Stamps>=(i==1?1:3));
    public bool Found(int island,int site)=>island>=0&&island<3&&site>=0&&site<2&&(State.discoveries&(1<<(island*2+site)))!=0;
    public bool Unlocked(int i)
    {
        if(i<0||i>=Catalog.materials.Length)return false;
        var m=Catalog.materials[i];return State.creative||((State.visited&(1<<m.island))!=0&&(m.site<0||Found(m.island,m.site)));
    }
    public int Stock(int i)=>i<0||i>=Catalog.materials.Length?0:stocks[Catalog.materials[i].id].count;
    public void Focus(Vector3Int cell){State.pivot=Center(cell);State.cursor=cell;}
    public bool Travel(int i)
    {
        if(!CanTravel(i))return false;
        bool first=(State.visited&(1<<i))==0;
        State.island=i;State.visited|=1<<i;Focus(Dock);
        if(first)Supply();
        Changed();return true;
    }
    void Supply()
    {
        for(int i=0;i<Catalog.materials.Length;i++)
            if(Unlocked(i)){var m=Catalog.materials[i];stocks[m.id].count=Math.Max(stocks[m.id].count,m.supply);}
    }
    public int Nearby()
    {
        for(int site=-1;site<2;site++)
        {
            var c=Center(site<0?Dock:Landmark(site));
            var delta=c-State.pivot;
            if(delta.x*delta.x+delta.z*delta.z<6.25f)return site;
        }
        return -2;
    }
    public bool Collect()
    {
        int site=Nearby();if(site==-2)return false;
        if(site>=0)State.discoveries|=1<<(Island*2+site);
        Supply();Changed();return true;
    }
    public bool CanPlace(Vector3Int cell,string id)
    {
        int i=Catalog.Material(id);return Unlocked(i)&&(State.creative||Stock(i)>0)&&CanAttach(cell);
    }
    public bool Place(Vector3Int cell,string id,int turns)
    {
        if(!CanPlace(cell,id))return false;
        var b=new VoxelBlock{island=Island,x=cell.x,y=cell.y,z=cell.z,materialId=id,unlimitedOrigin=State.creative,turns=Catalog.materials[Catalog.Material(id)].directional?Turn(turns):0};
        var e=new VoxelEdit{block=b,placed=true,finite=!State.creative};Commit(e);return true;
    }
    public bool Remove(Vector3Int cell)
    {
        var b=At(cell);if(b==null)return false;
        var e=new VoxelEdit{block=b,placed=false,finite=!b.unlimitedOrigin};Commit(e);return true;
    }
    void Commit(VoxelEdit edit)
    {
        Apply(edit,edit.placed);State.undo.Add(edit);State.redo.RemoveAll(e=>e.block.island==Island);
        if(State.undo.Count>220)State.undo.RemoveAt(0);
        Changed();RecordTourEdit(edit.block,edit.placed);
    }
    void Apply(VoxelEdit e,bool put)
    {
        var b=e.block;var map=islands[b.island];
        if(put){map.Add(b.Cell,b);State.blocks.Add(b);}
        else{var original=map[b.Cell];map.Remove(b.Cell);State.blocks.Remove(original);}
        if(e.finite)
        {
            if(!stocks.TryGetValue(b.materialId,out var s)){s=new VoxelStock{id=b.materialId};stocks.Add(s.id,s);State.stocks.Add(s);}
            s.count+=put?-1:1;
        }
    }
    public int HistoryCount(bool redo)
    {
        int count=0;foreach(var e in redo?State.redo:State.undo)if(e.block.island==Island)count++;
        return count;
    }
    public bool History(bool redo)
    {
        var from=redo?State.redo:State.undo;var to=redo?State.undo:State.redo;
        int i=from.FindLastIndex(e=>e.block.island==Island);if(i<0)return false;
        var e=from[i];var b=e.block;bool put=redo?e.placed:!e.placed;var current=At(b.Cell);
        if(put && (current!=null||e.finite&&(!stocks.TryGetValue(b.materialId,out var s)||s.count<1)))return false;
        if(!put && (current==null||current.materialId!=b.materialId||current.turns!=b.turns))return false;
        Apply(e,put);from.RemoveAt(i);to.Add(e);Changed();RecordTourEdit(b,put);return true;
    }
    public int PlanIsland(int index)=>index==Catalog.plans.Length?Catalog.plans[RepeatPlan].island:index>=0&&index<Catalog.plans.Length?Catalog.plans[index].island:-1;
    public bool CanSelect(int index)=>index==Catalog.plans.Length?CreativeAvailable:CanTravel(PlanIsland(index));
    public bool Select(int index)
    {
        if(!CanSelect(index))return false;
        bool different=index!=ActivePlan;
        if(different&&index==Catalog.plans.Length)State.tourPlanId=Catalog.plans[RepeatPlan].id;
        State.repeating=index==Catalog.plans.Length;
        if(!State.repeating)State.planId=Catalog.plans[index].id;
        if(different)
        {
            State.planTurn=State.repeating?Turn(State.repeats+1):0;
            State.tourEdits=0;State.tourMarks.Clear();
        }
        if(Island!=Plan.island)Travel(Plan.island);
        Focus(Anchor);State.guide=true;Changed();return true;
    }
    public void RotatePlan(){State.planTurn=Turn(State.planTurn+1);State.tourMarks.Clear();Changed();}
    void RecordTourEdit(VoxelBlock block,bool put)
    {
        if(!State.repeating||Island!=Plan.island)return;
        State.tourEdits++;
        State.tourMarks.RemoveAll(m=>m.cell==block.Cell);
        if(!put)return;
        foreach(var p in GuideCells)
            if(p.cell==block.Cell&&p.matched)
            {
                State.tourMarks.Add(new VoxelTourMark{cell=p.cell,materialId=p.materialId,turns=block.turns});return;
            }
    }
    public int TourBuilt
    {
        get
        {
            if(!State.repeating)return 0;
            int count=0;
            foreach(var p in GuideCells)
            {
                if(!p.matched)continue;
                int turns=At(p.cell).turns;
                foreach(var m in State.tourMarks)
                    if(m.cell==p.cell&&m.materialId==p.materialId&&m.turns==turns){count++;break;}
            }
            return count;
        }
    }
    public int BestEdits(int index)
    {
        if(index<0||index>=Catalog.plans.Length)return 0;
        foreach(var b in State.best)if(b.planId==Catalog.plans[index].id)return b.edits;
        return 0;
    }
    public bool Submit()
    {
        if(!CanSubmit)return false;
        if(State.repeating)
        {
            if(TourBuilt==Plan.parts.Length&&State.tourEdits>=TourBuilt)
            {
                var best=State.best.Find(b=>b.planId==Plan.id);
                if(best==null)State.best.Add(new VoxelBest{planId=Plan.id,edits=State.tourEdits});
                else best.edits=Math.Min(best.edits,State.tourEdits);
            }
            State.planId=Plan.id;State.repeats++;State.repeating=false;State.planTurn=0;State.tourPlanId=null;
            State.tourMarks.Clear();State.tourEdits=0;
        }
        else State.stamps.Add(Plan.id);
        Changed();return true;
    }
    public static Vector3Int RotateOffset(VoxelPart p,int turn)
    {
        switch(Turn(turn))
        {
            case 1:return new Vector3Int(p.z,p.y,-p.x);
            case 2:return new Vector3Int(-p.x,p.y,-p.z);
            case 3:return new Vector3Int(-p.z,p.y,p.x);
            default:return new Vector3Int(p.x,p.y,p.z);
        }
    }
    void Changed()
    {
        CountStamps();Version++;GuideCells.Clear();Matched=0;
        if(Island!=Plan.island)return;
        foreach(var p in Plan.parts)
        {
            var cell=Anchor+RotateOffset(p,State.planTurn);var b=At(cell);
            int rot=Turn(p.turns+State.planTurn);int i=Catalog.Material(p.materialId);
            bool matched=b!=null&&b.materialId==p.materialId&&(!Catalog.materials[i].directional||b.turns==rot);
            if(matched)Matched++;
            GuideCells.Add(new VoxelGuideCell{cell=cell,materialId=p.materialId,turns=rot,matched=matched});
        }
    }
    void CountStamps(){Stamps=0;foreach(var p in Catalog.plans)if(State.stamps.Contains(p.id))Stamps++;if(Stamps==Catalog.plans.Length)State.freeEarned=true;}
    public bool Next(out VoxelGuideCell next)
    {
        // Prefer a reachable, unlocked part first. The next pass identifies missing supplies or an occupied cell.
        foreach(var part in GuideCells)if(!part.matched&&CanPlace(part.cell,part.materialId)){next=part;return true;}
        foreach(var part in GuideCells)if(!part.matched){next=part;return true;}
        next=default;return false;
    }
    public bool Raycast(Ray ray,out Vector3Int hit,out Vector3Int normal)
    {
        hit=default;normal=Vector3Int.up;
        Vector3 d=ray.direction.normalized;if(d.sqrMagnitude<.5f)return false;
        var cell=Vector3Int.FloorToInt(ray.origin);
        int sx=Math.Sign(d.x),sy=Math.Sign(d.y),sz=Math.Sign(d.z);
        float dx=sx==0?float.PositiveInfinity:1f/Mathf.Abs(d.x);
        float dy=sy==0?float.PositiveInfinity:1f/Mathf.Abs(d.y);
        float dz=sz==0?float.PositiveInfinity:1f/Mathf.Abs(d.z);
        float tx=FaceTime(ray.origin.x,cell.x,sx,dx),ty=FaceTime(ray.origin.y,cell.y,sy,dy),tz=FaceTime(ray.origin.z,cell.z,sz,dz);
        for(int steps=0;steps<200;steps++)
        {
            if(Solid(cell)){hit=cell;return true;}
            if(Mathf.Min(tx,Mathf.Min(ty,tz))>85f)return false;
            if(tx<=ty&&tx<=tz){cell.x+=sx;tx+=dx;normal=new Vector3Int(-sx,0,0);}
            else if(ty<=tz){cell.y+=sy;ty+=dy;normal=new Vector3Int(0,-sy,0);}
            else{cell.z+=sz;tz+=dz;normal=new Vector3Int(0,0,-sz);}
        }
        return false;
    }
    static float FaceTime(float origin,int cell,int step,float delta)=>step==0?float.PositiveInfinity:(step>0?(cell+1-origin):(origin-cell))*delta;
}
