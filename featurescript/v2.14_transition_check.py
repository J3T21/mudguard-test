# Offline check of station transitions: v2.13 vs v2.14 (failed, C1 to s12 + round tail) vs v2.14a (C1 to s10, v2.13 tail)
import numpy as np, math
tA=math.tan(math.radians(19))
W,WL,ph5,ltf=82,97,None,None
dB=31.95; dL=30.49; lo=45
def ywall(row,z):
    """half-width of outer U at depth z below top: top flat a, corner r tangent to top and wall (alpha from vertical), wall to depth d"""
    s,w,d,al,r=row[:5]; al=math.radians(al)
    a=w/2-d*math.tan(al)
    # corner centre: (a - r*tan((90-al)/2)?) use exact: top line z=0, wall line through (a,0) going out at angle al from vertical
    # centre at distance r from both lines, inside
    half=(math.pi/2+al)/2  # half of interior angle between top (pointing -y) and wall (pointing down/out)
    t=r/math.tan(half)      # tangent length from vertex
    cy=a - t*1.0 + 0; cz=r   # centre below top at depth r, y = a - t
    # wall tangent point
    zt=t*math.cos(al)
    if z<=zt+1e-9:
        # on arc
        dz=cz-z
        return cy+math.sqrt(max(r*r-dz*dz,0))
    return a+z*math.tan(al)
def spline(x,y):
    n=len(x); h=np.diff(x); A=np.zeros((n,n)); b=np.zeros(n)
    A[0,0]=A[-1,-1]=1
    for i in range(1,n-1):
        A[i,i-1]=h[i-1];A[i,i]=2*(h[i-1]+h[i]);A[i,i+1]=h[i]
        b[i]=3*((y[i+1]-y[i])/h[i]-(y[i]-y[i-1])/h[i-1])
    c=np.linalg.solve(A,b)
    def f(xx):
        i=np.clip(np.searchsorted(x,xx)-1,0,n-2); dx=xx-x[i]
        bb=(y[i+1]-y[i])/h[i]-h[i]*(2*c[i]+c[i+1])/3; d=(c[i+1]-c[i])/(3*h[i])
        return y[i]+bb*dx+c[i]*dx**2+d*dx**3
    return f
def drop(rows):
    out=[]
    for r in rows:
        if out and r[0]-out[-1][0]<8: continue
        out.append(r)
    return out
front213=[[-70,97,34,15,10],[-42.5,89.5,(34+dB)/2,17,8],[-15,W,dB,19,6],[0,W,dB,19,6],[10,W,dB,19,6],[16,(W+WL)/2,(dB+dL)/2,19,6]]
leg=[[lo-23,WL,dL,19,6],[lo-11,WL,dL,19,6],[lo,WL,dL,19,6],[lo+11,WL,dL,19,6],[lo+23,WL,dL,19,6]]
tail19=[[lo+40,97,31,19,6],[157,97,28,19,6],[242,97,25,19,6],[315,97,22,19,6]]
tailR=[[lo+40,97,31,5,20],[157,97,28,5,20],[242,97,25,5,20],[315,97,22,5,20]]
C1D=float(__import__("os").environ.get("C1D","26"))
c1=lambda s:[s,82,C1D,0,18]
fr214=[[-70,97,34,5,20],[-55,97,34,5,20],[-35,90,30,8,14],c1(-15),c1(0)]
cases={'v2.13':drop(front213+leg+tail19),'v2.14 failed':drop(fr214+[c1(12)]+leg+tailR),'v2.14a':drop(fr214+[c1(10),[16,0,0,0,0]]+leg+tail19)}
cases['v2.14a']=[r for r in cases['v2.14a'] if r[1]>0]
res={}
for k,rows in cases.items():
    ss=np.array([r[0] for r in rows],float)
    print(f"\n== {k}: stations {[r[0] for r in rows]}")
    worst=0
    for z in [3,10,18,24]:
        ys=np.array([ywall(r,min(z,r[2])) for r in rows]); f=spline(ss,ys)
        x=np.linspace(ss[0],ss[-1],6000); y=f(x)
        # overshoot: beyond max of the two bracketing station values
        i=np.clip(np.searchsorted(ss,x)-1,0,len(ss)-2)
        hi=np.maximum(ys[i],ys[i+1]); lo_=np.minimum(ys[i],ys[i+1])
        ov=np.maximum(y-hi,lo_-y); j=ov.argmax()
        m=(x>-20)&(x<100); jj=np.argmax(np.where(m,ov,-1))
        print(f"  |y| at depth {z:2}: worst overshoot {ov[j]:.2f} at s {x[j]:.0f};  in s -20..100: {ov[jj]:.2f} at s {x[jj]:.0f} (y {y[jj]:.2f})")
        worst=max(worst,ov[jj])
    ds=np.array([r[2] for r in rows]); f=spline(ss,ds); x=np.linspace(-20,100,3000); y=f(x)
    i=np.clip(np.searchsorted(ss,x)-1,0,len(ss)-2); ov=np.maximum(y-np.maximum(ds[i],ds[i+1]),np.minimum(ds[i],ds[i+1])-y)
    j=ov.argmax(); print(f"  depth overshoot s -20..100: {ov[j]:.2f} at s {x[j]:.0f} -> extra |y| at wall edge ~{ov[j]*tA:.2f}")
    # max interval slope of bottom-edge |y|
    ye=np.array([ywall(r,r[2]) for r in rows]); sl=np.abs(np.diff(ye))/np.diff(ss)
    print(f"  steepest bottom-edge step {sl.max():.2f} mm/mm between s {ss[sl.argmax()]:.0f}-{ss[sl.argmax()+1]:.0f}")
