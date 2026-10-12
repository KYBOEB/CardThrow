import numpy as np, sys
from PIL import Image
S=4; W=140*S
TH=np.radians(float(sys.argv[1]) if len(sys.argv)>1 else 28)  # наклон камеры от вертикали
ct,st=np.cos(TH),np.sin(TH)
# профиль бутылки шампанского (R тела = 1)
def prof(z):
    r=np.ones_like(z)
    r=np.where(z<0.12, 0.93+0.07*np.sqrt(np.clip(z/0.12,0,1)), r)          # скругление дна
    t=np.clip((z-3.3)/1.6,0,1); s=t*t*(3-2*t); r=np.where(z>3.3, 1-(1-0.37)*s, r)  # плечи
    r=np.where(z>4.9, 0.37-0.03*np.clip((z-4.9)/1.4,0,1), r)               # горлышко
    lip=np.exp(-((z-6.35)/0.12)**2)*0.07; r=r+lip                          # венчик под фольгой
    return r
H=6.62
zs=np.linspace(0,H,1400)
rs=prof(zs); dr=np.gradient(rs,zs)
# масштаб: вписать силуэт в спрайт
top=H*st+prof(np.array([H]))[0]*ct; bot=-1*ct
R=(W*0.95)/max(2.0, top-bot)
cx=W/2; base_y=W/2+R*(top+bot)/2   # экранная y основания (вниз +)
yy,xx=np.mgrid[0:W,0:W].astype(np.float32)+0.5
img=np.zeros((W,W,3),np.float32); alpha=np.zeros((W,W),np.float32)
L=np.array([-0.55,0.35,0.76]); L/=np.linalg.norm(L)
V=np.array([0,-st,ct]); Hh=L+V; Hh/=np.linalg.norm(Hh)
glass=np.array([0.09,0.16,0.10]); gold=np.array([0.86,0.68,0.32]); goldd=np.array([0.45,0.32,0.12])
labelc=np.array([0.05,0.05,0.05])
rng=np.random.default_rng(3); noise=rng.normal(0,1,(64,256))
def foilnoise(phi,z):
    i=((z/H)*63).astype(int)%64; j=(((phi+np.pi)/(2*np.pi))*255).astype(int)%256
    return noise[i,j]
for z,r,d in zip(zs,rs,dr):
    cy=base_y - z*st*R
    X=(xx-cx)/(r*R); Y=-(yy-cy)/(r*R*ct)
    m=X*X+Y*Y<=1.0
    if not m.any(): continue
    Xm,Ym=X[m],Y[m]; phi=np.arctan2(Ym,Xm); rad=np.sqrt(Xm*Xm+Ym*Ym)
    n=np.stack([np.cos(phi),np.sin(phi),-d*np.ones_like(phi)],1); n/=np.linalg.norm(n,axis=1,keepdims=True)
    if z>=H-1e-6:  # верх пробки — диск
        n=np.stack([0*phi,0*phi,1+0*phi],1)
    ndl=np.clip(n@L,0,1); ndh=np.clip(n@Hh,0,1); ndv=np.clip(n@V,0,1)
    if z>4.95:   # фольга
        base=gold*(0.85+0.12*foilnoise(phi,np.full_like(phi,z)))[:,None]
        if 5.25<z<5.6: base=np.array([0.10,0.07,0.04])+0*base   # тёмный поясок на горлышке
        if 5.3<z<5.55: base=np.where((np.abs(np.cos(phi*7))>0.92)[:,None],gold,base)
        if z>=H-1e-6:
            ring=(np.abs(rad-0.62)<0.07)|(rad<0.18)
            base=np.where(ring[:,None],goldd,gold*1.02)
        col=base*(0.25+0.85*ndl)[:,None]+np.array([1,0.9,0.65])*(ndh**30*0.9)[:,None]
    else:
        col=glass*(0.35+0.9*ndl)[:,None]
        # этикетка
        if 1.0<z<2.7:
            u=phi+np.pi/2  # 0 = к зрителю
            u=(u+np.pi)%(2*np.pi)-np.pi
            on=np.abs(u)<1.25
            lab=np.tile(labelc,(len(phi),1))
            edge=(np.abs(z-1.08)<0.06)|(np.abs(z-2.62)<0.06)|(np.abs(np.abs(u)-1.2)<0.05)
            dia=(np.abs(u)/0.55+np.abs(z-1.85)/0.55)
            dm=(np.abs(dia-1)<0.09)
            spade=(np.abs(u)/0.18+np.abs(z-1.85)/0.28)<1
            g=(edge|dm|spade)&on
            lab[g]=gold
            col=np.where(on[:,None],lab*(0.4+0.8*ndl)[:,None],col)
        da=(phi+2.25+np.pi)%(2*np.pi)-np.pi; db=(phi+0.75+np.pi)%(2*np.pi)-np.pi
        sw=0.10 if z<3.3 else 0.16
        stripe=np.exp(-(da/sw)**2)*0.75+np.exp(-(db/0.06)**2)*0.22
        if z>3.3: stripe=stripe*(1+0.6*np.clip(-d,0,1))
        spec=ndh**60*1.1+ndh**8*0.10+stripe
        fres=(1-ndv)**3*0.35
        col=col+np.array([1,0.88,0.6])*spec[:,None]+np.array([0.25,0.4,0.3])*fres[:,None]
    img[m]=col; alpha[m]=1
# мягкая тень на столе (под основанием)
sh=np.zeros((W,W),np.float32)
X=(xx-cx-R*0.18)/(R*1.15); Y=(yy-base_y+R*0.05)/(R*1.0*ct)
sh=np.clip(1-(X*X+Y*Y),0,1)**1.5*0.45
out=np.zeros((W,W,4),np.float32)
out[...,:3]=img*alpha[...,None]
a=alpha+sh*(1-alpha)
out[...,3]=a
out[...,:3]=np.where(a[...,None]>0,out[...,:3]/np.maximum(a[...,None],1e-6),0)
o=(np.clip(out,0,1)*255).astype(np.uint8)
from PIL import ImageFilter
A=Image.fromarray((alpha*255).astype(np.uint8)).filter(ImageFilter.MaxFilter(7))
ol=(np.array(A)/255.0>0.5)&(alpha<0.5)
o[ol]=[8,12,8,235]
big=Image.fromarray(o,'RGBA')
big.save('bottle_560.png')
big.resize((140,140),Image.LANCZOS).save('bottle.png')
print('R px @140:',R/S)
