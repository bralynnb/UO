"""Local-only authoring studio. Python 3, Pillow and sludge-compiler required."""
from http.server import ThreadingHTTPServer,SimpleHTTPRequestHandler
from pathlib import Path
import json,subprocess,tempfile,shutil,os,threading,sys,webbrowser
R=Path(__file__).resolve().parents[1]; LOCK=threading.Lock()
class Handler(SimpleHTTPRequestHandler):
 def __init__(self,*a,**kw):super().__init__(*a,directory=str(R),**kw)
 def do_GET(self):
  if self.path=='/':self.path='/editor/index.html'
  super().do_GET()
 def reply(self,code,value):
  data=json.dumps(value).encode();self.send_response(code);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(data)));self.end_headers();self.wfile.write(data)
 def do_POST(self):
  if self.path!='/api/build':return self.reply(404,{'error':'Unknown endpoint'})
  origin=self.headers.get('Origin')
  if origin and origin not in ['http://127.0.0.1:8765','http://localhost:8765']:return self.reply(403,{'error':'Only the local editor may build'})
  if self.headers.get_content_type()!='application/json':return self.reply(415,{'error':'Expected JSON'})
  try:
   size=int(self.headers.get('Content-Length','0'))
   if not 0<size<2_000_000:raise ValueError('Project too large or empty')
   data=json.loads(self.rfile.read(size))
   with LOCK,tempfile.TemporaryDirectory(prefix='moonlight-build-') as tmp:
    stage=Path(tmp)
    for folder in ['assets','tools']:shutil.copytree(R/folder,stage/folder)
    (stage/'project.json').write_text(json.dumps(data,indent=2)+'\n')
    run=subprocess.run([sys.executable,str(stage/'tools/build.py')],capture_output=True,text=True,timeout=60)
    if run.returncode:raise ValueError((run.stdout+run.stderr)[-5000:])
    shutil.copy2(R/'project.json',R/'project.previous.json');os.replace(stage/'project.json',R/'project.json')
    for file in (stage/'game').iterdir():shutil.copy2(file,R/'game'/file.name)
   self.reply(200,{'bytes':(R/'game/chapter1.slg').stat().st_size})
  except Exception as e:self.reply(400,{'error':str(e)})
if __name__=='__main__':
 print('Pale Moonlight Studio: http://127.0.0.1:8765')
 if '--no-browser' not in sys.argv:webbrowser.open('http://127.0.0.1:8765')
 ThreadingHTTPServer(('127.0.0.1',8765),Handler).serve_forever()
