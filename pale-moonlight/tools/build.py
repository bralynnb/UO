"""Compile project.json to a genuine SLUDGE game for ScummVM."""
from pathlib import Path
import json, os, sys, shutil, subprocess, re, io, struct
from PIL import Image, ImageEnhance, ImageColor
R=Path(__file__).resolve().parents[1]; G=R/'game'; A=R/'assets'; G.mkdir(exist_ok=True)
VERBS=['Give','Open','Close','Pick up','Look at','Talk to','Use','Push','Pull']
def q(s):return json.dumps(s,ensure_ascii=True)
def bank(path,im,hot):
 b=io.BytesIO();im.save(b,format='PNG');path.write_bytes(b'\0\0\3\0\1'+struct.pack('<hh',*hot)+b.getvalue()[8:])
def triangles(poly):
 pts=[tuple(map(int,p)) for p in poly]
 if len(pts)<3:raise ValueError('Each floor needs at least three vertices')
 if len(set(pts))!=len(pts):raise ValueError('Duplicate floor vertex')
 def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
 for i in range(len(pts)):
  a,b=pts[i],pts[(i+1)%len(pts)]
  for j in range(i+1,len(pts)):
   if j in [i,(i+1)%len(pts)] or (j+1)%len(pts)==i:continue
   c,d=pts[j],pts[(j+1)%len(pts)]
   if cross(a,b,c)*cross(a,b,d)<=0 and cross(c,d,a)*cross(c,d,b)<=0 and max(min(a[0],b[0]),min(c[0],d[0]))<=min(max(a[0],b[0]),max(c[0],d[0])) and max(min(a[1],b[1]),min(c[1],d[1]))<=min(max(a[1],b[1]),max(c[1],d[1])):raise ValueError('Floor edges cross')
 if sum(pts[i][0]*pts[(i+1)%len(pts)][1]-pts[(i+1)%len(pts)][0]*pts[i][1] for i in range(len(pts)))<0:pts.reverse()
 out=[]
 while len(pts)>3:
  for i,b in enumerate(pts):
   a=pts[i-1];c=pts[(i+1)%len(pts)]
   if cross(a,b,c)<=0:continue
   if any(cross(a,b,p)>=0 and cross(b,c,p)>=0 and cross(c,a,p)>=0 for p in pts if p not in [a,b,c]):continue
   out.append([a,b,c]);pts.pop(i);break
  else:raise ValueError('Floor polygon crosses itself or has duplicate/collinear vertices')
 out.append(pts);return out
def floor_polygons(poly):
 parts=triangles(poly)
 def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
 changed=True
 while changed:
  changed=False
  for i in range(len(parts)):
   for j in range(i+1,len(parts)):
    edges=[(a[k],a[(k+1)%len(a)]) for a in [parts[i],parts[j]] for k in range(len(a))]
    boundary=[e for e in edges if (e[1],e[0]) not in edges]
    if len(boundary)==len(edges):continue
    chain=[boundary[0][0]]
    while len(chain)<len(boundary):
     nxt=next((b for a,b in boundary if a==chain[-1]),None)
     if nxt is None or nxt in chain:break
     chain.append(nxt)
    if len(chain)!=len(boundary) or any(cross(chain[k-1],chain[k],chain[(k+1)%len(chain)])<0 for k in range(len(chain))):continue
    parts[i]=chain;parts.pop(j);changed=True;break
   if changed:break
 return parts

def validate(p):
 if p.get('schema')!=1:raise ValueError('Unsupported project version')
 ids=set();rooms={r['id'] for r in p['rooms']};flags=set(p['flags'])
 if len(rooms)!=len(p['rooms']):raise ValueError('Duplicate room ID')
 for flag in flags:
  if not re.fullmatch('[a-z][a-z0-9_]*',flag):raise ValueError('Invalid flag')
 for room in p['rooms']:
  if not re.fullmatch('[a-z][a-z0-9_]*',room['id']):raise ValueError('Invalid room ID')
  if not (A/(room['background']+'.tga')).is_file():raise ValueError('Missing background')
  for poly in room['floor']:
   if any(not(0<=x<=639 and 0<=y<=351) for x,y in poly):raise ValueError('Floor vertex outside scene')
   triangles(poly)
  for o in room['objects']:
   if not re.fullmatch('[a-z][a-z0-9_]*',o['id']) or o['id'] in ids:raise ValueError('Object IDs must be unique lowercase identifiers')
   ids.add(o['id'])
   if o.get('exit') and o['exit'] not in rooms:raise ValueError('Unknown exit room')
   if o['asset'] and (not re.fullmatch('[a-z][a-z0-9_]*',o['asset']) or not (A/(o['asset']+'.png')).is_file()):raise ValueError('Unknown sprite')
   if not(1<=o['width']<=640 and 1<=o['height']<=352):raise ValueError('Invalid object dimensions')
   for a in o['actions']:
    if a['verb'] not in VERBS+['Use item']:raise ValueError('Unknown action verb')
    if any(f.lstrip('!') not in flags for f in a.get('requires',[])+a.get('sets',[])):raise ValueError('Unknown action flag')
    if a.get('dialogue') and a['dialogue'] not in p['dialogues']:raise ValueError('Unknown dialogue')
 return p
def build():
 p=validate(json.loads((R/'project.json').read_text()));s=p['settings'];objects=[o for r in p['rooms'] for o in r['objects']];ix={o['id']:i for i,o in enumerate(objects)}
 chunks=[]
 def emit(t):chunks.append(t)
 emit('# Generated from project.json; edit the project, then rebuild.\n')
 for f in p['flags']:emit('var f_'+f+' = 0;')
 emit('var inventory = newStack(); var invOffset=0; var roomNow = 0; var verb = "Walk to"; var held = NULL; var busy = 0; var choosing = 0; var choice = -1;')
 emit('var px = newStack('+','.join(str(o['x']) for o in objects)+');')
 emit('var py = newStack('+','.join(str(o['y']) for o in objects)+');')
 emit('sub inInventory(o) { var i=0; while(i<stackSize(inventory)) { if(inventory[i]==o)return TRUE; i++; } return FALSE; }')
 emit('objectType sisko ("") { speechColour 225, 210, 166; }')
 for o in objects:emit('objectType '+o['id']+' ('+q(o['name'])+') { speechColour 163, 199, 199; }')
 emit('sub egoCostume () { return costume ('+','.join(["anim ('../assets/sisko.duc',%d)"%(d*7) for d in range(4)]+["anim ('../assets/sisko.duc',"+','.join('wait (%d, 4)'%(d*7+j) for j in range(1,7))+')' for d in range(4)]+["anim ('../assets/sisko.duc',%d)"%(d*7) for d in range(4)])+'); }')
 emit('sub init () { setCustomEncoding(4701); setFont (\'../assets/font.duc\', '+q(''.join(chr(i) for i in range(32,127)))+', 16); setCursor (anim (\'../assets/cursor.duc\',0)); setSpeechSpeed ('+str(s['speechSpeed'])+'); setSpeechMode (TEXTONLY); setScale ('+str(s['horizon'])+','+str(s['scaleDivide'])+'); addStatus (); alignStatus (LEFT); positionStatus (18,357); setStatusColour (217,184,126); onLeftMouse (click); onRightMouse (rightClick); onKeyboard (keys); onFocusChange (focus); enterRoom (0); }')
 emit('sub gui () { setBlankColour (10,15,22); blankArea (0,352,639,479); setBlankColour (118,94,65); blankArea (0,350,639,351); setPasteColour (177,153,114);')
 for i,v in enumerate(VERBS):emit('pasteString ('+str(18+(i%3)*101)+','+str(388+(i//3)*24)+','+q(v)+');')
 emit('setPasteColour (91,118,131); pasteString (342,385,"INVENTORY"); var ii=invOffset; setPasteColour (208,194,158); while(ii<stackSize(inventory) && ii<invOffset+2) { pasteString(342,410+(ii-invOffset)*21,inventory[ii]); ii++; } if(stackSize(inventory)>2) pasteString(584,435,">>"); if(f_complete) {setPasteColour(225,197,143);pasteString(342,452,"CHAPTER 1 COMPLETE");} setPasteColour (96,110,125); pasteString (18,462,"F1 Help   F5 Save   F7 Load"); statusText (verb); }')
 emit('sub focus (o) { if (! o) o=""; if (busy || choosing) return; if (held) statusText (verb + " " + held + " with " + o); else statusText (verb + " " + o); }')
 emit('sub place (i) {')
 dims={}
 for o in objects:
  if not o['asset']:continue
  im=Image.open(A/(o['asset']+'.png')).convert('RGBA').resize((int(o['width']),int(o['height'])),Image.Resampling.NEAREST)
  if o.get('flip'):im=im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
  tint=ImageColor.getrgb(o.get('tint','#ffffff'));pix=im.load()
  for y in range(im.height):
   for x in range(im.width):
    r,g,b,a=pix[x,y];pix[x,y]=(r*tint[0]//255,g*tint[1]//255,b*tint[2]//255,int(a*o.get('opacity',100)/100))
  if o.get('rotation'):im=im.rotate(-float(o['rotation']),resample=Image.Resampling.NEAREST,expand=True)
  bank(G/(o['id']+'.duc'),im,(im.width//2,im.height));dims[o['id']]=im.size
  i=ix[o['id']]
  emit('if (i == '+str(i)+') { jumpCharacter ('+o['id']+',px['+str(i)+']+'+str(int(o['width'])//2)+',py['+str(i)+']+'+str((int(o['height'])+im.height)//2)+'); return; }')
 emit('}')
 emit('sub enterRoom (n) { removeAllCharacters (); removeAllScreenRegions (); setFloor (NULL); roomNow = n;')
 for ri,r in enumerate(p['rooms']):
  tris=[t for poly in r['floor'] for t in floor_polygons(poly)]
  (G/(r['id']+'.flo')).write_text('\n'.join('* '+ '; '.join(f'{x},{y}' for x,y in t) for t in tris)+'\n')
  emit('if (n == '+str(ri)+') { addOverlay (\'../assets/'+r['background']+'.tga\',0,0); setFloor (\''+r['id']+'.flo\');')
  for o in sorted(r['objects'],key=lambda o:o['layer']):
   i=ix[o['id']];id=o['id']
   emit('if (! inInventory('+id+')) {')
   if o['asset']:
    w,h=dims[id];w=int(o['width']);h=(int(o['height'])+h)//2;emit('addCharacter ('+id+',px['+str(i)+']+'+str(w//2)+',py['+str(i)+']+'+str(h)+',costume(anim(\''+id+'.duc\',0),anim(\''+id+'.duc\',0),anim(\''+id+'.duc\',0))); setCharacterExtra ('+id+', FIXEDSIZE + RECTANGULAR'+(' + FRONT' if o['layer']>0 else '')+');')
   else:emit('addScreenRegion ('+id+','+','.join(map(str,[o['x'],o['y'],o['x']+o['width'],o['y']+o['height'],*o['walk']]))+',SOUTH);')
   emit('}')
  emit('addCharacter (sisko,'+','.join(map(str,r['spawn']))+',egoCostume()); setCharacterWalkSpeed (sisko,'+str(s['walkSpeed'])+'); setPasteColour (165,154,133); pasteString (18,10,'+q(r['name'])+'); }')
 emit('gui (); }')
 emit('sub hit(x,y) { var o=NULL;')
 for ri,r in enumerate(p['rooms']):
  emit('if(roomNow=='+str(ri)+') {')
  for ob in sorted(r['objects'],key=lambda ob:(ob['layer'],ob['y']+ob['height'])):
   i=ix[ob['id']];emit('if((inInventory('+ob['id']+') == FALSE) && x>=px['+str(i)+'] && x<=px['+str(i)+']+'+str(ob['width'])+' && y>=py['+str(i)+'] && y<=py['+str(i)+']+'+str(ob['height'])+') o='+ob['id']+';')
  emit('}')
 emit('return o; }')
 emit('sub click () { if (busy) { skipSpeech (); return; } var x = getMouseX (); var y = getMouseY (); var o = hit(x,y); if (choosing) { if (y >= 376) choice = (y-376)/22; return; }')
 emit('if (y >= 386 && y < 454 && x < 320) { var c = (x-18)/101; var r = (y-386)/24; var vs = newStack("Give","Open","Close","Pick up","Look at","Talk to","Use","Push","Pull"); if (c >= 0 && c < 3 && r >= 0 && r < 3) verb = vs[r*3+c]; focus (NULL); return; } if (y >= 430 && y < 452 && x >= 570 && stackSize(inventory)>2) {invOffset=invOffset+2;if(invOffset>=stackSize(inventory))invOffset=0;gui();return;} if (y >= 407 && y < 450 && x >= 330) {var ii=invOffset+(y-407)/21; if(ii<stackSize(inventory)) { held=inventory[ii]; if(verb=="Look at") {busy=1;act(held);busy=0;held=NULL;} else if(verb!="Give") verb="Use"; focus(NULL);}return; } if (y >= 350) return; if (! o) { held = NULL; verb = "Walk to"; stopCharacter(sisko); moveCharacter (sisko,x,y); return; } busy=1; stopCharacter(sisko);')
 for o in objects:emit('if (o == '+o['id']+') moveCharacter(sisko,'+','.join(map(str,o['walk']))+');')
 emit('act (o); held = NULL; verb = "Walk to"; busy=0; gui (); }')
 emit('sub rightClick () { if (busy) { skipSpeech(); return; } verb="Look at"; click (); }')
 emit('sub act (o) {')
 roomids={r['id']:i for i,r in enumerate(p['rooms'])}
 def condition(fs):return ' && '.join(('! ' if f.startswith('!') else '')+'f_'+f.lstrip('!') for f in fs) or 'TRUE'
 for o in objects:
  emit('if (o == '+o['id']+') {')
  if o.get('pickup'):emit('if(verb=="Pick up") {if(! inInventory(o)) {enqueue(inventory,o);removeCharacter(o);} '+('f_has_padd=1;' if o['id']=='padd' else '')+' return;}')
  if o.get('exit'):emit('if (verb == "Walk to" || verb == "Open" || verb == "Use") { enterRoom ('+str(roomids[o['exit']])+'); return; }')
  for a in o['actions']:
   v='Use' if a['verb']=='Use item' else a['verb'];cond='verb == '+q(v)
   if a.get('item'):cond+=' && held == '+a['item']
   elif v in ['Use','Give']:cond+=' && held == NULL'
   cond+=' && ('+condition(a.get('requires',[]))+')'
   emit('if ('+cond+') {')
   if a.get('text'):emit('say ('+a.get('speaker','sisko')+','+q(a['text'])+');')
   for f in a.get('sets',[]):emit('f_'+f+'=1;')
   if o.get('pickup') and v=='Pick up':emit('removeCharacter ('+o['id']+');')
   if a.get('dialogue'):emit('dialogue_'+a['dialogue']+'();')
   emit('return; }')
  emit('}')
 emit('if (verb == "Walk to") return; if (verb == "Talk to") say(sisko,"There is no one to speak to there."); else if (verb == "Pick up") say(sisko,"I have no reason to carry that."); else if (verb == "Open" || verb == "Close") say(sisko,"There is nothing to open or close there."); else if (verb == "Push" || verb == "Pull") say(sisko,"Moving that would accomplish very little."); else if (verb == "Give") say(sisko,"I should choose an item and someone who needs it."); else say(sisko,"That does not help with the assessment."); }')
 for key,choices in p['dialogues'].items():
  emit('sub dialogue_'+key+' () { var options=newStack(); setBlankColour(10,15,22); blankArea(0,352,639,479); setPasteColour(208,188,149); var row=0;')
  for i,c in enumerate(choices):emit('if ('+condition(c.get('requires',[]))+') { pushToStack(options,'+str(i)+'); pasteString(18,376+row*22,'+q(c['label'])+'); row++; }')
  # pushToStack adds start; use enqueue instead (pushToStack documentation confirms below).
  emit('choice=-1; busy=0; choosing=1; while (choice < 0 || choice >= stackSize(options)) { pause(1); } choosing=0; busy=1; var pick=options[choice];')
  for i,c in enumerate(choices):
   emit('if(pick=='+str(i)+') {')
   for speaker,line in c['lines']:emit('say('+speaker+','+q(line)+');')
   for f in c.get('sets',[]):emit('f_'+f+'=1;')
   emit('}')
  emit('if(f_complete) { say(sisko,"I have set something in motion. For now, I am calling it an inquiry."); gui(); setPasteColour(225,197,143); pasteString(350,444,"CHAPTER 1 COMPLETE"); saveGame("chapter1-complete.sav"); } }')
 emit('sub keys (k) { if (busy) { if (k==" " || k=="ESCAPE") skipSpeech(); return; } if (choosing) return; if(k=="F5") { saveGame("chapter1.sav"); statusText("Game saved."); } if(k=="F7") { if(fileExists("chapter1.sav")) loadGame("chapter1.sav"); else statusText("No saved game yet."); } if(k=="F1") { busy=1; say(sisko,"Choose a verb, then an object. Click the floor to walk. Right-click examines. Click during speech to advance."); if(!f_read_report) say(sisko,"The secure terminal in my office has the casualty report."); else if(!f_strategy) say(sisko,"The strategic display in Ops will show where the pressure is coming from."); else if(!f_has_padd) say(sisko,"My PADD is on the desk in my office."); else if(!f_assessment) say(sisko,"Use the command PADD with the strategic display in Ops."); else if(!f_complete) say(sisko,"Garak may have contacts who can help. I should speak to him."); else say(sisko,"Chapter one is complete. Garak will begin his inquiries in chapter two."); busy=0; } }')
 # Append, rather than prepend, visible choices.
 source='\n'.join(chunks).replace('pushToStack(options,','enqueue(options,')
 source=re.sub(r'!(?=[A-Za-z])', '! ', source)
 (G/'main.slu').write_text(source)
 (G/'chapter1.slp').write_text('[SETTINGS]\nwindowname=In the Pale Moonlight - Chapter 1\nfinalfile=chapter1\nlanguage=English\ndatafolder=PaleMoonlightChapter1\nquitmessage=Leave the station?\nmouse=1\nfullscreen=N\nmakesilent=N\nshowlogo=N\nshowloading=N\ninvisible=N\nditherimages=N\nwidth=640\nheight=480\nspeed=30\n\n[FILES]\nmain.slu\n')
 compiler=os.environ.get('SLUDGE_COMPILER') or shutil.which('sludge-compiler')
 if not compiler:raise RuntimeError('Install sludge-compiler or set SLUDGE_COMPILER to its executable path')
 result=subprocess.run([compiler,str(G/'chapter1.slp')],cwd=G,capture_output=True,text=True)
 print(result.stdout); print(result.stderr)
 if result.returncode or not (G/'chapter1.slg').exists():raise RuntimeError('SLUDGE compilation failed')
 print('Built',G/'chapter1.slg',(G/'chapter1.slg').stat().st_size,'bytes')
if __name__=='__main__':build()
