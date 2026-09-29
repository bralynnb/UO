"""Original, reproducible pixel artwork. No extracted television/game assets."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import random, math, struct, io
ROOT=Path(__file__).resolve().parents[1]; A=ROOT/'assets'; A.mkdir(exist_ok=True)
random.seed(47)
def bank(path, sprites, hotspots=None):
    with open(path,'wb') as f:
        f.write(b'\0\0\3'+struct.pack('>H',len(sprites)))
        for i,im in enumerate(sprites):
            f.write(struct.pack('<hh',*(hotspots[i] if hotspots else (0,0))))
            b=io.BytesIO(); im.convert('RGBA').save(b,format='PNG'); f.write(b.getvalue()[8:])
def save(im,name):
    im=im.resize((640,352),Image.Resampling.NEAREST);im.save(A/(name+'.png')); im.convert('RGB').save(A/(name+'.tga'))
def room(kind):
    im=Image.new('RGB',(320,176)); p=im.load()
    for y in range(176):
        for x in range(320):
            light=max(0,1-abs(x-160)/190);n=random.choice([-2,-1,0,0,1,2]);
            base=(20,24,29) if y<117 else (38,36,38)
            p[x,y]=tuple(max(0,int(c*light)+n) for c in base)
    d=ImageDraw.Draw(im)
    d.polygon([(0,118),(320,118),(320,176),(0,176)],fill='#29292e')
    for y in [125,139,157,175]:d.line((0,y,320,y),fill='#39383c')
    for x in range(-150,500,60):d.line((160+(x-160)*.38,118,x,176),fill='#191d24')
    d.line((0,117,320,117),fill='#807468',width=2)
    # Cardassian structural ribs, double bevels and recessed amber lamps.
    for x in [5,67,252,314]:
        d.polygon([(x-8,0),(x+8,0),(x+4,100),(x-5,120),(x-15,120),(x-5,98)],fill='#342f2d')
        d.line([(x+5,0),(x+1,97),(x-8,116)],fill='#75665b',width=2)
        d.line([(x-1,0),(x-5,92)],fill='#191d22',width=2)
        d.rectangle((x-3,34,x,62),fill='#b98c57')
        d.line((x-2,36,x-2,60),fill='#edd49a')
    d.polygon([(0,0),(320,0),(280,17),(40,17)],fill='#302d2d')
    d.line((40,17,280,17),fill='#6b5c4d')
    def stars(box):
        d.rectangle(box,fill='#090f1b')
        for _ in range(55):
            x=random.randint(box[0]+2,box[2]-2);y=random.randint(box[1]+2,box[3]-2)
            d.point((x,y),fill=random.choice(['#697582','#aab5b9','#364c64']))
    if kind=='office':
        stars((88,25,233,94))
        for x in [87,122,159,197,233]:
            d.line((x,24,x,95),fill='#746757',width=3);d.line((x+2,25,x+2,95),fill='#252a2d')
        d.rectangle((84,94,237,104),fill='#4b4138');d.line((85,95,236,95),fill='#88725b')
        # Left cabinet / baseball display recess, right door.
        d.rectangle((22,64,57,111),fill='#111b21');d.rectangle((25,67,54,109),outline='#76695b')
        for y in [83,97]:d.line((25,y,54,y),fill='#5b5049')
        d.polygon([(271,44),(296,44),(305,58),(305,116),(263,116),(263,58)],fill='#0f151a')
        for x in [267,283,300]:d.line((x,58,x,115),fill='#544d48',width=2)
        d.line((271,44,296,44),fill='#ae9574',width=2)
        d.rectangle((245,79,252,96),fill='#0a1219')
        for y in range(82,96,4):d.line((246,y,250,y),fill='#bd925a')
        # wall inset ribs
        for y in [30,36,42,48]:d.line((23,y,54,y),fill='#49413a')
    elif kind=='ops':
        stars((105,23,215,69))
        for x in [105,141,178,215]:d.line((x,23,x,70),fill='#796b56',width=3)
        d.polygon([(94,76),(226,76),(247,113),(74,113)],fill='#302d30')
        d.line((94,77,226,77),fill='#a39176',width=2)
        for x in [12,266]:
            d.rectangle((x,56,x+39,104),fill='#111b25',outline='#82715e',width=2)
            for yy in range(63,100,7):
                d.rectangle((x+5,yy,x+14,yy+2),fill='#b48f60')
                d.line((x+18,yy,x+32,yy),fill='#5e91a5')
        for y in [115,120,126]:d.line((64,y,255,y),fill='#696056',width=2)
        for x in [61,258]:d.line((x,87,x,143),fill='#8e8170',width=3)
        d.line((61,91,258,91),fill='#8e8170',width=2)
        d.rectangle((276,22,303,42),fill='#171b21',outline='#736553')
    else:
        d.rectangle((84,28,230,101),fill='#282528',outline='#6d5947',width=2)
        for x in range(91,227,16):
            c=random.choice(['#494e4a','#5b4742','#3d4e59','#685442'])
            d.polygon([(x,39),(x+6,36),(x+12,41),(x+11,86),(x+1,86)],fill=c)
            for yy in range(45,83,4):d.line((x+3,yy,x+10,yy),fill='#353335')
        d.line((86,34,229,34),fill='#a08a68',width=2)
        d.rectangle((20,48,55,114),fill='#0e141a',outline='#7e6b53',width=2)
        for x in [27,46]:d.line((x,52,x,111),fill='#3d3835',width=3)
        d.rectangle((264,43,299,108),fill='#443832',outline='#8e7556')
        for yy in range(50,106,14):d.line((265,yy,298,yy),fill='#a58a64')
    save(im,kind)
for r in ['office','ops','shop']:room(r)
def sprite(kind):
    sizes={'desk':(100,44),'terminal':(25,27),'padd':(13,16),'baseball':(13,15),'chair':(34,43),'console':(68,40),'table':(79,38),'fabric':(28,18),'plant':(25,39)}
    im=Image.new('RGBA',sizes[kind]);d=ImageDraw.Draw(im)
    if kind in ['desk','console','table']:
        w,h=im.size;d.polygon([(3,9),(w-15,0),(w-1,14),(w-8,31),(15,h-1),(5,h-10)],fill='#44382f',outline='#a18b70')
        d.polygon([(3,9),(w-15,0),(w-1,14),(19,24)],fill='#6b5742')
        d.line((20,26,w-7,17),fill='#2b2524',width=2)
        if kind=='console':
            d.polygon([(9,8),(w-17,3),(w-9,13),(19,18)],fill='#182935')
            for x in range(16,w-18,8):d.line((x,9,x+4,9),fill='#dcac6a',width=2)
    elif kind=='terminal':
        d.polygon([(2,2),(23,0),(21,20),(0,23)],fill='#877760');d.polygon([(4,4),(20,3),(18,17),(3,20)],fill='#142a38')
        for y in range(6,16,3):d.line((6,y,16,y-1),fill='#83a6ad');d.rectangle((8,22,19,26),fill='#5a5147')
    elif kind=='padd':
        d.rounded_rectangle((0,0,12,15),radius=2,fill='#968670');d.rectangle((2,2,10,11),fill='#214151')
        for y in [4,7,9]:d.line((3,y,8,y),fill='#91b7b5')
        d.rectangle((3,13,7,13),fill='#d4af70')
    elif kind=='baseball':
        d.rectangle((1,12,11,14),fill='#78624a');d.ellipse((2,1,11,10),fill='#d5ccb0',outline='#a69c88');d.arc((2,1,8,10),-60,60,fill='#9a5548');d.arc((6,1,12,10),120,240,fill='#9a5548')
    elif kind=='chair':
        d.rounded_rectangle((5,0,28,26),radius=5,fill='#4e4945',outline='#8c7a60');d.polygon([(4,24),(29,24),(33,34),(1,34)],fill='#66564a');d.line((9,34,5,42),fill='#928775',width=2);d.line((26,34,29,42),fill='#928775',width=2)
    elif kind=='fabric':
        d.polygon([(1,4),(20,0),(27,13),(8,17)],fill='#6e4c50');
        for x in range(3,22,3):d.line((x,4,x+5,14),fill='#a77a70')
    else:
        d.polygon([(8,29),(21,29),(18,38),(10,38)],fill='#6e5543')
        for x,y in [(4,6),(19,4),(11,0),(2,19),(23,17)]:d.line((15,30,x,y),fill='#67674c',width=2);d.ellipse((x-2,y,x+4,y+12),fill='#555d48')
    im=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST);im.save(A/(kind+'.png'));bank(A/(kind+'.duc'),[im]);return im
for k in ['desk','terminal','padd','baseball','chair','console','table','fabric','plant']:sprite(k)
def person(direction,frame,garak=False):
    im=Image.new('RGBA',(23,51));d=ImageDraw.Draw(im);s=math.sin(frame*math.pi/3) if frame>=0 else 0
    skin='#aa8061' if not garak else '#959887';shade='#694c3e' if not garak else '#626b65'
    d.ellipse((2,47,21,50),fill=(0,0,0,65))
    a=int(s*3);b=-a
    d.line((9,32,8+a,47),fill='#161e29',width=5);d.line((15,32,15+b,47),fill='#202734',width=5)
    d.rectangle((5+a,46,10+a,48),fill='#10151d');d.rectangle((13+b,46,18+b,48),fill='#10151d')
    d.polygon([(6,15),(16,15),(19,24),(17,35),(6,35),(4,24)],fill='#282e39')
    d.polygon([(6,15),(16,15),(18,20),(5,20)],fill='#74767b' if not garak else '#6b685c')
    d.rectangle((10,16,13,23),fill='#8d3940' if not garak else '#4c5a53')
    d.line((5,20,3-b//2,33),fill='#252d39',width=4);d.line((17,20,20+b//2,33),fill='#343944',width=4)
    d.rectangle((2-b//2,32,4-b//2,36),fill=skin);d.rectangle((19+b//2,32,21+b//2,35),fill=skin)
    d.rectangle((10,12,13,16),fill=shade);d.ellipse((7,2,16,14),fill=skin)
    d.arc((7,2,16,14),65,255,fill=shade,width=2)
    if direction!=2:
        d.line((9,8,10,8),fill='#29252b');d.point((14,8),fill='#29252b');d.line((10,12,14,12),fill='#3b3030')
        if not garak:d.rectangle((10,12,14,14),fill='#493930')
        d.point((7,21),fill='#c8bc89')
    if garak:
        d.arc((6,0,18,15),185,355,fill='#313b3f',width=3);d.line((11,3,11,6),fill='#c0b99e');d.line((8,10,10,12),fill='#c0b99e')
    if direction==1:im=im.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    return im.resize((46,102),Image.Resampling.NEAREST)
for name in ['sisko','garak']:
    frames=[person(d,f,name=='garak') for d in range(4) for f in [-1,0,1,2,3,4,5]]
    bank(A/(name+'.duc'),frames,[(23,100)]*len(frames));frames[0].save(A/(name+'.png'))
chars=''.join(chr(i) for i in range(32,127))
fontpath='/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf'
font=ImageFont.truetype(fontpath,12)
glyphs=[]
for c in chars:
    g=Image.new('RGBA',(8,16));ImageDraw.Draw(g).text((0,-1),c,font=font,fill='white',stroke_width=0)
    # Hard edges remain legible at native VGA; no smooth scaling.
    g.putalpha(g.getchannel('A').point(lambda v:255 if v>100 else 0));glyphs.append(g)
bank(A/'font.duc',glyphs)
cur=Image.new('RGBA',(13,17));ImageDraw.Draw(cur).polygon([(0,0),(0,13),(4,10),(7,16),(10,14),(7,9),(12,9)],fill='#dfc18c',outline='#151c25');bank(A/'cursor.duc',[cur])
print('Original backgrounds, layered props, four-direction six-frame walks and font generated.')
