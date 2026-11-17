import numpy as np, math, matplotlib
matplotlib.use('Agg'); import matplotlib.pyplot as plt
R=393.5
def prof(a,alpha,r,t,hEnd,n=60):
    # replicate profSketch: returns (x,h) polyline right half then mirrored, h = depth below R
    ca,sa=math.cos(math.radians(alpha)),math.sin(math.radians(alpha))
    cx=a-r*(1-sa)/ca; rr=r-t
    wtx=cx+rr*ca; wth=r-rr*sa
    xe=a+hEnd*sa/ca-t/ca
    pts=[(0,t),(cx,t)]
    # arc centre (cx, r), from angle 90deg (up => h=t ... h axis down) param: point = (cx+rr*sin(phi), r-rr*cos(phi)), phi 0->90-alpha
    for ph in np.linspace(0,math.radians(90-alpha),n):
        pts.append((cx+rr*math.sin(ph), r-rr*math.cos(ph)))
    pts.append((xe,hEnd))
    return np.array(pts)
def stn(w,d,alpha,r): return dict(a=w/2-d*math.tan(math.radians(alpha)),alpha=alpha,r=r,d=d)
tA=math.tan(math.radians(19))
dB=(82-60)/(2*tA)
v213=stn(82,dB,19,6); 
def seg_dist(P,poly):
    best=1e9
    for i in range(len(poly)-1):
        A,B=poly[i],poly[i+1]; AB=B-A; t=np.clip(np.dot(P-A,AB)/max(np.dot(AB,AB),1e-12),0,1)
        best=min(best,np.linalg.norm(P-(A+t*AB)))
    return best
def tyre_clear(S,wall,Wt):
    inner=prof(S['a'],S['alpha'],S['r'],wall,S['d'])
    # tyre circle centre at radius 375 - Wt/2 => depth below R: R-(375-Wt/2)
    rr=Wt/2; cd=R-(375-rr)
    pts=np.array([(x,h) for x,h in inner])
    return min(math.hypot(x, cd-h)-rr for x,h in pts) , pts
# arch: z rel brace underside (radius 395) -> depth below R: h = -(z) - 1.5
def arch_pts():
    P=[(0,0),(25,0)]
    for ph in np.linspace(0,math.pi/2,40): P.append((25+20*math.sin(ph), -20+20*math.cos(ph)))
    P.append((45,-20)); P.append((42.5,-40))
    return np.array([(y,-z-1.5) for y,z in P])
arch=arch_pts()
def dense(poly,step=0.2):
    out=[]
    for i in range(len(poly)-1):
        A,B=poly[i],poly[i+1]; n=max(1,int(np.linalg.norm(B-A)/step))
        for t in np.linspace(0,1,n,endpoint=False): out.append(A+t*(B-A))
    out.append(poly[-1]); return np.array(out)
res={}
for name,S,wall in [('v2.13 waist (82, d31.95, 19deg, r6)',v213,3.0),('v2.14 C1 d26',stn(82,26,0,18),3.0),('v2.14 C1 d24',stn(82,24,0,18),3.0),
                    ('v2.14 blend s-35 (90,d30,8deg,r14)',stn(90,30,8,14),2.75),('v2.14 front/tail (97,d34,5deg,r20)',stn(97,34,5,20),2.5),('v2.14 tail d22',stn(97,22,5,20),2.5)]:
    cm,_=tyre_clear(S,wall,69.2); ca,_=tyre_clear(S,wall,63.5)
    outer=dense(prof(S['a'],S['alpha'],S['r'],0,S['d']))
    gaps=[(seg_dist(p,dense(arch)),p) for p in outer if p[1]<=S['d']]
    gmin=min(gaps,key=lambda g:g[0])
    side=[g for g in gaps if g[1][0]>24]
    res[name]=(cm,ca,gmin,min(side,key=lambda g:g[0]),max(side,key=lambda g:g[0]))
    print(f"{name}: tyre clr max/Assegai {cm:.2f}/{ca:.2f}; arch gap min {gmin[0]:.2f} at y{gmin[1][0]:.1f} d{gmin[1][1]:.1f}; beyond flat (|y|>24): {res[name][3][0]:.2f}..{res[name][4][0]:.2f}")
# plot
fig,ax=plt.subplots(figsize=(9,6.5))
def plot_prof(S,wall,c,lab):
    for t,ls in [(0,'-'),(wall,'--')]:
        P=prof(S['a'],S['alpha'],S['r'],t,S['d'])
        ax.plot(P[:,0],-P[:,1],c=c,ls=ls,label=lab if t==0 else None); ax.plot(-P[:,0],-P[:,1],c=c,ls=ls)
plot_prof(v213,3.0,'tab:gray','v2.13 waist (trapezium) outer / inner--')
plot_prof(stn(82,26,0,18),3.0,'tab:blue','v2.14 C1 (d26, R18) outer / inner--')
ax.plot(arch[:,0],-arch[:,1],'r-',lw=2,label='brace arch (EST, Reference Analyst, ±4 mm)'); ax.plot(-arch[:,0],-arch[:,1],'r-',lw=2)
for W,c in [(69.2,'k'),(63.5,'tab:green')]:
    th=np.linspace(0,2*math.pi,300); cd=R-(375-W/2)
    ax.plot(W/2*np.cos(th),-(cd+W/2*np.sin(th)),c=c,lw=0.8,ls=':',label=f'tyre model {W} mm')
ax.axhline(1.5,c='r',lw=0.5)
ax.set_aspect('equal'); ax.set_xlim(-60,60); ax.set_ylim(-45,8); ax.grid(alpha=.3)
ax.set_xlabel('y (mm)'); ax.set_ylabel('height above guard top R393.5 (mm)')
r=res['v2.14 C1 d26']
ax.set_title(f"Section at s=0 (under brace): v2.13 vs v2.14 C1 [ANALYTIC from profSketch params, not an Onshape build]\nC1 tyre clr {r[0]:.2f}/{r[1]:.2f} (v2.13 {res[list(res)[0]][0]:.2f}/{res[list(res)[0]][1]:.2f}); arch gap: top 1.5 (pad), 45deg corner 3.0, wall 3.0-4.0 mm",fontsize=9)
ax.legend(fontsize=7,loc='lower center')
plt.savefig('/workspace/mudguard/out/v2.14_section_s0_v2.13_vs_v2.14_ANALYTIC.png',dpi=130,bbox_inches='tight')
S=stn(82,26,0,18); outer=dense(prof(S['a'],0,18,0,26)); A=dense(arch)
for y0 in [23,26,28,30,32,35.7,38.6,41]:
    p=min(outer,key=lambda p:abs(p[0]-y0)+ (0 if p[0]<41-1e-6 else 100))
    print('C1 y',y0,'d',round(p[1],1),'gap',round(seg_dist(p,A),2))
for d0 in [10,18,22,26]:
    print('C1 wall d',d0,'gap',round(seg_dist(np.array([41.0,d0]),A),2))
