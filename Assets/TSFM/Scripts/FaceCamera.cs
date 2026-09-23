using UnityEngine;
namespace TSFM {
    public class FaceCamera:MonoBehaviour {void LateUpdate(){if(Camera.main!=null)transform.rotation=Camera.main.transform.rotation;}}
}
