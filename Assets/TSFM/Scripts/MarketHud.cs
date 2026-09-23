using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace TSFM {
    public class MarketHud:MonoBehaviour {
        MarketGame game;RectTransform canvas,entry,dialog,inventory,options;Text balance,people,job,toast,entryStatus,nearLabel,chatLog;
        InputField nameInput,chatInput;Button joinButton;GameObject nearPanel;int color;float toastUntil;
        Vendor shownVendor;readonly List<string> messages=new List<string>();
        readonly Color ink=MarketWorld.Hex("142623"),paper=MarketWorld.Hex("e0e7d6"),muted=MarketWorld.Hex("a8b9ad"),accent=MarketWorld.Hex("a9cbb1");
        public bool InputBlocked=>entry.gameObject.activeSelf||dialog.gameObject.activeSelf||inventory.gameObject.activeSelf||options.gameObject.activeSelf||chatInput.isFocused;
        public bool Typing=>nameInput.isFocused||chatInput.isFocused;
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h,Vector2? anchor=null) {
            var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=anchor??new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color c,Vector2? anchor=null) {
            var r=Rect(name,parent,x,y,w,h,anchor);r.gameObject.AddComponent<Image>().color=c;return r;
        }
        Text Text(string value,Transform parent,float x,float y,float w,float h,int size,Color c,FontStyle style=FontStyle.Normal) {
            var r=Rect(value,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text=value;t.color=c;t.fontSize=size;t.fontStyle=style;t.supportRichText=false;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        Button Button(string label,Transform parent,float x,float y,float w,float h,Action click,bool primary=false) {
            var r=Panel(label,parent,x,y,w,h,primary?accent:MarketWorld.Hex("304b43"));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
            var colors=b.colors;colors.highlightedColor=MarketWorld.Hex("b9d1c2");colors.pressedColor=MarketWorld.Hex("8caaa0");b.colors=colors;
            b.onClick.AddListener(()=>click());var t=Text(label,r,12,0,w-24,h,16,primary?ink:paper,FontStyle.Bold);t.alignment=TextAnchor.MiddleCenter;return b;
        }
        InputField CreateInput(string label,Transform parent,float x,float y,float w,float h,int limit) {
            var r=Panel(label,parent,x,y,w,h,MarketWorld.Hex("263c36"));var field=r.gameObject.AddComponent<InputField>();
            var text=Text("",r,14,0,w-28,h,18,paper);text.alignment=TextAnchor.MiddleLeft;
            var placeholder=Text(label,r,14,0,w-28,h,18,muted);placeholder.alignment=TextAnchor.MiddleLeft;
            field.textComponent=text;field.placeholder=placeholder;field.characterLimit=limit;field.targetGraphic=r.GetComponent<Image>();return field;
        }
        public void Build(MarketGame owner) {
            game=owner;var obj=new GameObject("Market interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            obj.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scale=obj.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,900);scale.matchWidthOrHeight=.5f;
            canvas=obj.GetComponent<RectTransform>();
            var events=FindFirstObjectByType<EventSystem>();
            if(events==null){var e=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));events=e.GetComponent<EventSystem>();}
            events.sendNavigationEvents=false;
            var brand=Panel("Brand",canvas,24,24,407,88,ink);
            Text("THE STRANGEST FLEA MARKET",brand,20,15,367,24,19,paper,FontStyle.Bold);
            Text("NEW YORK  /  1985  /  MARKET BLOCK 01",brand,20,47,367,21,13,muted);
            var status=Panel("Status",canvas,-346,24,322,88,ink,new Vector2(1,1));
            balance=Text("0 SCARCITY TOKENS",status,18,15,286,23,18,paper,FontStyle.Bold);
            people=Text("CONNECTING",status,18,47,286,21,13,muted);
            var jobs=Panel("Current job",canvas,24,136,298,222,new Color(ink.r,ink.g,ink.b,.95f));
            Text("YOUR FIRST BLOCK",jobs,20,18,258,24,16,accent,FontStyle.Bold);
            job=Text("Start at the exchange machine.",jobs,20,56,258,96,17,paper);
            Button("Show me the way  →",jobs,20,165,258,38,Guide);
            var chat=Panel("Street chat",canvas,24,-210,400,186,new Color(ink.r,ink.g,ink.b,.91f),new Vector2(0,0));
            Text("STREET CHAT",chat,16,12,365,20,12,accent,FontStyle.Bold);
            chatLog=Text("",chat,16,38,365,89,14,paper);
            chatInput=CreateInput("Say something…",chat,12,137,298,37,180);
            Button("Send",chat,318,137,70,37,SendChat);
            chatInput.onEndEdit.AddListener(value=>{if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))SendChat();});
            var controls=Panel("Controls",canvas,-340,-89,316,65,ink,new Vector2(1,0));
            Text("WASD · walk   Shift · jog",controls,16,11,285,22,15,paper);
            Text("RMB · orbit   Wheel · zoom   ? · help",controls,16,36,285,18,13,muted);
            var inventoryButton=Button("Your bag",canvas,0,0,145,40,ToggleInventory);
            var ib=inventoryButton.GetComponent<RectTransform>();ib.anchorMin=ib.anchorMax=new Vector2(1,0);ib.anchoredPosition=new Vector2(-169,141);
            var helpButton=Button("?",canvas,0,0,44,40,ToggleOptions);
            var hb=helpButton.GetComponent<RectTransform>();hb.anchorMin=hb.anchorMax=new Vector2(1,0);hb.anchoredPosition=new Vector2(-225,141);
            options=Panel("Options",canvas,-260,-270,520,540,ink,new Vector2(.5f,.5f));options.gameObject.SetActive(false);
            nearPanel=Panel("Interaction",canvas,-190,-85,380,61,ink,new Vector2(.5f,0)).gameObject;
            nearLabel=Text("",nearPanel.transform,15,8,350,43,16,paper,FontStyle.Bold);nearLabel.alignment=TextAnchor.MiddleCenter;
            var interact=nearPanel.AddComponent<Button>();interact.onClick.AddListener(()=>game.OpenNearest());nearPanel.SetActive(false);
            var toastRoot=Panel("Notification",canvas,-260,129,520,58,new Color(ink.r,ink.g,ink.b,.94f),new Vector2(.5f,1));
            toast=Text("",toastRoot,18,10,484,40,16,paper);toast.alignment=TextAnchor.MiddleCenter;toastRoot.gameObject.SetActive(false);
            dialog=Panel("Conversation",canvas,-448,138,424,515,ink,new Vector2(1,1));dialog.gameObject.SetActive(false);
            inventory=Panel("Your inventory",canvas,-448,138,424,515,ink,new Vector2(1,1));inventory.gameObject.SetActive(false);
            entry=Panel("Guest entry",canvas,-290,-260,580,520,ink,new Vector2(.5f,.5f));
            Text("YOU HAVE ARRIVED AT",entry,38,32,504,24,13,accent,FontStyle.Bold);
            Text("The Strangest\nFlea Market",entry,38,70,504,116,47,paper,FontStyle.Bold);
            Text("One city block. A few impossible neighbors.\nMake yourself useful. Make yourself at home.",entry,38,193,504,54,18,muted);
            nameInput=CreateInput("Your market name",entry,38,269,504,48,20);
            Button("Coat color  ↻",entry,38,330,175,39,()=>{color=(color+1)%6;nameInput.GetComponent<Image>().color=MarketWorld.Coats[color]*.4f;});
            joinButton=Button("Enter the market  →",entry,226,330,316,39,()=>game.Join(nameInput.text,color),true);
            Button("New guest",entry,38,384,175,35,()=>game.Join(nameInput.text,color,true));
            Text("Returning guests keep their saved progress.\nNew guest starts a separate character.",entry,228,383,309,47,13,muted);
            entryStatus=Text("",entry,38,446,504,58,15,paper);
        }
        public void ShowEntry(string status,string name){entry.gameObject.SetActive(true);entryStatus.text=status;nameInput.text=name;joinButton.interactable=true;}
        public void HideEntry(){entry.gameObject.SetActive(false);}
        public void SetEntryStatus(string text){entryStatus.text=text;joinButton.interactable=true;}
        public void Population(int count){people.text=$"{count} PLAYER{(count==1?"":"S")} ON THIS BLOCK";}
        public void RefreshProfile() {
            var p=game.Profile;if(p==null)return;
            balance.text=$"{p.tokens} SCARCITY TOKENS";
            string step=p.job==1?"Collect the order from the mushroom merchant.":p.job==2?"Deliver the mushrooms to the bodega across the street.":!p.exchanged?"Exchange your Earth seed packet. Dollars have no value here.":"Find a delivery on the work board. Earn 10 tokens for each completed job.";
            job.text=step+$"\n\nReputation {p.xp}  ·  Deliveries {p.deliveries}";
            if(inventory.gameObject.activeSelf)ShowInventory();
            if(dialog.gameObject.activeSelf&&shownVendor!=null)ShowVendor(shownVendor);
        }
        void Guide() {
            var p=game.Profile;if(p==null)return;
            string id=p.job==1?"mushroom":p.job==2?"bodega":!p.exchanged?"exchange":"board";
            foreach(var vendor in game.Block.vendors)if(vendor.id==id){game.WalkTo(vendor);break;}
        }
        public void Interaction(Vendor v,Pickup pickup){nearPanel.SetActive(game.Joined&&(v!=null||pickup!=null)&&!InputBlocked);if(pickup!=null)nearLabel.text=$"E  ·  Pick up {pickup.name}";else if(v!=null)nearLabel.text=$"E  ·  {v.name}  →";}
        public void Notify(string message){if(string.IsNullOrEmpty(message))return;toast.text=message;toast.transform.parent.gameObject.SetActive(true);toastUntil=Time.unscaledTime+5;}
        public void AddChat(string text){messages.Add(text);while(messages.Count>5)messages.RemoveAt(0);chatLog.text=string.Join("\n",messages);}
        public void FocusChat(){chatInput.ActivateInputField();}
        void SendChat(){if(!string.IsNullOrWhiteSpace(chatInput.text)){game.Chat(chatInput.text);chatInput.text="";}chatInput.DeactivateInputField();EventSystem.current.SetSelectedGameObject(null);}
        void Clear(RectTransform panel){for(int i=panel.childCount-1;i>=0;i--){var child=panel.GetChild(i);child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        public void ShowVendor(Vendor vendor) {
            options.gameObject.SetActive(false);inventory.gameObject.SetActive(false);shownVendor=vendor;dialog.gameObject.SetActive(true);Clear(dialog);
            Text(vendor.subtitle,dialog,26,25,320,24,13,accent,FontStyle.Bold);
            Text(vendor.name,dialog,26,69,372,69,31,paper,FontStyle.Bold);
            bool business=!string.IsNullOrEmpty(vendor.action)||!string.IsNullOrEmpty(vendor.item);
            Text(vendor.line,dialog,26,153,372,business?104:265,business?19:20,paper);
            if(business)Text("MARKET BUSINESS",dialog,26,268,372,20,12,accent,FontStyle.Bold);
            float y=306;
            if(!string.IsNullOrEmpty(vendor.action)) {
                string label=vendor.actionLabel;
                var p=game.Profile;
                if(vendor.id=="exchange"&&p.exchanged)label="Seed packet already exchanged";
                var b=Button(label,dialog,26,y,372,48,()=>game.Action(vendor,vendor.action),true);
                b.interactable=!(vendor.id=="exchange"&&p.exchanged);y+=64;
            }
            if(!string.IsNullOrEmpty(vendor.item)) {
                var b=Button($"{vendor.itemLabel}  ·  {vendor.price} tokens",dialog,26,y,372,48,()=>game.Action(vendor,"buy",vendor.item),true);
                b.interactable=game.Profile.tokens>=vendor.price&&!(vendor.item=="guard-bag"&&game.Profile.bag);
                if(!b.interactable)Text(vendor.item=="guard-bag"&&game.Profile.bag?"Your guard bag is already on duty.":"Complete a delivery to earn more tokens.",dialog,26,y+54,372,42,14,muted);
            }
            Button("Close",dialog,314,464,84,31,ClosePanels);
        }
        public void ToggleInventory(){if(game.Profile==null)return;if(inventory.gameObject.activeSelf)ClosePanels();else ShowInventory();}
        void ShowInventory() {
            options.gameObject.SetActive(false);dialog.gameObject.SetActive(false);inventory.gameObject.SetActive(true);Clear(inventory);
            Text("YOUR BELONGINGS",inventory,26,25,350,24,13,accent,FontStyle.Bold);
            Text("A bag of possibilities.",inventory,26,68,372,89,32,paper,FontStyle.Bold);
            var viewport=Panel("Inventory viewport",inventory,22,155,380,250,new Color(0,0,0,0));viewport.gameObject.AddComponent<RectMask2D>();
            var body=Rect("Items",viewport,0,0,375,500);var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=body;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=25;
            var p=game.Profile;float y=5;
            if(!p.exchanged){Text("Earth seed packet  ·  for exchange",body,4,y,365,34,18,paper);y+=45;}
            if(p.job==2){Text("Bodega mushroom delivery",body,4,y,365,34,18,accent);y+=45;}
            if(p.inventory!=null)foreach(var item in p.inventory){Text($"{item.name}  ×{item.count}",body,4,y,365,34,18,paper);y+=45;}
            if(y==5)Text("Your pockets are empty.\nThe vendors can help with that.",body,4,y,365,80,20,muted);
            body.sizeDelta=new Vector2(375,Mathf.Max(250,y+85));
            Text("Market goods stay inside the market.",inventory,26,420,372,25,14,muted);Button("Close",inventory,314,464,84,31,ClosePanels);
        }
        public void ClosePanels(){options.gameObject.SetActive(false);dialog.gameObject.SetActive(false);inventory.gameObject.SetActive(false);chatInput.DeactivateInputField();EventSystem.current.SetSelectedGameObject(null);}
        public void ToggleOptions() {
            if(options.gameObject.activeSelf){ClosePanels();return;}
            ClosePanels();options.gameObject.SetActive(true);Clear(options);options.SetAsLastSibling();
            Text("MARKET FIELD GUIDE",options,28,24,464,24,13,accent,FontStyle.Bold);
            Text("Make yourself at home.",options,28,60,464,46,29,paper,FontStyle.Bold);
            Text("WASD / arrows     Walk relative to camera\nShift     Jog\nRight mouse drag     Orbit camera\nMouse wheel     Zoom in / out\nClick ground     Walk to a location\nE     Talk / collect a nearby item\nI     Inventory     Enter     Street chat\n? or F1     Help & options     Esc     Close",options,28,121,464,220,17,paper);
            Button("Sensitivity -",options,28,356,146,38,()=>game.Orbit.Sensitivity=Mathf.Max(.5f,game.Orbit.Sensitivity-.3f));
            Button("Sensitivity +",options,184,356,146,38,()=>game.Orbit.Sensitivity=Mathf.Min(6,game.Orbit.Sensitivity+.3f));
            Button("Reset view",options,340,356,152,38,()=>game.Orbit.ResetView());
            Button(game.Orbit.InvertY?"Invert Y: on":"Invert Y: off",options,28,408,146,38,()=>{game.Orbit.InvertY=!game.Orbit.InvertY;ToggleOptions();ToggleOptions();});
            Button("Toggle shadows",options,184,408,146,38,()=>{QualitySettings.shadows=QualitySettings.shadows==ShadowQuality.Disable?ShadowQuality.All:ShadowQuality.Disable;Notify(QualitySettings.shadows==ShadowQuality.Disable?"Shadows off":"Shadows on");});
            Button("Fullscreen",options,340,408,152,38,()=>Screen.fullScreen=!Screen.fullScreen);
            Text("Goods stay in NYC. Construction closes this block.",options,28,468,370,45,14,muted);
            Button("Close",options,402,480,90,34,ClosePanels);
        }
        void Update(){if(toast!=null&&Time.unscaledTime>toastUntil)toast.transform.parent.gameObject.SetActive(false);}
    }
}
