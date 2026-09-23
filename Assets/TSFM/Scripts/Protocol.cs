using System;
using UnityEngine;
namespace TSFM {
    [Serializable] public class Obstacle { public string id,kind,color; public float x,z,w,d,height; }
    [Serializable] public class Vendor {
        public string id,name,subtitle,kind,line,action,actionLabel,item,itemLabel;
        public float x,z,propX,propZ,facing; public int price;
        public Vector3 Position => new Vector3(x,0,z);
    }
    [Serializable] public class Block {
        public string id,name; public int version,year;
        public float minX,maxX,minZ,maxZ,spawnX,spawnZ,speed,jogSpeed,radius;
        public Obstacle[] obstacles; public Vendor[] vendors; public Pickup[] pickups;
        public bool Walkable(float x,float z) {
            if(x<minX||x>maxX||z<minZ||z>maxZ)return false;
            foreach(var o in obstacles)if(Mathf.Abs(x-o.x)<o.w/2+radius&&Mathf.Abs(z-o.z)<o.d/2+radius)return false;
            return true;
        }
    }
    [Serializable] public class Pickup { public string id,name,kind; public float x,z; public Vector3 Position=>new Vector3(x,0,z); }
    [Serializable] public class InventoryItem { public string id,name; public int count; }
    [Serializable] public class PlayerState {
        public string id,name; public int color; public float x,z,rotation; public bool bag;
    }
    [Serializable] public class Profile : PlayerState {
        public int tokens,xp,job,deliveries; public long cooldownUntil; public bool exchanged;
        public InventoryItem[] inventory; public string[] collected;
    }
    [Serializable] public class Envelope {
        public string type,id,name,token,text; public int capacity,tick,worldVersion;
        public Profile profile; public PlayerState[] players;
    }
    [Serializable] public class Command {
        public string type,name,token,vendor,action,item,text;
        public float dx,dz; public int seq,color; public bool jog;
    }
}
