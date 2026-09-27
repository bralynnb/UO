"""Physical browser inputs, bounds assertions and retained image evidence."""
from playwright.sync_api import sync_playwright
from pathlib import Path
import json,os
OUT=Path('test-results');OUT.mkdir(exist_ok=True);url=os.environ.get('TEST_URL','http://127.0.0.1:8000');logs=[];errors=[];requests=[];results=[]
with sync_playwright() as p:
 browser=p.chromium.launch(headless=True,args=['--no-sandbox']);page=browser.new_page(viewport={'width':1024,'height':720})
 page.on('console',lambda m:logs.append(m.type+': '+m.text));page.on('pageerror',lambda e:errors.append(str(e)));page.on('requestfailed',lambda r:requests.append(r.url+' '+str(r.failure)));page.on('response',lambda r:requests.append(f'{r.status} {r.url}') if r.status>=400 else None)
 try:
  page.goto(url,wait_until='domcontentloaded');frame=page.frame_locator('#game');frame.locator('#start').click();frame.locator('#loading').wait_for(state='hidden',timeout=100000);page.wait_for_timeout(600);results.append('Actual SCUMM room 2 started');game=page.frames[1]
  metrics=game.evaluate('({buffer:[canvas.width,canvas.height],rect:canvas.getBoundingClientRect().toJSON(),viewport:[innerWidth,innerHeight]})');(OUT/'metrics.json').write_text(json.dumps(metrics,indent=2));r=metrics['rect'];assert r['y']>=-1 and r['bottom']<=metrics['viewport'][1]+1,metrics;results.append('Entire canvas including verb panel fits viewport');page.screenshot(path=str(OUT/'01-start.png'))
  def click(x,y,button='left'):
   b=frame.locator('#canvas').bounding_box();scale=min(b['width']/320,b['height']/200);page.mouse.click(b['x']+(b['width']-320*scale)/2+x*scale,b['y']+(b['height']-200*scale)/2+y*scale,button=button);page.wait_for_timeout(180)
  def event(s):game.wait_for_function('(s)=>gameEvents.some(x=>x.includes(s))',arg=s,timeout=20000)
  click(317,70);event('TSFM_CAR=3');page.wait_for_timeout(300);page.screenshot(path=str(OUT/'02-car3.png'));results.append('Walk from car 2 to car 3')
  click(2,70);page.wait_for_timeout(5000);click(2,70);event('TSFM_CAR=1');page.screenshot(path=str(OUT/'03-car1.png'));results.append('Walk back through car 2 to car 1')
  for name,x in [('wallet',176),('tokens',222),('card',268)]:
   click(65,177);click(x,167);page.wait_for_timeout(300);page.screenshot(path=str(OUT/('04-'+name+'.png')));page.wait_for_timeout(2500)
  results.append('Three inventory Look at actions exercised; screenshots retained')
  page.locator('#editButton').click();page.locator('#speed').fill('6');page.locator('#speed').dispatch_event('change');page.locator('#apply').click();page.locator('#editor').wait_for(state='hidden',timeout=40000);frame=page.frame_locator('#game');frame.locator('#start').click();frame.locator('#loading').wait_for(state='hidden',timeout=100000);page.wait_for_timeout(500);page.screenshot(path=str(OUT/'05-rebuilt.png'));results.append('Editor rebuilt native game and it restarted')
  bad=page.request.get(url+'/project.json').json();bad['character_scale']=3;r=page.request.post(url+'/api/build',data=bad);assert r.status==400;results.append('Invalid scale rejected');assert not errors,errors;(OUT/'PASS').write_text('\n'.join(results))
 finally:
  page.screenshot(path=str(OUT/'last.png'));(OUT/'console.json').write_text(json.dumps(logs,indent=2));(OUT/'errors.json').write_text(json.dumps(errors,indent=2));(OUT/'requests.json').write_text(json.dumps(requests,indent=2));(OUT/'checks.json').write_text(json.dumps(results,indent=2));browser.close()
