using System.Collections.Generic;
using UnityEngine;
namespace TSFM {
    public static class Pathfinder {
        static Vector2Int Cell(Vector3 p)=>new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.z));
        static bool Safe(Block b,Vector2Int p)=>b.Walkable(p.x,p.y);
        static float Cost(Vector2Int a,Vector2Int b)=>Vector2Int.Distance(a,b);
        public static Queue<Vector3> Find(Block b,Vector3 from,Vector3 to) {
            var route=new Queue<Vector3>();var start=Cell(from);var goal=Cell(to);
            if(!Safe(b,goal))return route;
            if(!Safe(b,start)) {
                float nearest=float.MaxValue;var original=start;
                for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++) {
                    var p=original+new Vector2Int(x,z);var cost=Vector3.Distance(from,new Vector3(p.x,0,p.y));
                    if(Safe(b,p)&&cost<nearest){start=p;nearest=cost;}
                }
                if(nearest==float.MaxValue)return route;
            }
            var open=new List<Vector2Int>{start};var closed=new HashSet<Vector2Int>();
            var parents=new Dictionary<Vector2Int,Vector2Int>();var scores=new Dictionary<Vector2Int,float>{{start,0}};
            while(open.Count>0) {
                int best=0;float lowest=float.MaxValue;
                for(int i=0;i<open.Count;i++){float f=scores[open[i]]+Cost(open[i],goal);if(f<lowest){lowest=f;best=i;}}
                var current=open[best];open.RemoveAt(best);
                if(current==goal) {
                    var reverse=new List<Vector3>();
                    while(current!=start){reverse.Add(new Vector3(current.x,0,current.y));current=parents[current];}
                    reverse.Reverse();foreach(var p in reverse)route.Enqueue(p);return route;
                }
                closed.Add(current);
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++) {
                    if(x==0&&z==0)continue;var next=current+new Vector2Int(x,z);
                    if(closed.Contains(next)||!Safe(b,next))continue;
                    if(x!=0&&z!=0&&(!Safe(b,current+new Vector2Int(x,0))||!Safe(b,current+new Vector2Int(0,z))))continue;
                    float score=scores[current]+Cost(current,next);
                    if(scores.TryGetValue(next,out var old)&&score>=old)continue;
                    scores[next]=score;parents[next]=current;if(!open.Contains(next))open.Add(next);
                }
            }
            return route;
        }
    }
}
