#!/usr/bin/env python3
"""Same-origin static host and bounded, per-session native-game compiler."""
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
from pathlib import Path
from urllib.parse import urlparse
import json,os,sys,subprocess,uuid,threading,shutil
ROOT=Path(__file__).resolve().parent;WEB=ROOT/'web';BUILDS=WEB/'builds';LOCK=threading.Lock();BUILDS.mkdir(exist_ok=True)
def validate(c):
 if not isinstance(c,dict):raise ValueError('A scene JSON object is required.')
 for key,lo,hi in [('floor_y',100,118),('ceiling_y',25,38),('character_scale',.35,.95),('walk_speed',1,12),('tunnel_delay',2,60),('gold_tokens',0,9999)]:
  v=c.get(key)
  if type(v) not in (int,float) or not lo<=v<=hi:raise ValueError(f'{key} must be between {lo} and {hi}.')
 if len(c.get('palette',[]))!=64:raise ValueError('Exactly 64 palette colors are required.')
 for p in c['palette']:
  if not isinstance(p,str) or len(p)!=6:raise ValueError('Palette colors must be six hex digits.')
  bytes.fromhex(p)
 if not 1<=len(c.get('windows',[]))<=16:raise ValueError('Use 1–16 window rectangles.')
 for w in c['windows']:
  if len(w)!=4 or any(type(n)!=int for n in w) or not(0<=w[0]<w[2]<=319 and 39<=w[1]<w[3]<=105):raise ValueError('Window bounds are invalid.')
 for group in ['inventory_names','descriptions','use_text']:
  for key in ['wallet','tokens','card']:
   s=c.get(group,{}).get(key)
   if not isinstance(s,str) or len(s)>140 or any(ord(ch)<32 or ord(ch)>126 or ch in '\\"' for ch in s):raise ValueError('Text must be 0–140 printable ASCII characters, without double quotes or backslashes.')
 return c
class Handler(SimpleHTTPRequestHandler):
 def __init__(self,*a,**kw):super().__init__(*a,directory=str(WEB),**kw)
 def end_headers(self):
  self.send_header('X-Content-Type-Options','nosniff');self.send_header('Cache-Control','no-cache');super().end_headers()
 def do_GET(self):
  if urlparse(self.path).path=='/health':self.reply(200,{'ok':True});return
  return super().do_GET()
 def reply(self,status,obj):
  b=json.dumps(obj).encode();self.send_response(status);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(b)));self.end_headers();self.wfile.write(b)
 def do_POST(self):
  if self.path!='/api/build':self.reply(404,{'error':'Not found'});return
  origin=self.headers.get('Origin')
  if origin and urlparse(origin).netloc!=self.headers.get('Host'):self.reply(403,{'error':'Same-origin requests only'});return
  try:
   size=int(self.headers.get('Content-Length','0'))
   if not 0<size<=16000:raise ValueError('Scene JSON must be under 16 KB.')
   c=validate(json.loads(self.rfile.read(size)))
  except (ValueError,TypeError,KeyError,AttributeError) as e:self.reply(400,{'error':str(e)});return
  if not LOCK.acquire(False):self.reply(429,{'error':'Another scene is compiling. Try again shortly.'});return
  ident=uuid.uuid4().hex;dest=BUILDS/ident
  try:
   old=sorted(BUILDS.iterdir(),key=lambda p:p.stat().st_mtime)
   for p in old[:-16]:shutil.rmtree(p,ignore_errors=True)
   dest.mkdir();cfg=dest/'project.json';cfg.write_text(json.dumps(c))
   p=subprocess.run([sys.executable,str(ROOT/'tools/build_game.py'),str(dest/'game'),str(cfg)],capture_output=True,text=True,timeout=20)
   if p.returncode:raise ValueError('Native compilation failed. The current ride is unchanged.')
   self.reply(200,{'id':ident,'build':json.loads((dest/'game/build.json').read_text())})
  except (ValueError,subprocess.TimeoutExpired) as e:shutil.rmtree(dest,ignore_errors=True);self.reply(422,{'error':str(e)})
  finally:LOCK.release()
if __name__=='__main__':ThreadingHTTPServer(('0.0.0.0',int(os.environ.get('PORT','8000'))),Handler).serve_forever()
