using UnityEngine;
using UnityEngine.EventSystems;
namespace TSFM {
    // Metre-scale shoulder camera. Obstacle tests use the same footprints as the server.
    public class ThirdPersonCamera:MonoBehaviour {
        MarketGame game;float yaw=70,pitch=24,distance=6;Vector3 smoothTarget;bool ready;
        public float Sensitivity=2.4f;public bool InvertY;
        public float Yaw=>yaw;
        public float Zoom=>distance;
        public void Configure(MarketGame owner){game=owner;}
        public void ResetView(){yaw=70;pitch=24;distance=6;ready=false;}
        public Vector2 RelativeInput(Vector2 input) {
            var rotated=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
            return new Vector2(rotated.x,rotated.z);
        }
        void LateUpdate() {
            if(game==null||!game.Joined)return;
            bool onUI=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
            if(!game.Hud.InputBlocked&&!onUI) {
                distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*.8f,2.2f,16);
                if(Input.GetMouseButton(1)) {
                    yaw+=Input.GetAxisRaw("Mouse X")*Sensitivity;
                    pitch=Mathf.Clamp(pitch+Input.GetAxisRaw("Mouse Y")*Sensitivity*(InvertY?1:-1),8,72);
                }
            }
            var target=game.VisualPosition+Vector3.up*1.45f;
            if(!ready){smoothTarget=target;ready=true;}
            smoothTarget=Vector3.Lerp(smoothTarget,target,1-Mathf.Exp(-Time.deltaTime*12));
            var rotation=Quaternion.Euler(pitch,yaw,0);
            var desired=smoothTarget-rotation*Vector3.forward*distance;
            var delta=desired-smoothTarget;float fraction=1;
            foreach(var o in game.Block.obstacles) {
                float height=o.height;
                if(o.kind=="stall")height=2.9f;
                var box=new Bounds(new Vector3(o.x,height/2,o.z),new Vector3(o.w+.45f,height+.3f,o.d+.45f));
                if(box.IntersectRay(new Ray(smoothTarget,delta.normalized),out var hit)&&hit<distance)
                    fraction=Mathf.Min(fraction,Mathf.Max(.35f,hit-.12f)/distance);
            }
            transform.position=smoothTarget+delta*fraction;transform.rotation=rotation;
        }
    }
}
