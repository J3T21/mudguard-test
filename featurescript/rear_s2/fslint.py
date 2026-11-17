"""Minimal FeatureScript-subset parser (lint). Reports the first syntax error with line number.
Validate on known-good files before trusting it."""
import re, sys
KW = {"annotation","enum","export","function","import","operator","precondition","predicate","returns","type","typecheck",
      "typeconvert","as","is","new","break","const","continue","for","in","return","var","while","false","inf","true",
      "undefined","catch","throw","try","assert","case","default","do","switch","if","else","silent"}
TOK = re.compile(r'''(?P<ws>\s+)|(?P<c1>//[^\n]*)|(?P<c2>/\*.*?\*/)|(?P<num>\d+\.?\d*(?:[eE][-+]?\d+)?|\.\d+)|(?P<str>"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')|(?P<id>@?[A-Za-z_]\w*)|(?P<op>->|\?\?=|\|\|=|&&=|::|==|!=|<=|>=|&&|\|\||\?\?|\+=|-=|\*=|/=|\^=|%=|~=|[-+*/%^~<>!=?:.,;(){}\[\]])''', re.S)
def lex(src):
    out=[]; pos=0; line=1
    while pos < len(src):
        m = TOK.match(src, pos)
        if not m: raise SyntaxError(f"line {line}: bad char {src[pos]!r}")
        k = m.lastgroup; t = m.group()
        if k not in ("ws","c1","c2"): out.append((k,t,line))
        line += t.count("\n"); pos = m.end()
    for a,b in (("++","+"),("--","-")): pass
    out.append(("eof","",line)); return out
class P:
    def __init__(s,t): s.t=t; s.i=0; s.sc=[{}]; s.free={}; s.issues=[]; s.top=set()
    def pk(s,o=0): return s.t[s.i+o]
    def err(s,msg): k,t,l=s.pk(); raise SyntaxError(f"line {l}: {msg} (at {t!r})")
    def eat(s,v=None,kind=None):
        k,t,l=s.pk()
        if (v is not None and t!=v) or (kind is not None and k!=kind): s.err(f"expected {v or kind}")
        s.i+=1; return t
    def at(s,v): return s.pk()[1]==v
    def ident(s):
        k,t,l=s.pk()
        if k!="id" or t in KW: s.err("expected identifier")
        s.i+=1; return t
    # top level
    def program(s):
        s.eat("FeatureScript"); s.eat(kind="num"); s.eat(";")
        while not s.at(""):
            if s.at("import"):
                s.eat("import"); s.eat("(")
                while not s.at(")"):
                    s.ident(); s.eat(":"); s.expr()
                    if s.at(","): s.eat(",")
                s.eat(")"); s.eat(";")
            elif s.at("annotation"): s.eat("annotation"); s.mapLit()
            elif s.at("export") or s.at("const"):
                if s.at("export"): s.eat("export")
                if s.at("function"): s.funcDecl(); continue
                s.eat("const"); s.ident(); s.eat("="); s.expr(); s.eat(";")
            elif s.at("function"): s.funcDecl()
            else: s.err("unexpected top-level token")
    def decl(s,name,kind):
        l=s.t[s.i-1][2]
        for d in s.sc[1:]:
            if name in d: s.issues.append(f"line {l}: '{name}' redeclared/shadowed (first {d[name][1]})")
        s.sc[-1][name]=(kind,l)
    def params(s):
        s.eat("(")
        while not s.at(")"):
            s.decl(s.ident(),"param")
            if s.at("is"): s.eat("is"); s.typeName()
            if s.at(","): s.eat(",")
            elif not s.at(")"): s.err("bad parameter list")
        s.eat(")")
    def typeName(s):
        k,t,l=s.pk()
        if k!="id": s.err("expected type")
        s.i+=1
    def funcDecl(s):
        s.eat("function"); n=s.ident(); s.top.add(n); s.sc.append({}); s.params()
        if s.at("returns"): s.eat("returns"); s.typeName()
        if s.at("precondition"): s.eat("precondition"); s.block()
        s.block(); s.sc.pop()
    def block(s):
        s.eat("{"); s.sc.append({})
        while not s.at("}"): s.stmt()
        s.eat("}"); s.sc.pop()
    def stmt(s):
        k,t,l=s.pk()
        if t=="{": s.block(); return
        if t=="annotation": s.eat("annotation"); s.mapLit(); return
        if t in ("var","const"):
            s.i+=1; n=s.ident()
            if s.at("is"): s.eat("is"); s.typeName()
            if s.at("="): s.eat("="); s.expr()
            elif t=="const": s.issues.append(f"line {l}: const without initialiser")
            s.decl(n,t)
            s.eat(";"); return
        if t=="if":
            s.eat("if"); s.eat("("); s.expr(); s.eat(")"); s.stmt()
            if s.at("else"): s.eat("else"); s.stmt()
            return
        if t=="for":
            s.eat("for"); s.eat("(")
            if s.at("var") and s.pk(2)[1]=="in":
                s.eat("var"); n=s.ident(); s.eat("in"); s.expr(); s.sc.append({}); s.decl(n,"var"); s.eat(")"); s.stmt(); s.sc.pop(); return
            s.sc.append({})
            if s.at("var"): s.eat("var"); n=s.ident(); s.eat("="); s.expr(); s.decl(n,"var")
            s.eat(";"); s.expr(); s.eat(";"); s.simple(); s.eat(")"); s.stmt(); s.sc.pop(); return
        if t=="while": s.eat("while"); s.eat("("); s.expr(); s.eat(")"); s.stmt(); return
        if t=="return":
            s.eat("return")
            if not s.at(";"): s.expr()
            s.eat(";"); return
        if t=="throw": s.eat("throw"); s.expr(); s.eat(";"); return
        if t in ("break","continue"): s.i+=1; s.eat(";"); return
        if t=="try":
            s.eat("try")
            if s.at("silent"): s.eat("silent")
            if s.at("{"): s.block()
            else: s.eat("("); s.expr(); s.eat(")"); s.eat(";"); return
            if s.at("catch"): s.eat("catch"); s.eat("("); s.ident(); s.eat(")"); s.block()
            return
        s.simple(); s.eat(";")
    def simple(s):
        st=s.i; s.expr()
        k0,t0,l0=s.t[st]
        if s.pk()[1] in ("=","+=","-=","*=","/=","^=","%=","~=","||=","&&=","??="):
            for d in reversed(s.sc):
                if t0 in d:
                    if d[t0][0]=="const": s.issues.append(f"line {l0}: assignment to const '{t0}'")
                    break
        if s.pk()[1] in ("=","+=","-=","*=","/=","^=","%=","~=","||=","&&=","??="): s.i+=1; s.expr()
    def args(s,close):
        while not s.at(close):
            s.expr()
            if s.at(","): s.eat(",")
            elif not s.at(close): s.err("expected , or "+close)
        s.eat(close)
    def mapLit(s):
        s.eat("{")
        while not s.at("}"):
            s.expr(); s.eat(":"); s.expr()
            if s.at(","): s.eat(",")
            elif not s.at("}"): s.err("expected , or } in map")
        s.eat("}")
    BIN=[["??"],["||"],["&&"],["==","!="],["<",">","<=",">="],["~"],["+","-"],["*","/","%"],["^"]]
    def expr(s):
        s.binary(0)
        if s.at("?"): s.eat("?"); s.expr(); s.eat(":"); s.expr()
    def binary(s,lvl):
        if lvl==len(s.BIN): s.unary(); return
        s.binary(lvl+1)
        while s.pk()[1] in s.BIN[lvl] or (lvl==3 and s.at("is")) or (lvl==3 and s.at("as")):
            if s.at("is") or s.at("as"): s.i+=1; s.typeName(); continue
            s.i+=1; s.binary(lvl+1)
    def unary(s):
        if s.pk()[1] in ("-","!"): s.i+=1; s.unary(); return
        s.postfix()
    def postfix(s):
        s.primary()
        while True:
            if s.at("("): s.eat("("); s.args(")")
            elif s.at("["): s.eat("["); s.expr(); s.eat("]")
            elif s.at("."): s.eat("."); s.eat(kind="id")
            elif s.at("::"): s.eat("::"); s.eat(kind="id")
            elif s.at("->"): s.eat("->"); s.eat(kind="id")
            else: break
    def primary(s):
        k,t,l=s.pk()
        if k in ("num","str"): s.i+=1; return
        if t in ("true","false","undefined","inf"): s.i+=1; return
        if t=="(": s.eat("("); s.expr(); s.eat(")"); return
        if t=="[": s.eat("["); s.args("]"); return
        if t=="{": s.mapLit(); return
        if t=="try":
            s.eat("try")
            if s.at("silent"): s.eat("silent")
            s.eat("("); s.expr(); s.eat(")"); return
        if t=="function":
            s.eat("function"); s.sc.append({}); s.params()
            if s.at("returns"): s.eat("returns"); s.typeName()
            if s.at("precondition"): s.eat("precondition"); s.block()
            s.block(); s.sc.pop(); return
        if k=="id" and t not in KW:
            if s.i>0 and s.t[s.i-1][1] in (".","->","::"): s.i+=1; return
            if not any(t in d for d in s.sc): s.free.setdefault(t,l)
            s.i+=1; return
        s.err("unexpected token in expression")
def lint(path):
    src=open(path).read()
    for k,t,l in lex(src):
        if re.search(r'[^\x00-\x7f]',t) and k!="str": return f"non-ascii token at line {l}"
    try:
        p=P(lex(src)); p.program()
        free=sorted((n,l) for n,l in p.free.items() if n not in p.top)
        return "OK\n  issues: "+repr(p.issues)+"\n  external names: "+", ".join(n for n,l in free)
    except SyntaxError as e: return "SYNTAX: "+str(e)
if __name__=="__main__":
    for p in sys.argv[1:]: print(p, lint(p))
