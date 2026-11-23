using System;
using System.Collections.Generic;
using UnityEngine;

// Runs against disposable worlds. Every built example uses the same place, survey and stock rules as the game.
public static class VoxelVerification
{
    static void Check(bool result,string action){if(!result)throw new InvalidOperationException("Voxel: "+action);}
    static VoxelWorkshop Resume(VoxelCatalog c,VoxelWorkshop w)=>new VoxelWorkshop(c,JsonUtility.FromJson<VoxelRecord>(JsonUtility.ToJson(w.State)));
    static void Survey(VoxelWorkshop w)
    {
        for(int i=0;i<2;i++){w.Focus(w.Landmark(i));Check(w.Collect()&&w.Found(w.Island,i),"名所発見");}
    }
    static void Parts(VoxelWorkshop w,bool repair=false)
    {
        int safety=w.Plan.parts.Length*4+4;
        while(w.Next(out var next)&&safety-->0)
        {
            int i=w.Catalog.Material(next.materialId);
            if(!w.Unlocked(i))
            {
                var m=w.Catalog.materials[i];int origin=w.Island;
                Check(w.Travel(m.island),"資材の採取島");
                w.Focus(m.site>=0?w.Landmark(m.site):w.Dock);Check(w.Collect(),"調査による資材補給");
                Check(w.Travel(origin),"設計台への帰港");w.Focus(w.Anchor);continue;
            }
            if(w.Stock(i)==0&&!w.State.creative){w.Focus(w.Dock);Check(w.Collect(),"在庫補給");w.Focus(w.Anchor);continue;}
            if(repair&&w.At(next.cell)!=null){Check(w.Remove(next.cell),"既設部位を取り外す");continue;}
            Check(w.Place(next.cell,next.materialId,next.turns),w.Plan.id+" 部位 "+next.materialId+" "+next.cell);
        }
        Check(safety>=0&&w.Matched==w.Plan.parts.Length,"全ての部位が一致 "+w.Plan.id);
    }
    public static VoxelWorkshop Populated(VoxelCatalog c,int turns=0)
    {
        var w=new VoxelWorkshop(c);
        for(int i=0;i<c.plans.Length;i++)
        {
            Check(w.Select(i),"設計を選ぶ "+i);Survey(w);w.Focus(w.Anchor);
            for(int n=0;n<turns;n++)w.RotatePlan();
            Parts(w);Check(w.CanSubmit && w.Submit()&&w.HasStamp(i),"出品 "+c.plans[i].id);
            Check(!w.Submit(),"二重出品を拒否");
        }
        return w;
    }
    static void GridAndHistory(VoxelCatalog c)
    {
        var w=new VoxelWorkshop(c);int stockId=c.Material("wood");string wood=c.materials[stockId].id;
        var p=new Vector3Int(-3,0,-3);int before=w.Stock(stockId);
        Check(!w.Travel(1)&&!w.CanSelect(c.plans.Length),"渡航と巡回の初期条件");
        Check(!w.Place(p+Vector3Int.up*3,wood,0)&&!w.Place(new Vector3Int(80,0,80),wood,0)
            &&!w.Place(w.Dock,wood,0)&&!w.Place(w.Landmark(0),wood,0)&&!w.Place(p+Vector3Int.down,wood,0),"範囲、空中、予約区画、地中を拒否");
        Check(!w.Remove(p+Vector3Int.down)&&w.Stock(stockId)==before,"地面の保護");
        Check(w.Raycast(new Ray(new Vector3(-2.6f,8,-2.6f),Vector3.down),out var hit,out var face)&&hit==p+Vector3Int.down&&face==Vector3Int.up,"負座標の上面");
        Check(w.Place(hit+face,wood,0)&&w.Stock(stockId)==before-1,"上面の隣に設置");
        Check(!w.Place(p,wood,0)&&!w.Place(p,"未登録",0)&&w.Stock(stockId)==before-1,"重複と無効な資材の原子性");
        Check(w.Raycast(new Ray(new Vector3(-7,.5f,-2.5f),Vector3.right),out hit,out face)&&hit==p&&face==Vector3Int.left,"西面の判定");
        Check(w.Raycast(new Ray(new Vector3(1,.5f,-2.5f),Vector3.left),out hit,out face)&&hit==p&&face==Vector3Int.right,"東面の判定");
        Check(w.Raycast(new Ray(new Vector3(-2.5f,.5f,-7),Vector3.forward),out hit,out face)&&hit==p&&face==Vector3Int.back,"南面の判定");
        Check(w.Raycast(new Ray(new Vector3(-2.5f,.5f,2),Vector3.back),out hit,out face)&&hit==p&&face==Vector3Int.forward,"北面の判定");
        Check(!w.Raycast(new Ray(new Vector3(30,8,30),Vector3.up),out _,out _),"空への光線");
        Check(w.Place(p+Vector3Int.left,wood,0)&&w.Place(p+Vector3Int.up,wood,0)&&w.Remove(p),"隣接・上積み・回収");
        int balance=w.Stock(stockId);
        Check(w.History(false)&&w.At(p)!=null&&w.Stock(stockId)==balance-1,"回収を戻す");
        w=Resume(c,w);Check(w.History(true)&&w.At(p)==null&&w.Stock(stockId)==balance,"再開後のやり直し");
        Check(w.History(false),"分岐の前に回収を戻す");
        Check(w.Place(p+Vector3Int.right,wood,0)&&!w.History(true),"新しい編集でやり直しを更新");
        var budget=new VoxelWorkshop(c);
        for(int x=-5;x<5;x++)for(int z=-4;z<-1;z++)budget.Place(new Vector3Int(x,0,z),wood,0);
        Check(budget.Stock(stockId)==0&&!budget.Place(new Vector3Int(5,0,4),wood,0),"在庫切れ");
        budget.Focus(budget.Dock);Check(budget.Collect()&&budget.Stock(stockId)>0,"補給で復帰");
        int supplied=budget.Stock(stockId);
        Check(budget.History(false)&&budget.Stock(stockId)==supplied+1,"補給後の取り消しで新在庫を保持");
    }
    static void FacingAndCatalog(VoxelCatalog c)
    {
        var w=new VoxelWorkshop(c);Check(w.Select(c.Plan("bench")),"向きのある設計を選ぶ");
        VoxelGuideCell directional=default;
        foreach(var part in w.GuideCells.ToArray())
        {
            if(c.materials[c.Material(part.materialId)].directional && directional.materialId==null)
            {
                directional=part;Check(w.Place(part.cell,part.materialId,part.turns+1),"向き違いの仮置き");
            }
            else Check(w.Place(part.cell,part.materialId,part.turns),"設計の他部位");
        }
        Check(directional.materialId!=null&&!w.CanSubmit&&w.Matched==w.Plan.parts.Length-1,"向き違いを識別");
        Check(w.Remove(directional.cell)&&w.Place(directional.cell,directional.materialId,directional.turns)&&w.Submit(),"向き修正で完工");
        int olderUndo=w.HistoryCount(false);Check(w.Travel(1)&&!w.History(false),"別島の取り消しを隔離");
        Check(w.Place(new Vector3Int(-2,0,-2),"wood",0)&&w.Travel(0)&&w.HistoryCount(false)==olderUndo,"島の編集履歴");
        Check(w.History(false)&&w.HasStamp(c.Plan("bench")),"印は撤去後も継続");
        var edited=JsonUtility.FromJson<VoxelCatalog>(JsonUtility.ToJson(c));
        var more=new List<VoxelMaterial>(edited.materials);more.Reverse();
        more.Add(new VoxelMaterial{id="pearl",name="真珠色",note="拡張素材",color="#FFFFFF",site=-1,supply=4,initial=3});
        edited.materials=more.ToArray();Array.Reverse(edited.plans);edited.Validate();
        var firstDay=new VoxelWorkshop(edited);Check(firstDay.Plan.island==0&&firstDay.Unlocked(edited.Material(firstDay.State.selectedId)),"並べ替えた工房の最初の依頼");
        var reopened=Resume(edited,w);
        Check(reopened.HasStamp(edited.Plan("bench"))&&reopened.ActivePlan==edited.Plan("bench")&&reopened.Stock(edited.Material("pearl"))==3,"IDで再結合、新しい資材");
        Check(reopened.History(true)&&reopened.At(directional.cell).materialId==directional.materialId,"素材順序変更後の履歴");
        Check(reopened.Place(new Vector3Int(0,0,0),"pearl",0),"追加された素材で建築");
        var retired=Resume(c,reopened);var cell=new Vector3Int(0,0,0);
        Check(retired.At(cell)!=null&&!retired.Place(cell,"wood",0)&&retired.Remove(cell)&&retired.History(false),"削除済み素材を保存、回収、復元");
        Check(Resume(edited,retired).At(cell).materialId=="pearl","復帰した素材ID");
    }
    public static VoxelWorkshop Run(VoxelCatalog c)
    {
        GridAndHistory(c);FacingAndCatalog(c);
        var w=Populated(c);Check(w.Stamps==c.plans.Length&&w.CreativeAvailable&&w.State.discoveries==(1<<(VoxelGame.IslandNames.Length*2))-1,"三島・全名所・全依頼");
        for(int turns=1;turns<4;turns++)Check(Populated(c,turns).CreativeAvailable,"90度ずつ回した六つの設計");
        w=Resume(c,w);
        Check(w.CreativeAvailable&&w.CanSelect(c.plans.Length)&&w.Select(c.plans.Length)&&w.State.planTurn==1,"巡回設計の別地点・別方角");
        Parts(w,true);Check(w.TourBuilt==w.Plan.parts.Length&&w.State.tourEdits==w.Plan.parts.Length,"全施工かつ最短手数");
        var stale=Resume(c,w);
        var newEdition=JsonUtility.FromJson<VoxelCatalog>(JsonUtility.ToJson(c));
        Array.Reverse(newEdition.plans);var plans=new List<VoxelPlan>(newEdition.plans);
        plans.Add(new VoxelPlan{id="extra_stool",name="旅の椅子",island=0,anchorX=4,anchorZ=-2,parts=new[]{new VoxelPart{materialId="wood"}}});
        newEdition.plans=plans.ToArray();newEdition.Validate();
        var expanded=Resume(newEdition,w);
        Check(expanded.Plan.id==w.Plan.id&&expanded.TourBuilt==w.TourBuilt&&expanded.CreativeAvailable&&expanded.Submit()&&
            expanded.BestEdits(newEdition.Plan(w.Plan.id))==w.State.tourEdits,"工房拡張後も獲得した権利・受注した巡回・対象別記録を維持");
        Check(stale.Select(0)&&stale.Select(c.plans.Length)&&stale.TourBuilt==0&&stale.CanSubmit,"既存建築の出品と手数を区別");
        Check(stale.Submit()&&stale.BestEdits(0)==0,"出来合いの建築は記録対象外");
        w=Resume(c,w);int ideal=w.Plan.parts.Length;
        Check(w.Submit()&&w.State.repeats==1&&w.BestEdits(0)==ideal,"再開した巡回依頼で最短手数を保存");
        Check(w.Select(c.plans.Length)&&w.RepeatPlan==1&&w.State.planTurn==2,"次回の巡回依頼");
        Parts(w,true);Check(w.Submit()&&w.State.repeats==2,"連続巡回の完工");
        var free=new Vector3Int(5,0,-4);int i=c.Material("wood"),before=w.Stock(i);w.State.creative=true;
        Check(w.Place(free,"wood",0)&&w.Stock(i)==before,"自由建築の在庫");
        w.State.creative=false;Check(w.Remove(free)&&w.Stock(i)==before&&w.History(false)&&w.Stock(i)==before,"通常へ戻った際に無料の部材を重複返却しない");
        var completed=Resume(c,w);Check(completed.State.repeats==2&&completed.At(free)!=null&&completed.BestEdits(0)==ideal,"巡回・最短手数・自由建築を保存");
        Debug.Log("VOXEL_RULES_OK 12 plans x 4 bearings, 6 islands, 12 discoveries, 2 deliveries, stock, undo, catalog migrations, ray faces");
        return completed;
    }
}
