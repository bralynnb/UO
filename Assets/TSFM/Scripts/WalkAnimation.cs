using UnityEngine;
namespace TSFM {
    // Small procedural gait for the prototype model; replace with a rig for production assets.
    public class WalkAnimation:MonoBehaviour {
        Transform leftLeg,rightLeg,leftArm,rightArm;Vector3 previous;float phase,weight;
        void Start() {
            previous=transform.position;
            foreach(Transform part in transform) {
                if(part.name=="Boot"){if(part.localPosition.x<0)leftLeg=part;else rightLeg=part;}
                if(part.name=="Sleeve"){if(part.localPosition.x<0)leftArm=part;else rightArm=part;}
            }
        }
        void LateUpdate() {
            float speed=Vector3.Distance(transform.position,previous)/Mathf.Max(Time.deltaTime,.001f);previous=transform.position;
            weight=Mathf.MoveTowards(weight,Mathf.Clamp01(speed),Time.deltaTime*6);
            phase+=Time.deltaTime*Mathf.Clamp(speed,0,4)*4;
            float swing=Mathf.Sin(phase)*23*weight;
            if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(swing,0,0);
            if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(-swing*.7f,0,0);
            if(rightArm!=null)rightArm.localRotation=Quaternion.Euler(swing*.7f,0,0);
        }
    }
}
