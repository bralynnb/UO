using UnityEngine;
namespace TSFM {
    public class MarketActor:MonoBehaviour {
        public Vector3 Target {get;private set;}
        float heading;GameObject bag;bool ownsBag;
        public void Initialize(PlayerState s,bool self) {
            Target=new Vector3(s.x,0,s.z);transform.position=Target;
            if(self)return;
            var label=MarketWorld.Label(s.name,new Vector3(0,2.4f,0),.065f,MarketWorld.Hex("e4efdc"),transform);label.gameObject.AddComponent<FaceCamera>();
        }
        public void Apply(PlayerState s){Target=new Vector3(s.x,0,s.z);heading=s.rotation;ownsBag=s.bag;}
        void Update() {
            transform.position=Vector3.Lerp(transform.position,Target,1-Mathf.Exp(-Time.deltaTime*18));
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,heading,0),Time.deltaTime*12);
            if(ownsBag&&bag==null){bag=MarketWorld.Creature("bag");bag.transform.localScale=Vector3.one*.7f;bag.transform.position=transform.position;}
            if(bag!=null){var follow=transform.position-transform.forward*1.05f+transform.right*.5f;bag.transform.position=Vector3.Lerp(bag.transform.position,follow,Time.deltaTime*4);bag.transform.rotation=transform.rotation;}
        }
        void OnDestroy(){if(bag!=null)Destroy(bag);}
    }
}
