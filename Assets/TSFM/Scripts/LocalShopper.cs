using UnityEngine;
namespace TSFM {
    public class LocalShopper:MonoBehaviour {
        Vector3 origin;float phase;
        public void Configure(Vector3 p,int i){origin=p;phase=i*1.8f;}
        void Update(){float t=Time.time*.18f+phase;transform.position=origin+Vector3.right*Mathf.Sin(t)*2;transform.rotation=Quaternion.Euler(0,Mathf.Cos(t)>0?90:-90,0);}
    }
}
