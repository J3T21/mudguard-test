import numpy as np, math, json
from s3vox import *
def mem(cp,p):
    return bool(cp.member(np.array([p[0]]),p[1],np.array([p[2]]))[0])
def probes(cp):
    k=cp.k; w=k['w']; hc=k['hc']; nf=k['nf']; C1=k['C1']; yl=cp.ylip
    hs=hc-1.5*nf
    P=[("floor",(0,0,378.5)),("pocket",(0,0,390)),("wallR",(w+1.57,0,391)),("wallL",(-w-1.57,0,391)),
       ("cavity",(19.5,0,385)),("flank0",(hs[0],0,hs[1])),("holeP",(hs[0],8,hs[1])),("holeN",(-hs[0],-8,hs[1])),
       ("notchN",(C1[0],-8,399.0)),("crestP",(C1[0],8,399.0)),("end2",(0,27,370)),("cav24",(19.5,24,385)),
       ("legP",(10,30.4,370)),("legN",(10,-30.4,370)),("lipP",(-20.5,40,370)),("lipN",(-20.5,-40,370)),
       ("lipEdgeOut",(-20.5,yl+1,370)),("lipEdgeIn",(-20.5,yl-1,370)),("stayHole",(21,32.8,362)),
       ("recess",(0,31.77,362)),("legBehindRecess",(0,32.9,362)),("groove",(-21.6,40,362)),("lipFront",(-21.6,40,370)),
       ("stayVoid",(0,40,370)),("lipGap",(-18.25,40,370)),("web",(0,28.9,367.75)),("beyondEnd",(51,0,368)),("pocketEnd",(0,27,390)),("cupBelow",(10,34.75,355.5)),("cupBottom",(10,34.75,356.5))]
    return P
out={}
for name,o,yl in (("A",0.5,43.0),("B",1.0,43.0),("C",1.5,43.0),("D",1.0,47.0)):
    cp=Coupon(o,yl); res=[]; bad=[]
    for lab,p in probes(cp):
        m=mem(cp,p); res.append((lab,[round(v,3) for v in p],int(m)))
        for ax in range(3):
            for sg in (-0.4,0.4):
                q=list(p); q[ax]+=sg
                if mem(cp,q)!=m: bad.append((lab,ax,sg))
    out[name]=res
    print(name,"".join(str(r[2]) for r in res),"NOT ROBUST:" if bad else "robust",bad)
json.dump(out,open('probes.json','w'),indent=0)
