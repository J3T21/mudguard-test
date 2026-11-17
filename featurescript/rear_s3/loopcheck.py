import re, math, collections
from s3geom import *
src=open('rear_s3_coupons.fs').read()
# parse s3Prof blocks
blocks={}
for m in re.finditer(r'if \(oi == (\d)\)\s*return \{ "out" : (\[.*?\]),\s*"inn" : (\[.*?\]),\s*"c" : ([\d.]+)', src, re.S):
    blocks[int(m.group(1))]=dict(out=eval(m.group(2)),inn=eval(m.group(3)),c=float(m.group(4)))
assert sorted(blocks)==[1,2,3], blocks.keys()
def P(x,z,sx,dx): return (round(sx*(x+dx),6),round(z,6))
def chain(rows,sx,dx):
    segs=[]
    for g in rows:
        segs.append((P(g[1],g[2],sx,dx),P(g[5],g[6],sx,dx), g[0], P(g[3],g[4],sx,dx)))
    return segs
def closed(segs,label):
    cnt=collections.Counter()
    for a,b,_,_ in segs: cnt[a]+=1; cnt[b]+=1
    bad={k:v for k,v in cnt.items() if v!=2}
    if bad: print("OPEN LOOP",label,bad); return False
    return True
def arc_ok(segs,label):
    ok=True
    for a,b,t,m in segs:
        if t==1:
            # mid strictly between and not collinear
            ax,ay=a;bx,by=m;cx,cy=b
            cr=(bx-ax)*(cy-ay)-(by-ay)*(cx-ax)
            if abs(cr)<1e-6: print("degenerate arc",label); ok=False
    return ok
def last(rows): g=rows[-1]; return (g[5],g[6])
allok=True
dxs=[0,DX[2],DX[3],DX[4],DX[5]]
for oi,B in blocks.items():
    # continuity within chains
    for nm in ("out","inn"):
        rows=B[nm]
        for i in range(len(rows)-1):
            if (rows[i][5],rows[i][6])!=(rows[i+1][1],rows[i+1][2]): print("chain gap",oi,nm,i); allok=False
    for dx in dxs:
        c=B['c']+dx
        segs=[((round(-c,6),380.0),(round(c,6),380.0),0,None),((round(-c,6),377.0),(round(c,6),377.0),0,None)]
        for sx in (-1,1):
            segs+=chain(B['out'],sx,dx)+chain(B['inn'],sx,dx)
            po=last(B['out']); pq=last(B['inn'])
            segs.append((P(po[0],po[1],sx,dx),P(pq[0],pq[1],sx,dx),0,None))
        allok&=closed(segs,("shell",oi,dx)) and arc_ok(segs,("shell",oi,dx))
        for xa in (-3.0,-4.5):
            for sx in (-1,1):
                xc=round(sx*(B['c']+dx),6)
                segs=[((round(sx*xa,6),380.0),(xc,380.0),0,None),((round(sx*xa,6),377.0),(xc,377.0),0,None),((round(sx*xa,6),377.0),(round(sx*xa,6),380.0),0,None)]
                segs+=chain(B['out'],sx,dx)+chain(B['inn'],sx,dx)
                po=last(B['out']); pq=last(B['inn'])
                segs.append((P(po[0],po[1],sx,dx),P(pq[0],pq[1],sx,dx),0,None))
                allok&=closed(segs,("half",oi,dx,xa,sx))
    dx=DX[5]; c=B['c']+dx; po=last(B['out']); xo=po[0]+dx; zb=math.sqrt(358*358-xo*xo)
    segs=[((round(-c,6),380.0),(round(c,6),380.0),0,None),((round(-xo,6),round(zb,6)),(round(xo,6),round(zb,6)),1,(0,358))]
    for sx in (-1,1):
        segs+=chain(B['out'],sx,dx); segs.append((P(po[0],po[1],sx,dx),(round(sx*xo,6),round(zb,6)),0,None))
    allok&=closed(segs,("end",oi))
print("ALL LOOPS CLOSED" if allok else "PROBLEM")
# segment floor-start conditions
for (a,b),xa in {(10,14):-3.0,(14,18):-3.0,(18,22):-3.0,(22,25.5):-4.5}.items():
    da=dxs[[10,14,18,22,25.5].index(a)]; db=dxs[[10,14,18,22,25.5].index(b)]
    print(f"seg {a}-{b}: start {xa} (other-half corner at {-(4+da):.3f}, margin {xa+4+da:.3f}); end {xa+db-da:.3f} (overlap width {2*-(xa+db-da):.3f}, corner {-(4+db):.3f})")
