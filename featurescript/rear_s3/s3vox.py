import math, numpy as np, json, sys
from matplotlib.path import Path
from s3geom import *
K=math.sqrt(1.09)            # 1/cos(atan .3)
YS0=29.6; YT0=YS0-3*K         # cup leg stay-side / tyre-side y at z=378 (d 2)
SEGS=[(10,14),(14,18),(18,22),(22,25.5)]
DXS={10:0.0,14:DX[2],18:DX[3],22:DX[4],25.5:DX[5]}
XR={(10,14):-3.0,(14,18):-3.0,(18,22):-3.0,(22,25.5):-4.5}   # absolute floor start of the right half at segment start
def tr(pts,sx,dx): return [(sx*(x+dx),z) for x,z in pts]
def full_shell_poly(o,dx):
    out,inn,k=profile(o)
    ro=chain_pts(out); ri=chain_pts(inn)
    R=tr(ro,1,dx)+tr(ri[::-1],1,dx)          # right: (c,380)->...->Co, Ci->...->(c,377)
    L=tr(ri,-1,dx)+tr(ro[::-1],-1,dx)        # left: (-c,377)->...->-Ci, -Co ... -> (-c,380)
    return Path(np.array(R+L+[R[0]]))
def h2_poly(o,dx):
    out,inn,k=profile(o); ro=chain_pts(out)
    R=tr(ro,1,dx); xo=R[-1][0]; zb=math.sqrt(358**2-xo**2)
    arc=[(xo*math.cos(t)-0,0) for t in []]
    a0=math.atan2(xo,zb); bottom=[(358*math.sin(a0-2*a0*i/60),358*math.cos(a0-2*a0*i/60)) for i in range(61)]
    L=tr(ro[::-1],-1,dx)
    return Path(np.array(R+bottom+L+[R[0]]))
def cup_section_path():
    pts=[(26,YS0),(-17,YS0)]
    c=(-17,YS0+2); pts+=[(c[0]+2*math.cos(a),c[1]+2*math.sin(a)) for a in np.linspace(-math.pi/2,-math.pi,13)[1:]]
    pts+=[(-19,50),(-22,50),(-22,YT0+5)]
    c=(-17,YT0+5); pts+=[(c[0]+5*math.cos(a),c[1]+5*math.sin(a)) for a in np.linspace(math.pi,1.5*math.pi,25)[1:]]
    pts+=[(26,YT0),(26,YS0)]
    return Path(np.array(pts))
CUP=cup_section_path()
def obround_in(u,v,W,H):
    a=W/2-H/2; r=H/2
    uu=np.maximum(np.abs(u)-a,0); return uu*uu+v*v<=r*r
class Coupon:
    def __init__(s,o,ylip):
        s.o=o; s.ylip=ylip; s.out,s.inn,s.k=profile(o)
        s.p0=full_shell_poly(o,0.0); s.h2=h2_poly(o,DX[5])
        s.cache={}
    def shell_at(s,y):
        ay=abs(y)
        if ay<=10: return s.p0,0.0
        for a,b in SEGS:
            if a<ay<=b+1e-9:
                dx=DXS[a]+(DXS[b]-DXS[a])*(ay-a)/(b-a); key=round(dx,4)
                if key not in s.cache: s.cache[key]=full_shell_poly(s.o,dx)
                return s.cache[key],dx
        return None,None
    def member(s,x,y,z):
        """vectorised over arrays x,z for a single scalar y"""
        x=np.asarray(x,float); z=np.asarray(z,float); ay=abs(y); m=np.zeros(x.shape,bool)
        pts=np.c_[x.ravel(),z.ravel()]
        if ay<=25.5:
            p,dx=s.shell_at(y); m|=p.contains_points(pts).reshape(x.shape)
            # floor overlap fill (halves overlap; the full-profile polygon already contains the floor)
        elif ay<=28.0:
            m|=s.h2.contains_points(pts).reshape(x.shape)
        r=np.hypot(x,z); ang=np.arctan2(x,z)
        # web
        if 27.5<=ay<=31.6: m|=(r>=366.5)&(r<=369.0)&(ang>=-21/RO)&(ang<=24/RO)
        # cups (oblique prism along (0,+-.3,-1) from z 378 to 356)
        zz=(z>=356)&(z<=378)
        y0=ay-0.3*(378-z)
        inc=CUP.contains_points(np.c_[x.ravel(),y0.ravel()]).reshape(x.shape)&zz&(ay<=s.ylip)
        # cuts on cups
        hole=(x>=19.7)&(x<=22.3)&(z>=359)&(z<=365)&(ay>=29)&(ay<=36.5)
        yt=YT0+0.3*(378-z)
        rec=(x>=-16.5)&(x<=21)&(z>=359)&(z<=365)&(ay>=yt-1.5*K)&(ay<=yt+1.0*K)
        grv=(x>=-23.5)&(x<=-21.2)&(ay>=38)&(ay<=48)&(z>=359)&(z<=365)
        m|=inc
        cut=hole|rec|grv
        # flank holes + notch (hump), y +8: 6.0x2.6 ; y -8: 5.5x2.2 + crest notch
        hc=s.k['hc']; nf=s.k['nf']; fd=s.k['fd']
        for yc,W,H in ((8.0,6.0,2.6),(-8.0,5.5,2.2)):
            for sx in (1,-1):
                dxp=x*sx-hc[0]; dzp=z-hc[1]
                a=dxp*nf[0]+dzp*nf[1]; v=dxp*fd[0]+dzp*fd[1]
                cut|=(a>=-5)&(a<=2)&obround_in(np.full(x.shape,y-yc),v,W,H)
        C1=s.k['C1']
        if -11<=y<=-5:
            for sx in (1,-1):
                cut|=(x*sx>=C1[0]-3.5)&(x*sx<=C1[0]+3.5)&(z>=398.5)&(z<=401)
        m&=~cut
        m&=np.abs(ang)<=50/RO
        return m
    def text_vol(s): return 0.0
def volume(cp,h=0.25):
    xs=np.arange(-56,56,h)+h/2; zs=np.arange(352,402,h)+h/2; X,Z=np.meshgrid(xs,zs)
    tot=0; bb=[1e9,-1e9,1e9,-1e9,1e9,-1e9]
    for y in np.arange(-50,50,h)+h/2:
        m=cp.member(X,y,Z); n=m.sum()
        if n:
            tot+=n; bb[2]=min(bb[2],y); bb[3]=max(bb[3],y)
            bb[0]=min(bb[0],X[m].min()); bb[1]=max(bb[1],X[m].max()); bb[4]=min(bb[4],Z[m].min()); bb[5]=max(bb[5],Z[m].max())
    return tot*h**3/1000, bb
if __name__=="__main__":
    V={}
    for name,o,yl in (("A",0.5,43.0),("B",1.0,43.0),("C",1.5,43.0),("D",1.0,47.0)):
        cp=Coupon(o,yl); v,bb=volume(cp,float(sys.argv[1]) if len(sys.argv)>1 else 0.25)
        V[name]=dict(o=o,ylip=yl,cm3=round(v,3),bb=[round(b,2) for b in bb]); print(name,V[name],flush=True)
    json.dump(V,open('vol.json','w'),indent=1)
