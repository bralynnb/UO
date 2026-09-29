"""Initial chapter data. Run once; subsequent authoring uses project.json."""
import json
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def rule(verb,text='',requires=None,sets=None,**kw):
 return dict(verb=verb,text=text,requires=requires or [],sets=sets or [],**kw)
def obj(id,name,asset,x,y,w,h,look,**kw):
 return dict(id=id,name=name,asset=asset,x=x,y=y,width=w,height=h,rotation=0,flip=False,tint='#ffffff',opacity=100,walk=[min(600,x+w//2),320],layer=0,actions=[rule('Look at',look)],**kw)
desk=obj('desk','Sisko\'s desk','desk',195,224,200,88,'A clear desk. That used to mean the day was over.')
terminal=obj('terminal','secure terminal','terminal',294,192,50,54,'A secure link to Starfleet. The casualty report is waiting.')
terminal['actions'] += [rule('Use','Another list of ships that will not return. I need the strategic picture, not another projection.',sets=['read_report']),rule('Open','The casualty report is already on the screen. Use the terminal to review it.')]
padd=obj('padd','command PADD','padd',228,236,26,32,'My command PADD. I can take the assessment with me.',pickup=True)
padd['actions'] += [rule('Pick up','',sets=['has_padd']),rule('Use','I need to review the strategic display in Ops before I can prepare an assessment.',requires=['!strategy']),rule('Use','The assessment is ready. The Romulans are the missing factor.',requires=['strategy'])]
baseball=obj('baseball','baseball','baseball',361,214,26,30,'A game has rules. A war is less obliging.')
baseball['actions'] += [rule('Pick up','It belongs here. A reminder that there is something beyond this office.')]
chair=obj('chair','chair','chair',226,174,68,86,'I have spent enough time sitting with these reports.')
plant=obj('plant','Bajoran plant','plant',121,176,50,78,'Someone still finds time to keep things alive.')
office_exit=obj('office_exit','door to Ops','',524,87,90,152,'The door opens onto Ops.',exit='ops')
office_exit['walk']=[553,267]
display=obj('display','strategic display','console',246,175,136,80,'Federation and Klingon positions. Too many retreating lines.')
display['actions'] += [rule('Use','The Romulan border remains quiet. The Dominion can move forces elsewhere without fearing a second front.',requires=['read_report'],sets=['strategy']),rule('Use','First I should review the current casualty report in my office.',requires=['!read_report']),rule('Use item','I have recorded the assessment. A neutral Romulus is a luxury our enemy can afford. We cannot.',requires=['strategy','has_padd'],sets=['assessment'],item='padd')]
office_door=obj('office_door','captain\'s office','',20,108,88,111,'My office.',exit='office');office_door['walk']=[91,275]
lift=obj('lift','turbolift to the Promenade','',538,91,82,145,'Garak keeps his shop on the Promenade.',exit='shop');lift['walk']=[555,284]
opsconsole=obj('opsconsole','duty console','console',57,236,136,80,'Shift reports, repair schedules, and requests for ships we do not have.')
table=obj('table','cutting table','table',254,219,158,76,'Every tool has its place. That seems important to Garak.')
fabric=obj('fabric','folded fabric','fabric',304,217,56,36,'Muted cloth, carefully folded. Garak always notices details.')
garak=obj('garak','Garak','garak',405,171,46,102,'A tailor with a talent for knowing what people leave unsaid.',npc=True)
garak['walk']=[384,303]
garak['actions'] += [rule('Talk to','You have the expression of a man about to ask for something difficult.',speaker='garak',dialogue='garak'),rule('Give','A useful assessment, Captain. But I suspect this visit is about what is missing from it.',requires=['assessment'],item='padd',speaker='garak',dialogue='garak'),rule('Give','An empty PADD is a rather abstract commission, Captain.',requires=['!assessment'],item='padd',speaker='garak')]
shop_exit=obj('shop_exit','door to the Promenade','',38,93,76,139,'The turbolift will take me back to Ops.',exit='ops');shop_exit['walk']=[101,278]
p={'title':'In the Pale Moonlight — Chapter 1','schema':1,'settings':{'walkSpeed':5,'speechSpeed':65,'horizon':100,'scaleDivide':220},'flags':['read_report','strategy','has_padd','assessment','complete'],'rooms':[
 {'id':'office','name':'CAPTAIN\'S OFFICE','background':'office','spawn':[460,322],'floor':[[[32,265],[201,265],[201,310],[401,310],[401,263],[608,263],[625,346],[15,346]]],'objects':[chair,plant,desk,terminal,padd,baseball,office_exit]},
 {'id':'ops','name':'OPERATIONS','background':'ops','spawn':[108,331],'floor':[[[27,318],[194,318],[194,274],[244,259],[405,259],[443,263],[611,261],[625,346],[15,346]]],'objects':[display,opsconsole,office_door,lift]},
 {'id':'shop','name':'GARAK\'S CLOTHIERS','background':'shop','spawn':[123,321],'floor':[[[26,259],[240,259],[240,302],[423,302],[423,267],[610,267],[625,346],[15,346]]],'objects':[table,fabric,garak,shop_exit]}
 ],'dialogues':{'garak':[
 {'label':'Can your old contacts obtain Dominion intelligence?','requires':['assessment'],'lines':[['sisko','I need evidence of the Dominion\'s intentions toward Romulus. Evidence their government will believe.'],['garak','My former acquaintances are not accustomed to requests from Starfleet captains.'],['sisko','Then ask as a tailor. I need to know whether there is a way.'],['garak','I can make inquiries. I would advise you to decide how far you are prepared to follow the answers.']],'sets':['complete']},
 {'label':'Why keep a shop open during a war?','lines':[['garak','Because people continue to tear their sleeves, Captain. Some problems remain pleasingly simple.']]},
 {'label':'I need to finish my assessment.','requires':['!assessment'],'lines':[['sisko','I should bring you something more useful than speculation.'],['garak','An excellent habit. I hope you will be able to maintain it.']]},
 {'label':'That is all for now.','lines':[]}
 ]},'chapters':[
 {'number':1,'title':'The Quiet Border','status':'playable','premise':'Review the losses, prepare a strategic assessment, and ask Garak to investigate.'},
 {'number':2,'title':'Old Acquaintances','status':'planned','premise':'The search for reliable intelligence tests the limits of conventional methods.'},
 {'number':3,'title':'An Unwelcome Proposal','status':'planned','premise':'Sisko weighs a different route to Romulan involvement.'},
 {'number':4,'title':'The Specialist','status':'planned','premise':'An unreliable specialist becomes part of the undertaking.'},
 {'number':5,'title':'The Price of Cooperation','status':'planned','premise':'Private arrangements create public consequences on the station.'},
 {'number':6,'title':'A Convincing Record','status':'planned','premise':'The operation requires something that can withstand scrutiny.'},
 {'number':7,'title':'Necessary Supplies','status':'planned','premise':'Sisko confronts the cost of obtaining what the operation needs.'},
 {'number':8,'title':'The Visitor','status':'planned','premise':'A Romulan visitor puts the undertaking to its decisive test.'},
 {'number':9,'title':'The Consequence','status':'planned','premise':'Events move beyond Sisko\'s direct control.'},
 {'number':10,'title':'The Private Record','status':'planned','premise':'The political outcome leaves Sisko to confront his own responsibility.'}
 ]}
(R/'project.json').write_text(json.dumps(p,indent=2)+'\n')
