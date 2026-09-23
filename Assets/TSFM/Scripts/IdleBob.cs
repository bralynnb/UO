using UnityEngine;
namespace TSFM {
    public class IdleBob:MonoBehaviour {
        public float amount=.05f;Vector3 origin;float phase;
        void Start(){origin=transform.localPosition;phase=transform.position.x;}
        void Update(){transform.localPosition=origin+Vector3.up*Mathf.Sin(Time.time*1.4f+phase)*amount;}
    }
}
