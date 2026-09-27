#!/usr/bin/env python3
"""Build original indexed artwork and native SCUMM v6 resources. GPL-2.0-or-later."""
from PIL import Image, ImageDraw
from pathlib import Path
import os,sys,json,struct,subprocess,shutil,random,re
ROOT=Path(__file__).resolve().parents[1]
TOOL=Path(os.environ.get('SCUMMC_ROOT','/mnt/data/subway-tools/scummc'))
BIN=Path(os.environ.get('SCUMMC_BIN',next(TOOL.glob('build.*/*/scc')).parent if list(TOOL.glob('build.*/*/scc')) else TOOL/'bin'))
OUT=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else ROOT/'web/game'
CFG=json.loads(Path(sys.argv[2]).read_text()) if len(sys.argv)>2 else json.loads((ROOT/'project.json').read_text())
SRC=OUT.parent/'source';SRC.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
P=[tuple(bytes.fromhex(x.lstrip('#'))) for x in CFG['palette']]
P+=([(0,0,0)]*(256-len(P)));flat=[v for c in P for v in c]
def img(w,h,color=0):
 i=Image.new('P',(w,h),color);i.putpalette(flat);return i
def save(i,n):i.save(SRC/n)
def rect(d,box,c):d.rectangle(tuple(map(int,box)),fill=c)
def text(d,xy,s,c):d.text(xy,s,fill=c,font_size=8)
W,H=320,144
floor=int(CFG['floor_y']);ceiling=int(CFG['ceiling_y']);height=round((floor-ceiling)*CFG['character_scale'])
windows=[list(map(int,w)) for w in CFG['windows']]
for car in range(3):
 rng=random.Random(84+car)
 tunnel=img(W,H,32);d=ImageDraw.Draw(tunnel)
 for y in range(0,H,8):
  d.line((0,y,319,y),fill=33)
  for x in range((y//8%2)*16,320,32):d.line((x,y,x,y+7),fill=34)
 interior=img(W,H);d=ImageDraw.Draw(interior);rect(d,(0,ceiling,319,floor),37)
 for x in range(0,320,52):
  rect(d,(x+2,ceiling+2,x+45,floor-22),38);rect(d,(x+4,ceiling+4,x+43,ceiling+8),40)
  rect(d,(x+5,floor-20,x+43,floor-13),42);d.line((x+5,floor-21,x+43,floor-21),fill=43)
  rect(d,(x+3,floor-12,x+45,floor-9),41);d.line((x+46,ceiling+3,x+46,floor),fill=45);d.line((x+47,ceiling+3,x+47,floor),fill=46)
 exterior=img(W,H);d=ImageDraw.Draw(exterior);rect(d,(0,18,319,122),48)
 for y,c in [(18,50),(19,51),(20,53),(21,51),(22,50),(24,49),(26,48),(109,47),(118,50),(120,54),(122,33)]:d.line((0,y,319,y),fill=c)
 for x0,y0,x1,y1 in windows:
  rect(d,(x0-3,y0-3,x1+3,y1+3),33);rect(d,(x0-2,y0-2,x1+2,y1+2),52);rect(d,(x0-1,y0-1,x1+1,y1+1),35);rect(d,(x0,y0,x1,y1),0);d.line((x0,y0-2,x1,y0-2),fill=55)
 for x in [0,69,105,213,249,319]:
  d.line((x,31,x,118),fill=33);d.line((x+1,33,x+1,119),fill=50);rect(d,(x+26,95,x+28,101),33);d.line((x+27,96,x+27,100),fill=55)
 for x in range(4,320,14):
  for y in [29,116]:d.point((x,y),fill=55);d.point((x+1,y+1),fill=47)
 text(d,(5,19),str(402+car),55);rect(d,(137,23,187,31),33);text(d,(142,22),'DOWNTOWN',43)
 tags=['NIGHT','KEEP','LOCAL'];tx=[8,115,9][car]
 for dx,dy,c in [(1,2,33),(0,1,61),(0,0,59)]:text(d,(tx+dx,94+dy),tags[car],c)
 for k in range(20):
  x=rng.randrange(5,305);y=rng.randrange(101,116);d.line((x,y,x+rng.randrange(2,13),y-2),fill=56+car)
 for x in [146+car*13,178+car*7,200]:d.line((x,111,x+6,94,x+22,90),fill=56)
 under=img(W,H);d=ImageDraw.Draw(under);rect(d,(0,124,319,129),34)
 for x in [22,61,258,296]:
  d.ellipse((x-10,125,x+10,141),fill=33);d.ellipse((x-6,128,x+6,138),fill=50);d.ellipse((x-3,131,x+3,136),fill=32)
 for x in [88,150,202]:rect(d,(x,127,x+34,135),35);d.line((x,128,x+32,128),fill=49)
 glass=img(W,H);gd=ImageDraw.Draw(glass)
 for x0,y0,x1,y1 in windows:
  for k in range(10):gd.point((x0+5+k//2,y0+4+k*2),fill=46)
  gd.line((x0+2,y1-1,x1-1,y1-1),fill=41)
 composite=tunnel.copy()
 for layer in (interior,exterior,under,glass):composite.paste(layer,(0,0),Image.frombytes('L',layer.size,layer.tobytes()).point(lambda x:255 if x else 0))
 save(composite,f'car{car}.bmp')
 for name,layer in [('tunnel',tunnel),('interior',interior),('exterior',exterior),('undercarriage',under),('glass',glass)]:save(layer,f'car{car}-{name}.bmp')
 mask=Image.new('P',(W,H),1);mask.putpalette([0,0,0,255,255,255]+[0]*762);md=ImageDraw.Draw(mask)
 for w in windows:md.rectangle(w,fill=0)
 for x,y in [(x,y) for y in range(H) for x in range(W) if glass.getpixel((x,y))]:md.point((x,y),fill=1)
 mask.save(SRC/f'car{car}-mask.bmp')
for f in range(48):
 top=img(320,16,32);td=ImageDraw.Draw(top);rail=img(320,16,32);rd=ImageDraw.Draw(rail)
 for x in range(-96,416,96):
  x-=f*2;td.line((x,0,x,15),fill=35);td.line((x+1,0,x+1,15),fill=33)
  for y in [2,7,13]:td.line((x+3,y,x+90,y),fill=34)
  rect(td,(x+32,8,x+55,10),35);td.line((x+37,9,x+48,9),fill=43)
 for y in [5,12]:rd.line((0,y,319,y),fill=49)
 for x in range(-32,352,24):rd.line((x-f*3%24,15,x+18-f*3%24,1),fill=35,width=2)
 save(top,f'top{f}.bmp');save(rail,f'rails{f}.bmp')
(SRC/'person').mkdir(exist_ok=True)
for f in range(4):
 frame=img(20,height);d=ImageDraw.Draw(frame);head=max(7,round(height*.2));body=round(height*.46)
 rect(d,(7,0,13,head-2),17);rect(d,(6,2,14,head-3),17);rect(d,(8,3,14,head),19)
 rect(d,(6,0,13,3),16);rect(d,(6,2,8,6),16);d.point((13,5),fill=16);d.point((15,7),fill=19)
 rect(d,(8,head,11,head+2),18);d.polygon([(5,head+2),(13,head+2),(15,head+body),(5,head+body)],fill=20)
 d.line((9,head+3,9,head+body-1),fill=22);d.line((6,head+3,6,head+body-2),fill=21)
 swing=[0,2,0,-2][f];d.line((13,head+4,14+swing,head+body-4),fill=21,width=3);rect(d,(13+swing,head+body-4,15+swing,head+body-1),19)
 hip=head+body;stride=[0,3,0,-3][f]
 d.line((7,hip,6+stride,height-3),fill=23,width=3);d.line((12,hip,12-stride,height-3),fill=24,width=3)
 rect(d,(4+stride,height-3,9+stride,height-1),16);rect(d,(10-stride,height-3,15-stride,height-1),16)
 for direction in 'EWSN':save(frame.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if direction=='W' else frame,f'person/walk{direction}{f:02}.bmp')
for name in ['wallet','tokens','card']:
 i=img(32,24);d=ImageDraw.Draw(i)
 if name=='wallet':
  d.polygon([(3,5),(26,3),(28,20),(4,22),(1,17)],fill=27);rect(d,(4,6,25,19),28);d.line((4,7,24,5),fill=29);rect(d,(20,10,30,15),27);d.point((25,12),fill=30)
 elif name=='tokens':
  for x,y in [(5,8),(13,3),(20,10)]:d.ellipse((x,y,x+10,y+10),fill=29);d.ellipse((x+2,y+2,x+8,y+8),fill=28);d.arc((x+1,y+1,x+9,y+9),180,290,fill=30)
  text(d,(12,17),str(CFG['gold_tokens']),30)
 else:d.polygon([(2,5),(28,1),(30,19),(4,23)],fill=31);d.line((3,9,29,5),fill=16,width=3);text(d,(7,11),'NYC',27)
 save(i,f'{name}.bmp')
box0=struct.pack('<8hBBH',*([-32000]*8),0,0,255)
box1=struct.pack('<8hBBH',2,floor,317,floor,317,floor+1,2,floor+1,1,0,255)
data=struct.pack('<H',2)+box0+box1
(SRC/'aisle.box').write_bytes(b'BOXD'+struct.pack('>I',8+len(data))+data)
for fn in ['vera.bmp','vera-small.bmp','vera-gui.bmp','cursor.bmp']:shutil.copy(TOOL/'examples/road'/fn,SRC/fn)
h=(TOOL/'examples/road/common.sch').read_text().replace('object axe;','object wallet; object tokens; object card;')
h+='\nint spawnX,carIndex,walkSpeed,ambientSpeed;\nroom Road; room Car1; room Car3;\n';(SRC/'common.sch').write_text(h)
c=(TOOL/'examples/road/common.scc').read_text()
c=c.replace('"devil.cost"','"person.cost"').replace('"Beasty"','"Traveler"').replace('setActorWalkSpeed(2,1);',f'setActorWalkSpeed({int(CFG["walk_speed"])},1);').replace('setActorTalkPos(-60,-60);',f'setActorTalkPos(0,-{height+6});').replace('screenEffect(0x0005);','screenEffect(0);')
c=re.sub(r'    // the inventory icons.*?    local script localTest', '    // the inventory icons\n'+''.join(f'''    object {n} {{
       x=0; y=0; w=32; h=24; name="{CFG['inventory_names'][n]}";
       states={{{{0,0,"{n}.bmp"}}}}; state=1;
       verb(int vrb,int objA,int objB) {{
        case Icon: startScript2(vrb,[ResRoom::{n},objA,objB]);return;
        case LookAt: egoSay("{CFG['descriptions'][n]}");return;
        case Open: egoSay("{CFG['descriptions'][n]}");return;
        case Use: egoSay("{CFG['use_text'][n]}");return;
        case Give: egoSay("There is nobody else aboard.");return;
       }}
    }}\n''' for n in ['wallet','tokens','card'])+'    local script localTest',c,flags=re.S)
c=re.sub(r'    script keyboardHandler\(int key\) \{.*?\n    \}', '''    script keyboardHandler(int key) {
      if(key == 32) { pauseGame(); }
    }''',c,flags=re.S)
c=c.replace('        // A verb was clicked','''        if(area == 2) {
          stopSentence();
          if(btn == 2 || selVerb == LookAt) { egoSay("Empty seats. Warm lights. The tunnel slipping past."); resetSntc(0); return; }
          resetSntc(0);walkActorTo(VAR_EGO,VAR_VIRT_MOUSE_X,'''+str(floor)+''');return;
        }
        // A verb was clicked''')
c=c.replace('setVerbXY(160 + c*40,160 + l*20);','setVerbXY(163 + c*46,158 + l*24);').replace('setVerbXY(135,160);','setVerbXY(147,160);').replace('setVerbXY(135,182);','setVerbXY(147,182);')
c=c.replace('setVerbXY(10,','setVerbXY(6,').replace('setVerbXY(50,','setVerbXY(48,').replace('setVerbXY(100,','setVerbXY(108,')
c=c.replace('        startRoom(Road);',f'''        spawnX=160;carIndex=1;walkSpeed={int(CFG['walk_speed'])};ambientSpeed={int(CFG['tunnel_delay'])};
        pickupObject(wallet,ResRoom);pickupObject(tokens,ResRoom);pickupObject(card,ResRoom);
        startRoom(Road);''')
c=c.replace('ScummC Paused !','Ride paused').replace('ScummC test Menu','TSFM - Night Train').replace('Saveing','Saving');(SRC/'common.scc').write_text(c)
cost='palette([0-31]);\n'
for direction in 'EWSN':cost+=f'picture walk{direction} = {{ glob="person/walk{direction}??.bmp", position={{-10,-{height}}} }};\n'
cost+='limb body;\n'
for anim in ['init','stand','walk','talkStart','talkStop']:
 cost+=f'anim {anim} = {{\n'
 for direction in 'EWSN':cost+=f'{direction}={{body('+','.join(f'walk{direction}{f:02}' for f in (range(4) if anim=='walk' else [0]))+')};\n'
 cost+='};\n'
(SRC/'person.scost').write_text(cost)
rooms='#include <scummVars6.s>\n#include "common.sch"\n'
for car,name in enumerate(['Car1','Road','Car3']):
 rooms+=f'room {name} {{\n image="car{car}.bmp";zplane={{"car{car}-mask.bmp"}};boxd="aisle.box";trans=0;\n'
 for obj,y,prefix in [('tunnel',0,'top'),('rails',128,'rails')]:rooms+=f'object {obj} {{x=0;y={y};w=320;h=16;name="{obj}";states={{'+','.join(f'{{0,0,"{prefix}{f}.bmp"}}' for f in range(48))+'};state=1;}\n'
 rooms+='''local script motion() { int f,b;f=1;
  while(1) {setObjectState(tunnel,f);setObjectState(rails,f);f++;if(f>48) f=1;
    b=230+f;if(f>24)b=278-f;setRoomRGBIntensity(b,b,255,16,63);delay(ambientSpeed);
  }
 }
 local script boundary() {int x;while(1){x=getObjectX(VAR_EGO);
'''
 if car>0:rooms+=f'if(x<=5){{spawnX=307;startRoom({["Car1","Road"][car-1]});return;}}\n'
 if car<2:rooms+=f'if(x>=314){{spawnX=12;startRoom({["Road","Car3"][car]});return;}}\n'
 rooms+='breakScript();}}\n'
 rooms+=f'''local script entry() {{
 createBoxMatrix();carIndex={car};putActorAt(VAR_EGO,spawnX,{floor},{name});
 setCurrentActor(VAR_EGO);setActorZClip(1);setActorStanding();setActorWalkSpeed(walkSpeed,1);
 setCameraAt(160);egoPrintBegin();egoPrintOverhead();actorPrintEnd();
 ResRoom::showVerbs(1);ResRoom::showCursor();motion();boundary();dbgPrint("TSFM_CAR={car+1}");
 }}
}}\n'''
(SRC/'cars.scc').write_text(rooms)
for f in ['vera','vera-small','vera-gui']:subprocess.run([str(BIN/'char'),'-ochar',f+'.char','-ibmp',f+'.bmp'],cwd=SRC,check=True,capture_output=True)
subprocess.run([str(BIN/'cost'),'-o','person.cost','-I','.','person.scost'],cwd=SRC,check=True)
for f in ['common','cars']:subprocess.run([str(BIN/'scc'),'-o',f+'.roobj','-I',str(TOOL/'include'),'-I','.','-R','.',f+'.scc'],cwd=SRC,check=True)
subprocess.run([str(BIN/'sld'),'-o',str(OUT/'tentacle'),'-key','0x69','common.roobj','cars.roobj'],cwd=SRC,check=True)
(OUT/'build.json').write_text(json.dumps({'engine':'ScummVM / SCUMM v6','cars':3,'player_height':height,'interior_height':floor-ceiling,'scale':height/(floor-ceiling),'gold_tokens':CFG['gold_tokens']}))
print('Built original three-car SCUMM game:',OUT)
