"""S3.1 coupon geometry in TRUE frame: axle at origin, x = fore-aft (s direction at s=0), z up (radial at s=0), y lateral.
Hump section built in the XZ plane with exact lines/arcs (pocket/crest/flank Cartesian as in spec numbers);
root R25 / R27.5 solved tangent to the TRUE crown circles R369.5 / R367 (centre radius 394.5)."""
import math, numpy as np
D2R=math.pi/180
RO=369.5; RI=367.0; TH=3.0; RC=4.0; RR=25.0; THETA=50*D2R; DRAFT=1*D2R; CREST_TOP=399.5
AC=52.0/RO          # closing ray (beyond the s=+-50 cut, before both feet)
HB=[10.0,10.0,10.15,10.63,11.50,14.23]   # hb at y 0,10,14,18,22, and hb(28) used for 25.5..28
YST=[0,10,14,18,22,25.5]
def hbf(y):
    y=abs(y)
    if y<=10: return 10.0
    u=min((y-10)/20,1.0); return 17.5-7.5*math.sqrt(max(0,1-u*u))
DX=[0.0,0.0]+[ (hbf(14)-10),(hbf(18)-10),(hbf(22)-10),(hbf(28)-10)]
def ray_circle(a,C,r):
    # points p=t*(sin a, cos a); |p-C|=r ; return smaller t
    u=np.array([math.sin(a),math.cos(a)]); C=np.array(C)
    b=-2*u.dot(C); c=C.dot(C)-r*r; disc=b*b-4*c
    assert disc>0
    t=(-b-math.sqrt(disc))/2; return t*u
def solve_root(T1,fd,nf,rad,Rc):
    # centre C = T1 + t fd + rad nf with |C| = Rc ; pick t>0 smallest
    A=np.array(T1)+rad*np.array(nf); fd=np.array(fd)
    b=2*A.dot(fd); c=A.dot(A)-Rc*Rc; disc=b*b-4*c
    ts=sorted([(-b-math.sqrt(disc))/2,(-b+math.sqrt(disc))/2]); t=[x for x in ts if x>0][0]
    return A+t*fd, np.array(T1)+t*fd
def arcmid(C,r,p0,p1):
    a0=math.atan2(p0[1]-C[1],p0[0]-C[0]); a1=math.atan2(p1[1]-C[1],p1[0]-C[0])
    da=(a1-a0+math.pi)%(2*math.pi)-math.pi   # short way
    am=a0+da/2; return (C[0]+r*math.cos(am),C[1]+r*math.sin(am)), abs(da)
def profile(o):
    hb=10.0; w=hb+o; rf=6.0+o; c=w-rf
    d=DRAFT; t=THETA
    n=np.array([math.cos(d),-math.sin(d)]); dirw=np.array([math.sin(d),math.cos(d)])
    nf=np.array([math.sin(t),math.cos(t)]); fd=np.array([math.cos(t),-math.sin(t)])
    Cc=np.array([c,380.0+rf])
    P1=Cc+rf*np.array([math.cos(-d),math.sin(-d)])
    C1z=CREST_TOP-RC; Ptz=C1z-RC*n[1]; L=(Ptz-P1[1])/math.cos(d); Pt=P1+L*dirw; C1=Pt+RC*n
    T1=C1+RC*nf
    C2,T2=solve_root(T1,fd,nf,RR,RO+RR)
    foot=C2*RO/np.linalg.norm(C2)
    Co=ray_circle(AC,C2,RR)
    # inner
    Q0=np.array([c,377.0]); Q1=Cc+(rf+TH)*np.array([math.cos(-d),math.sin(-d)])
    Pti=Pt+TH*n; T1i=C1+(RC-TH)*nf
    C2i,T2i=solve_root(T1i,fd,nf,RR+2.5,RI+RR+2.5)
    footi=C2i*RI/np.linalg.norm(C2i)
    Ci=ray_circle(AC,C2i,RR+2.5)
    out=[]; inn=[]
    m,_=arcmid(Cc,rf,(c,380.0),P1); out.append(('A',(c,380.0),m,tuple(P1)))
    out.append(('L',tuple(P1),None,tuple(Pt)))
    # crest: from Pt over the top to T1 (long way through the top) -> mid at angle pi/2 + ... use explicit mid angle
    a0=math.pi-d; a1=math.pi/2-t; am=(a0+a1)/2
    out.append(('A',tuple(Pt),(C1[0]+RC*math.cos(am),C1[1]+RC*math.sin(am)),tuple(T1)))
    out.append(('L',tuple(T1),None,tuple(T2)))
    m,sw=arcmid(C2,RR,T2,Co); out.append(('A',tuple(T2),m,tuple(Co)))
    m,_=arcmid(Cc,rf+TH,Q0,Q1); inn.append(('A',tuple(Q0),m,tuple(Q1)))
    inn.append(('L',tuple(Q1),None,tuple(Pti)))
    inn.append(('A',tuple(Pti),(C1[0]+(RC-TH)*math.cos(am),C1[1]+(RC-TH)*math.sin(am)),tuple(T1i)))
    inn.append(('L',tuple(T1i),None,tuple(T2i)))
    m,swi=arcmid(C2i,RR+2.5,T2i,Ci); inn.append(('A',tuple(T2i),m,tuple(Ci)))
    hc=T1+7.0*fd
    key=dict(o=o,w=w,rf=rf,c=c,P1=P1,Pt=Pt,C1=C1,T1=T1,T2=T2,C2=C2,foot=foot,foot_s=RO*math.atan2(foot[0],foot[1]),
             Co=Co,T2i=T2i,C2i=C2i,footi=footi,footi_s=RI*math.atan2(footi[0],footi[1])*RO/RI,Ci=Ci,hc=hc,nf=nf,fd=fd,
             close_s=AC*RO, root_sweep=sw/D2R, rooti_sweep=swi/D2R,
             co_ang=math.atan2(Co[0],Co[1]), foot_ang=math.atan2(foot[0],foot[1]),
             ci_ang=math.atan2(Ci[0],Ci[1]), footi_ang=math.atan2(footi[0],footi[1]))
    return out,inn,key
def disc(seg,n=24):
    k,p0,m,p1=seg
    if k=='L': return [p0,p1]
    # circle through 3 points
    ax,ay=p0; bx,by=m; cx,cy=p1
    dd=2*(ax*(by-cy)+bx*(cy-ay)+cx*(ay-by))
    ux=((ax*ax+ay*ay)*(by-cy)+(bx*bx+by*by)*(cy-ay)+(cx*cx+cy*cy)*(ay-by))/dd
    uy=((ax*ax+ay*ay)*(cx-bx)+(bx*bx+by*by)*(ax-cx)+(cx*cx+cy*cy)*(bx-ax))/dd
    r=math.hypot(ax-ux,ay-uy)
    a0=math.atan2(ay-uy,ax-ux); am=math.atan2(by-uy,bx-ux); a1=math.atan2(cy-uy,cx-ux)
    def norm(a): return (a-a0)%(2*math.pi)
    if norm(am)<norm(a1): sweep=norm(a1)
    else: sweep=norm(a1)-2*math.pi
    return [(ux+r*math.cos(a0+sweep*i/n),uy+r*math.sin(a0+sweep*i/n)) for i in range(n+1)]
def chain_pts(ch):
    pts=[]
    for s in ch:
        p=disc(s); pts+= p if not pts else p[1:]
    return pts
if __name__=="__main__":
    for o in (0.5,1.0,1.5):
        out,inn,k=profile(o)
        print(f"o={o} w={k['w']} c={k['c']:.3f} Pt={k['Pt'].round(3)} C1={k['C1'].round(3)} T1={k['T1'].round(3)} T2={k['T2'].round(3)}")
        print(f"   C2={k['C2'].round(3)} |C2|={np.linalg.norm(k['C2']):.3f} foot_s={k['foot_s']:.2f} close_s={k['close_s']} Co={k['Co'].round(3)} rootsweep={k['root_sweep']:.1f}")
        print(f"   T2i={k['T2i'].round(3)} C2i={k['C2i'].round(3)} footi_s={k['footi_s']:.2f} Ci={k['Ci'].round(3)} hc={k['hc'].round(3)}")
        print(f"   angles: close {k['co_ang']:.5f} foot {k['foot_ang']:.5f} | inner close {k['ci_ang']:.5f} foot {k['footi_ang']:.5f}; DX={np.round(DX,3)}")
