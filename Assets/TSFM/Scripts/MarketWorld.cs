using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace TSFM {
    public static class MarketWorld {
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static Transform root;
        public static readonly Color[] Coats={Hex("72aaa0"),Hex("b57863"),Hex("7c91b3"),Hex("a88cae"),Hex("889867"),Hex("d0bdb2")};
        public static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
        public static Material Mat(string color,bool glow=false) {
            var key=color+(glow?"lit":"");
            if(materials.TryGetValue(key,out var old)&&old!=null)return old;
            var source=Resources.Load<Material>("MarketBase");
            var mat=source!=null?new Material(source):new Material(Shader.Find("Standard"));
            mat.color=Hex(color);mat.SetFloat("_Glossiness",.12f);
            if(glow){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Hex(color)*.7f);}
            materials[key]=mat;return mat;
        }
        public static GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 scale,string color,Transform parent=null) {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent==null?root:parent,false);
            obj.transform.localPosition=p;obj.transform.localScale=scale;
            obj.GetComponent<Renderer>().sharedMaterial=Mat(color);
            var collider=obj.GetComponent<Collider>();if(collider!=null)Object.Destroy(collider);
            return obj;
        }
        static GameObject Box(string n,Vector3 p,Vector3 s,string c,Transform parent=null)=>Shape(n,PrimitiveType.Cube,p,s,c,parent);
        public static TextMesh Label(string words,Vector3 p,float size,Color color,Transform parent=null) {
            var obj=new GameObject("Sign - "+words);obj.transform.SetParent(parent==null?root:parent,false);obj.transform.localPosition=p;
            var text=obj.AddComponent<TextMesh>();text.text=words;text.characterSize=size;text.fontSize=64;text.anchor=TextAnchor.MiddleCenter;
            text.alignment=TextAlignment.Center;text.color=color;text.richText=false;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            obj.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            return text;
        }
        public static Camera Build(Block block) {
            materials.Clear();root=new GameObject("NYC 1985 - metre scale city block").transform;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Hex("a2aab0");
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogStartDistance=90;RenderSettings.fogEndDistance=190;RenderSettings.fogColor=Hex("89959e");
            var sun=new GameObject("Neutral afternoon daylight").AddComponent<Light>();
            sun.type=LightType.Directional;sun.color=Hex("edf3ff");sun.intensity=1.1f;
            sun.transform.rotation=Quaternion.Euler(48,-32,0);sun.shadows=LightShadows.Soft;
            var camera=Camera.main;if(camera==null)camera=new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag="MainCamera";camera.backgroundColor=Hex("89959e");camera.clearFlags=CameraClearFlags.SolidColor;
            camera.fieldOfView=60;camera.nearClipPlane=.08f;camera.farClipPlane=220;
            camera.transform.position=new Vector3(-49,5,-29);camera.transform.LookAt(new Vector3(-43,1,-20));
            if(camera.GetComponent<AudioListener>()==null)camera.gameObject.AddComponent<AudioListener>();
            Box("Asphalt",new Vector3(0,-.18f,0),new Vector3(150,.3f,112),"383d40");
            Box("Block sidewalk",new Vector3(0,-.065f,0),new Vector3(85,.13f,45),"979994");
            for(int x=-42;x<=42;x+=3)Box("Sidewalk seam",new Vector3(x,.005f,0),new Vector3(.024f,.009f,45),"737b79");
            for(int z=-21;z<=21;z+=3)Box("Sidewalk seam",new Vector3(0,.005f,z),new Vector3(85,.009f,.025f),"737b79");
            foreach(int side in new[]{-1,1}) {
                Box("Stone curb",new Vector3(0,0,side*22.5f),new Vector3(85,.16f,.2f),"b1b5af");
                Box("Stone curb",new Vector3(side*42.5f,0,0),new Vector3(.2f,.16f,45),"b1b5af");
                for(int x=-50;x<=50;x+=8)Box("Faded lane stripe",new Vector3(x,-.019f,side*30),new Vector3(3,.008f,.1f),"c5b986");
                for(int z=-24;z<=24;z+=8)Box("Faded lane stripe",new Vector3(side*50,-.019f,z),new Vector3(.1f,.008f,3),"c5b986");
                for(int x=-36;x<=36;x+=11)Building(x,side*45,14+((x+36)%4)*3,"71625b",10.8f,12);
                for(int z=-25;z<=25;z+=10)Building(side*67,z,19,"675d56",12,9.8f);
            }
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1}) {
                for(int i=0;i<7;i++) {
                    Box("Crosswalk paint",new Vector3(end*46.5f,-.01f,side*(24+i*1.2f)),new Vector3(3,.012f,.6f),"c7c9bc");
                    Box("Crosswalk paint",new Vector3(end*(44+i*1.6f),-.01f,side*20),new Vector3(.8f,.012f,3),"c7c9bc");
                }
            }
            foreach(var o in block.obstacles) {
                if(o.kind=="building")Building(o.x,o.z,o.height,o.color,o.w,o.d);
                else if(o.kind=="car")Car(o);
                else if(o.kind=="barrier")Barrier(o);
                else if(o.kind=="hydrant")Hydrant(o.x,o.z);
                else if(o.kind=="prop")Prop(o);
            }
            foreach(var vendor in block.vendors)BuildVendor(vendor);
            foreach(int side in new[]{-1,1})for(int x=-39;x<=39;x+=13) {
                Shape("Street light",PrimitiveType.Cylinder,new Vector3(x,2.9f,side*22),new Vector3(.13f,2.9f,.13f),"454f4f");
                Box("Cobra-head lamp arm",new Vector3(x,5.75f,side*23),new Vector3(.1f,.1f,2),"6e7778");
                Box("Cobra-head lamp",new Vector3(x,5.7f,side*24),new Vector3(.5f,.16f,.75f),"bcc2b8");
            }
            for(int i=0;i<7;i++) {
                Bird(new Vector3(-33+i*11,16+(i%3)*3,-15.8f));
            }
            // Local ambience has no economy state; shared player state comes from the server.
            for(int i=0;i<12;i++) {
                var shopper=i%4==0?Creature("robot"):Avatar("Market shopper",i%6,false);
                shopper.transform.position=new Vector3(-34+(i%6)*13,0,i<6?-24:24);
                shopper.AddComponent<LocalShopper>().Configure(shopper.transform.position,i);
            }
            for(int i=0;i<14;i++) {
                float x=-35+i*5;float z=i%2==0?-25:25;
                Shape("Manhole",PrimitiveType.Cylinder,new Vector3(x,-.012f,z),new Vector3(.65f,.008f,.65f),"333839");
                for(int k=0;k<4;k++)Box("Drain slot",new Vector3(x-.21f+k*.14f,-.002f,z),new Vector3(.03f,.003f,.46f),"1e282c");
            }
            var sign=Label("THE STRANGEST FLEA MARKET",new Vector3(-20,4,-16.18f),.10f,Hex("e4e1ca"));
            sign.transform.rotation=Quaternion.identity;
            Label("NEW YORK  /  1985",new Vector3(-20,3.45f,-16.19f),.075f,Hex("c8cdc2"));
            var fixedObjects=new List<GameObject>();
            foreach(var mesh in root.GetComponentsInChildren<MeshRenderer>())
                if(mesh.GetComponent<MeshFilter>()!=null&&mesh.GetComponent<TextMesh>()==null&&mesh.GetComponentInParent<IdleBob>()==null&&mesh.GetComponentInParent<FaceCamera>()==null)
                    fixedObjects.Add(mesh.gameObject);
            StaticBatchingUtility.Combine(fixedObjects.ToArray(),root.gameObject);
            return camera;
        }
        static void Building(float x,float z,float height,string color,float width,float depth) {
            var b=new GameObject("Brick building").transform;b.SetParent(root);b.position=new Vector3(x,0,z);
            Box("Masonry",new Vector3(0,height/2,0),new Vector3(width,height,depth),color,b);
            Box("Cornice",new Vector3(0,height,0),new Vector3(width+.35f,.35f,depth+.35f),"a19b90",b);
            foreach(int side in new[]{-1,1}) {
                float face=side*(depth/2+.03f);
                for(float y=.5f;y<height;y+=.55f)Box("Mortar course",new Vector3(0,y,face),new Vector3(width,.018f,.018f),"645c55",b);
                for(float y=5;y<height-1;y+=3)for(float wx=-width/2+1.8f;wx<width/2-1;wx+=3) {
                    Box("Window stone surround",new Vector3(wx,y,face),new Vector3(1.45f,1.9f,.14f),"b2ada1",b);
                    Box("Dark window",new Vector3(wx,y,face+side*.09f),new Vector3(1.2f,1.64f,.13f),"283b43",b);
                    Box("Sash bar",new Vector3(wx,y,face+side*.17f),new Vector3(1.25f,.05f,.035f),"8e9b97",b);
                    Box("Sash bar",new Vector3(wx,y,face+side*.17f),new Vector3(.05f,1.67f,.035f),"8e9b97",b);
                    if(((int)(wx+y))%3==0)Box("Window air conditioner",new Vector3(wx,y-.65f,face+side*.3f),new Vector3(.65f,.4f,.65f),"89918c",b);
                }
                // Storefront glazing, doors and roll-down shutters are deliberately period-neutral.
                for(int shop=-1;shop<=1;shop++) {
                    Box("Storefront frame",new Vector3(shop*3,1.5f,face),new Vector3(2.65f,2.7f,.19f),"3c4946",b);
                    Box("Shop glass",new Vector3(shop*3,1.65f,face+side*.12f),new Vector3(2.3f,2.15f,.06f),"394c4e",b);
                    Box("Shop sill",new Vector3(shop*3,.38f,face+side*.14f),new Vector3(2.65f,.14f,.24f),"a19b90",b);
                }
                for(float y=5;y<height-1;y+=3) {
                    Box("Fire escape landing",new Vector3(2,y-1,face+side*.65f),new Vector3(2.8f,.1f,1.25f),"364447",b);
                    Box("Fire escape top rail",new Vector3(2,y,face+side*1.2f),new Vector3(2.8f,.045f,.045f),"364447",b);
                    for(int j=0;j<7;j++)Box("Fire escape bars",new Vector3(.65f+j*.45f,y-.5f,face+side*1.2f),new Vector3(.035f,1,.035f),"364447",b);
                    var ladder=Box("Escape ladder",new Vector3(2.7f,y-2,face+side*.8f),new Vector3(.6f,3.2f,.08f),"364447",b);ladder.transform.localRotation=Quaternion.Euler(0,0,15);
                }
            }
            Shape("Roof water tank",PrimitiveType.Cylinder,new Vector3(-1,height+1.6f,1),new Vector3(2.3f,1.4f,2.3f),"736655",b);
            for(int i=-1;i<=1;i+=2)Box("Tank supports",new Vector3(-1+i*.75f,height+.3f,1),new Vector3(.12f,1.5f,.12f),"414e51",b);
        }
        static void Car(Obstacle o) {
            var car=new GameObject("1980s parked sedan").transform;car.SetParent(root);car.position=new Vector3(o.x,0,o.z);
            Box("Steel body",new Vector3(0,.68f,0),new Vector3(4.9f,.65f,1.88f),o.color,car);
            Box("Squared cabin",new Vector3(-.15f,1.16f,0),new Vector3(2.5f,.65f,1.65f),o.color,car);
            foreach(int side in new[]{-1,1}) {
                Box("Side windows",new Vector3(-.15f,1.27f,side*.832f),new Vector3(2.18f,.38f,.026f),"304550",car);
                Box("Window pillar",new Vector3(-.15f,1.27f,side*.852f),new Vector3(.09f,.44f,.026f),o.color,car);
                Box("Chrome bumper",new Vector3(side*2.45f,.56f,0),new Vector3(.12f,.16f,1.9f),"a4afad",car);
                foreach(int end in new[]{-1,1}) {
                    var wheel=Shape("Tyre",PrimitiveType.Cylinder,new Vector3(end*1.6f,.36f,side*.91f),new Vector3(.65f,.12f,.65f),"222a2d",car);wheel.transform.localRotation=Quaternion.Euler(90,0,0);
                    Box("Lamp",new Vector3(end*2.46f,.78f,side*.6f),new Vector3(.025f,.18f,.38f),end>0?"dfd9b2":"9b3936",car);
                }
            }
            Box("Windscreen",new Vector3(1.115f,1.25f,0),new Vector3(.028f,.36f,1.52f),"304550",car);
            if(o.color=="d0a64c") {Box("Taxi roof light",new Vector3(0,1.62f,0),new Vector3(.7f,.22f,.35f),"ddd5ad",car);var t=Label("TAXI",new Vector3(0,1.63f,-.19f),.055f,Hex("263530"),car);}
        }
        static void Barrier(Obstacle o) {
            bool alongX=o.w>o.d;float length=alongX?o.w:o.d;
            var fence=new GameObject("Construction closure").transform;fence.SetParent(root);fence.position=new Vector3(o.x,0,o.z);
            if(!alongX)fence.rotation=Quaternion.Euler(0,90,0);
            Box("Plywood hoarding",new Vector3(0,1.2f,0),new Vector3(length,2.4f,.24f),"576f65",fence);
            Box("Warning stripe",new Vector3(0,.75f,-.16f),new Vector3(length,.24f,.06f),"dbd8bd",fence);
            for(float x=-length/2+1;x<length/2;x+=3) {
                var stripe=Box("Orange warning slash",new Vector3(x,.75f,-.2f),new Vector3(.3f,.28f,.04f),"bf6f43",fence);stripe.transform.localRotation=Quaternion.Euler(0,0,-28);
                Box("Fence support",new Vector3(x,1.2f,0),new Vector3(.12f,2.7f,.35f),"46554e",fence);
            }
            var text=Label("STREET CLOSED  /  UTILITY WORK",new Vector3(0,1.7f,-.2f),.13f,Hex("ede8cf"),fence);
            var reverse=Label("STREET CLOSED  /  UTILITY WORK",new Vector3(0,1.7f,.2f),.13f,Hex("ede8cf"),fence);reverse.transform.localRotation=Quaternion.Euler(0,180,0);
        }
        static void Hydrant(float x,float z) {
            Shape("Fire hydrant",PrimitiveType.Cylinder,new Vector3(x,.36f,z),new Vector3(.3f,.36f,.3f),"a94f42");
            Shape("Hydrant cap",PrimitiveType.Sphere,new Vector3(x,.76f,z),new Vector3(.37f,.22f,.37f),"a94f42");
            Box("Hose outlets",new Vector3(x,.5f,z),new Vector3(.49f,.15f,.16f),"847b68");
        }
        static void Prop(Obstacle o) {
            var parent=new GameObject(o.id).transform;parent.SetParent(root);parent.position=new Vector3(o.x,0,o.z);
            if(o.id.StartsWith("phone")) {
                Box("Payphone stand",new Vector3(0,.65f,0),new Vector3(.2f,1.3f,.2f),"71807c",parent);
                Box("Payphone hood",new Vector3(0,1.6f,0),new Vector3(.95f,.95f,.65f),"687c85",parent);
                Box("Payphone face",new Vector3(0,1.6f,-.34f),new Vector3(.62f,.78f,.07f),"c1c4b9",parent);
                Box("Receiver",new Vector3(-.2f,1.7f,-.42f),new Vector3(.12f,.46f,.12f),"252f31",parent);
                Label("PHONE",new Vector3(0,2.2f,-.35f),.075f,Hex("e9ead7"),parent);
            } else if(o.id.StartsWith("news")) {
                Box("Newspaper vending box",new Vector3(0,.6f,0),new Vector3(.8f,1.2f,.7f),"995146",parent);
                Box("Newspaper window",new Vector3(0,.8f,-.36f),new Vector3(.63f,.5f,.02f),"d6d1b9",parent);
                Label("DAILY NEWS",new Vector3(0,.87f,-.38f),.038f,Hex("313d3c"),parent);
            } else if(o.id.StartsWith("dumpster")) {
                Box("Steel dumpster",new Vector3(0,.55f,0),new Vector3(2.6f,1.1f,1),"3d6555",parent);
                Box("Dumpster lid",new Vector3(0,1.14f,0),new Vector3(2.7f,.14f,1.12f),"394743",parent);
                for(int i=0;i<3;i++)Shape("Tied rubbish sack",PrimitiveType.Sphere,new Vector3(-.8f+i*.7f,1.37f,0),new Vector3(.65f,.65f,.55f),"333b3b",parent);
            } else {
                Box("Milk crate",new Vector3(0,.36f,0),new Vector3(.65f,.72f,.65f),"8b7a61",parent);
                for(int i=0;i<4;i++)Box("Crate slot",new Vector3(-.24f+i*.16f,.4f,-.33f),new Vector3(.09f,.4f,.015f),"433f37",parent);
            }
        }
        static void BuildVendor(Vendor v) {
            var station=new GameObject(v.name).transform;station.SetParent(root);station.position=new Vector3(v.propX,0,v.propZ);station.rotation=Quaternion.Euler(0,v.facing,0);
            if(v.kind=="board"||v.kind=="notice") {
                Box("Notice board",new Vector3(0,1.6f,0),new Vector3(2.4f,1.4f,.14f),"4e6558",station);
                foreach(int side in new[]{-1,1})Box("Board leg",new Vector3(side*.9f,.8f,0),new Vector3(.1f,1.6f,.1f),"695744",station);
                Label(v.kind=="board"?"WORK HERE. LIVE HERE.":"ROAD CLOSED",new Vector3(0,1.9f,-.09f),.07f,Hex("e5dfc9"),station);
            } else if(v.kind=="machine"||v.kind=="infobot") {
                var actor=Creature("robot");actor.transform.SetParent(station,false);
                if(v.kind=="machine")Label("EXCHANGE",new Vector3(0,1.2f,-.34f),.053f,Hex("d1e0c5"),station);
                else for(int i=0;i<3;i++)Shape("Info button",PrimitiveType.Sphere,new Vector3(-.2f+i*.2f,1.1f,-.32f),new Vector3(.1f,.1f,.03f),"afc2a7",station);
            } else if(v.kind=="trash") {
                Shape("Sentient trash can",PrimitiveType.Cylinder,new Vector3(0,.6f,0),new Vector3(.78f,.6f,.78f),"697f72",station);
                Shape("Lid",PrimitiveType.Cylinder,new Vector3(0,1.2f,0),new Vector3(.85f,.035f,.85f),"8d9a8c",station);
                Box("Mouth",new Vector3(0,.65f,-.39f),new Vector3(.45f,.2f,.04f),"233831",station);
                foreach(int side in new[]{-1,1})Shape("Eye",PrimitiveType.Sphere,new Vector3(side*.2f,.99f,-.36f),new Vector3(.13f,.13f,.07f),"d5d2b1",station);
            } else if(v.kind=="bench") {
                for(int i=0;i<4;i++)Box("Bench slat",new Vector3(0,.52f,-.3f+i*.18f),new Vector3(2.4f,.09f,.14f),"807058",station);
                Box("Bench back",new Vector3(0,.95f,.3f),new Vector3(2.4f,.7f,.1f),"807058",station);
                foreach(int side in new[]{-1,1})Box("Bench leg",new Vector3(side*.8f,.25f,0),new Vector3(.13f,.5f,.65f),"3f514a",station);
            } else {
                Box("Stall counter",new Vector3(0,.75f,-.35f),new Vector3(3.4f,1.1f,.85f),"746452",station);
                for(int i=-2;i<=2;i++)Box("Canvas awning",new Vector3(i*.68f,2.8f,0),new Vector3(.68f,.12f,2),i%2==0?"667f70":"d5ceb3",station);
                foreach(int side in new[]{-1,1})Box("Awning post",new Vector3(side*1.65f,1.4f,.55f),new Vector3(.07f,2.8f,.07f),"52665a",station);
                var merchant=Creature(v.kind);merchant.transform.SetParent(station,false);merchant.transform.localPosition=new Vector3(0,0,.45f);
                merchant.AddComponent<IdleBob>().amount=.018f;
                if(v.kind=="mushroom")for(int i=-1;i<=1;i++) {
                    Shape("Mushroom stem",PrimitiveType.Cylinder,new Vector3(i,1.43f,-.35f),new Vector3(.1f,.15f,.1f),"cccab7",station);
                    Shape("Mushroom cap",PrimitiveType.Sphere,new Vector3(i,1.6f,-.35f),new Vector3(.55f,.22f,.55f),"839bb4",station);
                }
                if(v.kind=="cyclops")for(int i=0;i<5;i++)Shape("Vinyl record",PrimitiveType.Cylinder,new Vector3(-1+i*.5f,1.33f,-.35f),new Vector3(.3f,.015f,.3f),"263335",station);
                if(v.kind=="fox")for(int i=0;i<2;i++){var bird=FoldedBird(station.TransformPoint(new Vector3(i-.5f,2,-.6f)));bird.AddComponent<IdleBob>().amount=.16f;}
            }
            Label(v.subtitle,new Vector3(0,3.15f,0),.063f,Hex("e2e2cc"),station).gameObject.AddComponent<FaceCamera>();
        }
        public static GameObject PickupModel(Pickup item) {
            var obj=new GameObject(item.name);obj.transform.position=item.Position;var p=obj.transform;
            if(item.kind=="bottle") {
                Shape("Glass bottle",PrimitiveType.Cylinder,new Vector3(0,.15f,0),new Vector3(.1f,.15f,.1f),"587b68",p);
                Shape("Bottle neck",PrimitiveType.Cylinder,new Vector3(0,.34f,0),new Vector3(.05f,.05f,.05f),"587b68",p);
            }else if(item.kind=="coffee-cup")Shape("Paper cup",PrimitiveType.Cylinder,new Vector3(0,.08f,0),new Vector3(.1f,.08f,.1f),"b5c3bc",p);
            else if(item.kind=="cassette") {Box("Cassette",new Vector3(0,.025f,0),new Vector3(.10f,.025f,.064f),"302e2c",p);Box("Cassette label",new Vector3(0,.04f,0),new Vector3(.07f,.002f,.035f),"d3cbb0",p);}
            else {Box("Discarded newspaper",new Vector3(0,.02f,0),new Vector3(.4f,.006f,.3f),"c7c4ac",p);for(int i=0;i<4;i++)Box("Newsprint",new Vector3(0,.024f,-.1f+i*.05f),new Vector3(.32f,.002f,.015f),"75796f",p);}
            return obj;
        }
        public static GameObject Avatar(string name,int color,bool self) {
            var avatar=new GameObject(name);string coat=ColorUtility.ToHtmlStringRGB(Coats[Mathf.Clamp(color,0,5)]);
            Shape("Coat",PrimitiveType.Capsule,new Vector3(0,.92f,0),new Vector3(.65f,.62f,.5f),coat,avatar.transform);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.69f,0),new Vector3(.47f,.5f,.46f),"cbb1a3",avatar.transform);
            Box("Hair",new Vector3(0,1.9f,0),new Vector3(.46f,.16f,.45f),"454a48",avatar.transform);
            foreach(int side in new[]{-1,1}) {
                Box("Boot",new Vector3(side*.17f,.2f,.035f),new Vector3(.23f,.4f,.4f),"354448",avatar.transform);
                Shape("Sleeve",PrimitiveType.Capsule,new Vector3(side*.41f,1.01f,0),new Vector3(.2f,.4f,.2f),coat,avatar.transform);
            }
            Shape("Name ring",PrimitiveType.Cylinder,new Vector3(0,.05f,0),new Vector3(.9f,.014f,.9f),self?"b1d7b4":"546974",avatar.transform);
            avatar.AddComponent<WalkAnimation>();
            return avatar;
        }
        public static GameObject Creature(string kind) {
            var obj=new GameObject(kind);var parent=obj.transform;
            if(kind=="bag") {
                Box("Guard bag",new Vector3(0,.6f,0),new Vector3(.8f,.8f,.5f),"9c7a62",parent);
                Box("Handle left",new Vector3(-.2f,1.12f,0),new Vector3(.07f,.4f,.07f),"695646",parent);
                Box("Handle right",new Vector3(.2f,1.12f,0),new Vector3(.07f,.4f,.07f),"695646",parent);
                Box("Handle top",new Vector3(0,1.32f,0),new Vector3(.45f,.07f,.07f),"695646",parent);
                foreach(int s in new[]{-1,1}) {
                    Shape("Foot",PrimitiveType.Sphere,new Vector3(s*.25f,.15f,-.1f),new Vector3(.25f,.2f,.42f),"5f554b",parent);
                    Shape("Eye",PrimitiveType.Sphere,new Vector3(s*.17f,.75f,-.26f),new Vector3(.11f,.12f,.06f),"182e32",parent);
                }
            }else if(kind=="mushroom") {
                Shape("Merchant body",PrimitiveType.Capsule,new Vector3(0,1,0),new Vector3(.95f,1,.8f),"8b9a86",parent);
                Shape("Fungal cap",PrimitiveType.Sphere,new Vector3(0,2.1f,0),new Vector3(1.8f,.6f,1.4f),"939fb9",parent);
                for(int i=0;i<5;i++)Shape("Cap fleck",PrimitiveType.Sphere,new Vector3((i-2)*.28f,2.34f,-.2f+(i%2)*.3f),new Vector3(.14f,.06f,.14f),"d0d9d3",parent);
                foreach(int s in new[]{-1,1})Shape("Eye",PrimitiveType.Sphere,new Vector3(s*.22f,1.65f,-.37f),new Vector3(.12f,.1f,.06f),"293b40",parent);
            }else if(kind=="fox") {
                Box("Folded coat",new Vector3(0,.8f,0),new Vector3(.75f,1.3f,.6f),"d2b7a2",parent);
                var head=Box("Folded head",new Vector3(0,1.75f,-.08f),new Vector3(.65f,.65f,.62f),"e0c7b0",parent);head.transform.localRotation=Quaternion.Euler(0,0,12);
                foreach(int s in new[]{-1,1}) {
                    var ear=Box("Paper ear",new Vector3(s*.28f,2.17f,0),new Vector3(.22f,.5f,.1f),"bf8168",parent);ear.transform.localRotation=Quaternion.Euler(0,0,s*20);
                    Box("Eye",new Vector3(s*.17f,1.8f,-.405f),new Vector3(.09f,.09f,.02f),"283b40",parent);
                }
                var nose=Box("Paper muzzle",new Vector3(0,1.58f,-.43f),new Vector3(.28f,.25f,.45f),"e7d5bf",parent);nose.transform.localRotation=Quaternion.Euler(20,0,0);
            }else if(kind=="cyclops") {
                Shape("Cyclops coat",PrimitiveType.Capsule,new Vector3(0,.8f,0),new Vector3(.85f,.8f,.65f),"696b87",parent);
                Shape("Cyclops head",PrimitiveType.Sphere,new Vector3(0,1.75f,0),new Vector3(.83f,.8f,.7f),"8fa895",parent);
                Shape("Single eye",PrimitiveType.Sphere,new Vector3(0,1.82f,-.34f),new Vector3(.43f,.32f,.17f),"dbdcc4",parent);
                Shape("Pupil",PrimitiveType.Sphere,new Vector3(0,1.82f,-.43f),new Vector3(.14f,.18f,.04f),"253a36",parent);
            }else {
                Box("Robot torso",new Vector3(0,.95f,0),new Vector3(.8f,.95f,.6f),"839798",parent);
                Box("Robot face",new Vector3(0,1.8f,0),new Vector3(.85f,.65f,.62f),"405d60",parent);
                foreach(int s in new[]{-1,1}) {
                    Box("Robot leg",new Vector3(s*.25f,.3f,0),new Vector3(.25f,.6f,.32f),"4b666b",parent);
                    var eye=Box("Robot eye",new Vector3(s*.19f,1.84f,-.32f),new Vector3(.13f,.07f,.02f),"c8e6d5",parent);eye.GetComponent<Renderer>().sharedMaterial=Mat("c8e6d5",true);
                }
            }
            return obj;
        }
        static GameObject FoldedBird(Vector3 position) {
            var obj=new GameObject("Folded paper bird");obj.transform.SetParent(root);obj.transform.position=position;
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(0,0,-.35f),new Vector3(-.6f,.18f,0),new Vector3(0,.15f,.1f),new Vector3(.6f,.18f,0),new Vector3(0,0,.45f)};
            mesh.triangles=new[]{0,1,2,2,3,0,1,4,2,2,4,3,2,1,0,0,3,2,2,4,1,3,4,2};mesh.RecalculateNormals();
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=Mat("d7e1d1");return obj;
        }
        static void Bird(Vector3 p) {
            Shape("Rooftop watcher",PrimitiveType.Sphere,p,new Vector3(.4f,.55f,.7f),"3f5260");
            Shape("Watcher head",PrimitiveType.Sphere,p+new Vector3(0,.4f,-.2f),new Vector3(.36f,.34f,.36f),"4a6574");
        }
    }
}
