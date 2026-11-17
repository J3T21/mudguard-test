import math, json
from s3geom import *
def f(v): return ("%.6f" % v).rstrip('0').rstrip('.') if abs(v)>=1e-9 else "0"
def seglist(ch):
    rows=[]
    for k,p0,m,p1 in ch:
        if k=='L': rows.append("[0, %s, %s, 0, 0, %s, %s]"%(f(p0[0]),f(p0[1]),f(p1[0]),f(p1[1])))
        else: rows.append("[1, %s, %s, %s, %s, %s, %s]"%(f(p0[0]),f(p0[1]),f(m[0]),f(m[1]),f(p1[0]),f(p1[1])))
    return "[" + ",\n                 ".join(rows) + "]"
def prof_block(o):
    out,inn,k=profile(o)
    return ("    if (oi == %d)\n        return { \"out\" : %s,\n                 \"inn\" : %s,\n"
            "                 \"c\" : %s, \"w\" : %s, \"hc\" : [%s, %s], \"C1\" : [%s, %s] };\n") % (
        int(round(o*2)), seglist(out), seglist(inn), f(k['c']), f(k['w']), f(k['hc'][0]), f(k['hc'][1]), f(k['C1'][0]), f(k['C1'][1]))
blocks="".join(prof_block(o) for o in (0.5,1.0,1.5))
src=open('template.fs').read().replace("//@@PROFILES@@", blocks.rstrip("\n"))
src=src.replace("@@DX14@@",f(DX[2])).replace("@@DX18@@",f(DX[3])).replace("@@DX22@@",f(DX[4])).replace("@@DX255@@",f(DX[5]))
V=json.load(open('vol.json'))
for n in "ABCD": src=src.replace("@@V%s@@"%n, "%.2f"%V[n]['cm3'])
open('rear_s3_coupons.fs','w').write(src)
print("ok", len(src))
P=json.load(open('probes.json'))
pr=[]; px=[]
for n in "ABCD":
    pr.append("[" + ", ".join("[%s, %s, %s]"%tuple(f(v) for v in r[1]) for r in P[n]) + "]")
    px.append("[" + ", ".join(str(r[2]) for r in P[n]) + "]")
block=("        // probe points (coupon at the origin) and expected inside(1)/outside(0) string, from probes.py on the offline model\n"
       "        const PR = [" + ",\n                    ".join(pr) + "];\n        const PX = [" + ", ".join(px) + "];")
src=open('rear_s3_coupons.fs').read().replace("//@@PROBES@@", block)
open('rear_s3_coupons.fs','w').write(src)
print("probes injected")
