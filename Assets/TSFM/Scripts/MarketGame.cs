using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace TSFM {
    public class MarketGame:MonoBehaviour {
        public Block Block {get;private set;}
        public Profile Profile {get;private set;}
        public string SelfId {get;private set;}
        public bool Joined {get;private set;}
        public MarketHud Hud {get;private set;}
        public Vendor Nearest {get;private set;}
        public Pickup NearestPickup {get;private set;}
        public ThirdPersonCamera Orbit {get;private set;}
        public Vector3 VisualPosition=>actors.TryGetValue(SelfId??"",out var a)?a.transform.position:SelfPosition;
        readonly Dictionary<string,GameObject> pickups=new Dictionary<string,GameObject>();
        public Vector3 SelfPosition=>actors.TryGetValue(SelfId??"",out var actor)?actor.Target:new Vector3(Block.spawnX,0,Block.spawnZ);
        readonly Dictionary<string,MarketActor> actors=new Dictionary<string,MarketActor>();
        MarketConnection connection;Camera cameraView;Queue<Vector3> route=new Queue<Vector3>();
        string guestName="Visitor";int guestColor,sequence;float nextSend,connectStarted;
        bool connecting,focused=true;GameObject destination;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(FindFirstObjectByType<MarketGame>()==null)new GameObject("TSFM").AddComponent<MarketGame>();}
        void Start() {
            Application.targetFrameRate=60;Application.runInBackground=true;
            Block=JsonUtility.FromJson<Block>(Resources.Load<TextAsset>("block").text);
            cameraView=MarketWorld.Build(Block);
            Orbit=cameraView.gameObject.AddComponent<ThirdPersonCamera>();Orbit.Configure(this);
            foreach(var item in Block.pickups)pickups[item.id]=MarketWorld.PickupModel(item);
            connection=gameObject.AddComponent<MarketConnection>();connection.Opened+=OnOpen;connection.Message+=OnMessage;connection.Closed+=OnClosed;
            Hud=gameObject.AddComponent<MarketHud>();Hud.Build(this);
            destination=MarketWorld.Shape("Walking destination",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.7f,.012f,.7f),"c4d8ba");destination.SetActive(false);
            Hud.ShowEntry("Choose a name and step into the market.",PlayerPrefs.GetString("tsfm.name","Visitor"));
        }
        public void Join(string name,int color,bool newGuest=false) {
            if(newGuest){PlayerPrefs.DeleteKey("tsfm.token");PlayerPrefs.Save();}
            guestName=string.IsNullOrWhiteSpace(name)?"Visitor":name.Trim();guestColor=color;
            PlayerPrefs.SetString("tsfm.name",guestName);PlayerPrefs.Save();
            connecting=true;connectStarted=Time.realtimeSinceStartup;Hud.SetEntryStatus("Connecting to the market…");connection.Connect();
        }
        void OnOpen(){sequence=0;connection.Send(new Command{type="join",name=guestName,color=guestColor,token=PlayerPrefs.GetString("tsfm.token","")});}
        void OnClosed(string text) {
            Joined=false;connecting=false;route.Clear();destination.SetActive(false);Hud.ClosePanels();
            Hud.ShowEntry(text,guestName);
        }
        void OnMessage(string json) {
            Envelope message;
            try{message=JsonUtility.FromJson<Envelope>(json);}catch{Hud.Notify("Received an unreadable server message.");return;}
            if(message==null)return;
            switch(message.type) {
                case "welcome":
                    if(message.worldVersion!=Block.version){OnClosed("This client is out of date. Reload the page.");return;}
                    foreach(var actor in actors.Values)Destroy(actor.gameObject);actors.Clear();
                    connecting=false;Joined=true;SelfId=message.id;Profile=message.profile;
                    PlayerPrefs.SetString("tsfm.token",message.token);PlayerPrefs.Save();
                    Orbit.ResetView();RefreshPickups();Hud.HideEntry();Hud.RefreshProfile();Hud.Notify("Welcome. Start at the exchange machine or the work board.");break;
                case "state":
                    if(!Joined||message.players==null)return;
                    var live=new HashSet<string>();
                    foreach(var state in message.players) {
                        live.Add(state.id);
                        if(!actors.TryGetValue(state.id,out var actor)) {
                            var obj=MarketWorld.Avatar(state.name,state.color,state.id==SelfId);actor=obj.AddComponent<MarketActor>();actor.Initialize(state,state.id==SelfId);actors[state.id]=actor;
                        }
                        actor.Apply(state);
                    }
                    var departed=new List<string>();foreach(var pair in actors)if(!live.Contains(pair.Key)){Destroy(pair.Value.gameObject);departed.Add(pair.Key);}
                    foreach(var id in departed)actors.Remove(id);
                    Hud.Population(message.players.Length);break;
                case "profile":Profile=message.profile;RefreshPickups();Hud.RefreshProfile();Hud.Notify(message.text);break;
                case "chat":Hud.AddChat(message.name+": "+message.text);break;
                case "notice":Hud.AddChat(message.text);break;
                case "error":
                    if(!Joined){connecting=false;Hud.SetEntryStatus(message.text);}else Hud.Notify(message.text);break;
            }
        }
        public void Action(Vendor vendor,string action,string item="") {
            if(Joined)connection.Send(new Command{type="action",vendor=vendor.id,action=action,item=item});
        }
        public void Chat(string text){if(Joined&&!string.IsNullOrWhiteSpace(text))connection.Send(new Command{type="chat",text=text});}
        void RefreshPickups() {
            var owned=new HashSet<string>(Profile.collected??new string[0]);
            foreach(var pair in pickups)pair.Value.SetActive(!owned.Contains(pair.Key));
        }
        public void OpenNearest(){
            route.Clear();destination.SetActive(false);
            if(NearestPickup!=null)connection.Send(new Command{type="action",action="collect",item=NearestPickup.id});
            else if(Nearest!=null)Hud.ShowVendor(Nearest);
        }
        public void WalkTo(Vendor vendor){Hud.ClosePanels();route=Pathfinder.Find(Block,SelfPosition,vendor.Position);MarkDestination();}
        void MarkDestination() {
            destination.SetActive(route.Count>0);
            if(route.Count>0){var all=route.ToArray();destination.transform.position=all[all.Length-1]+Vector3.up*.09f;}
        }
        void Update() {
            if(Block==null)return;
            if(connecting&&Time.realtimeSinceStartup-connectStarted>12){connecting=false;Hud.SetEntryStatus("The server did not answer. Check your connection, then try again.");}
            if(!Joined)return;
            var p=SelfPosition;Nearest=null;NearestPickup=null;float best=3;
            foreach(var item in Block.pickups)if(pickups[item.id].activeSelf) {
                float d=Vector3.Distance(p,item.Position);if(d<2.1f&&(NearestPickup==null||d<Vector3.Distance(p,NearestPickup.Position)))NearestPickup=item;
            }
            foreach(var v in Block.vendors){float d=Vector3.Distance(p,v.Position);if(d<best){best=d;Nearest=v;}}
            if(NearestPickup!=null&&Nearest!=null&&Vector3.Distance(p,Nearest.Position)<Vector3.Distance(p,NearestPickup.Position))NearestPickup=null;
            Hud.Interaction(Nearest,NearestPickup);
            if(!Hud.Typing&&Input.GetKeyDown(KeyCode.Slash))Hud.ToggleOptions();
            if(!Hud.Typing&&Input.GetKeyDown(KeyCode.F1))Hud.ToggleOptions();
            if(Input.GetKeyDown(KeyCode.Escape))Hud.ClosePanels();
            Vector2 input=Vector2.zero;
            if(focused&&!Hud.InputBlocked) {
                if(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow))input.x--;
                if(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow))input.x++;
                if(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow))input.y++;
                if(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow))input.y--;
                if(Input.GetKeyDown(KeyCode.E))OpenNearest();
                if(Input.GetKeyDown(KeyCode.I))Hud.ToggleInventory();
                if(Input.GetKeyDown(KeyCode.Return))Hud.FocusChat();
                if(input.sqrMagnitude>.01f){input=Orbit.RelativeInput(input.normalized);route.Clear();destination.SetActive(false);}
                else if(Input.GetMouseButtonDown(0)&&!Input.GetMouseButton(1)&&!EventSystem.current.IsPointerOverGameObject()) {
                    var ray=cameraView.ScreenPointToRay(Input.mousePosition);
                    if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out var distance)) {
                        route=Pathfinder.Find(Block,p,ray.GetPoint(distance));MarkDestination();
                    }
                }
                if(input.sqrMagnitude<.01f&&route.Count>0) {
                    var delta=route.Peek()-p;
                    while(delta.magnitude<.28f&&route.Count>0){route.Dequeue();if(route.Count>0)delta=route.Peek()-p;}
                    if(route.Count>0)input=new Vector2(delta.x,delta.z).normalized;else destination.SetActive(false);
                }
            }
            if(Hud.InputBlocked)input=Vector2.zero;
            if(Time.unscaledTime>=nextSend){connection.Send(new Command{type="input",dx=input.x,dz=input.y,jog=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift),seq=++sequence});nextSend=Time.unscaledTime+.05f;}

        }
        void OnApplicationFocus(bool value){focused=value;if(!value){route.Clear();if(connection!=null)connection.Send(new Command{type="input",dx=0,dz=0,seq=++sequence});}}
    }
}
