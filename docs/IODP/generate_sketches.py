"""Bocetos SVG deterministas: solo biblioteca estandar, no modifica fotos ni escenas Unity."""
from pathlib import Path
from html import escape
import json

ROOT = Path(__file__).parent
OUT = ROOT / "sketches"
OUT.mkdir(exist_ok=True)
TITLES = ["Entrada", "Aulas", "Baños", "Cafetería", "La Revisión", "Biblioteca", "Laboratorios", "Ordenadores", "Despachos", "El Tribunal"]
SLUGS = ["Entrada", "Aulas", "Banos", "Cafeteria", "Revision", "Biblioteca", "Laboratorios", "Ordenadores", "Despachos", "Tribunal"]
NOTES = [
    ["Piedra + ladrillo sobre el acceso", "Recepción a un lado; camino legible", "Escalera y descansillo inventados"],
    ["Pupitres bajos que se pueden saltar", "Pizarra y tarima como destino", "Coberturas para el profe de Cálculo"],
    ["Cabinas de madera y lavabos", "Azul para espejo y agua", "Charcos solo decorativos por ahora"],
    ["Pilares con cinturón de madera", "Mostrador de café al fondo", "Mesas como ruta secundaria"],
    ["Capilla reinterpretada como arena", "Bancos fuera del espacio de combate", "Atril y estrado, luz cálida"],
    ["Biblioteca inventada", "Estanterías que crean escondites", "Altillo y rincón de lectura"],
    ["Motores y bancadas amarillas", "Pasillo central de trabajo", "Vitrinas abiertas estilizadas"],
    ["Informática inventada", "Pantallas azules y servidores", "Reserva para mecánica eléctrica"],
    ["Verde, crema y marcos de madera", "Puertas repetidas a ambos lados", "Zona central para patrullas"],
    ["Aula magna provisional", "Gradas laterales y pantalla al fondo", "Se adaptará a la foto del aula grande"],
]
COLORS = {"verde":"#667d65","crema":"#eee6d6","madera":"#b1814f","azul":"#426b83","ambar":"#e9ae49","turquesa":"#4fa99e","piedra":"#a9a69e","oscuro":"#243c42"}

def project(x,y,z): return (420+x*8+z*5, 340+x*3-z*4-y*10)
def points(coords): return " ".join(f"{x:.1f},{y:.1f}" for x,y in coords)
def shade(color,factor): return "#"+"".join(f"{min(255,int(int(color[i:i+2],16)*factor)):02x}" for i in (1,3,5))
def box(x,z,w,d,h,color,base=0):
    c=COLORS.get(color,color)
    a,b,c1,d1=[project(xx,base,zz) for xx,zz in [(x-w/2,z-d/2),(x+w/2,z-d/2),(x+w/2,z+d/2),(x-w/2,z+d/2)]]
    aa,bb,cc,dd=[project(xx,base+h,zz) for xx,zz in [(x-w/2,z-d/2),(x+w/2,z-d/2),(x+w/2,z+d/2),(x-w/2,z+d/2)]]
    return f'<g stroke="#37443b" stroke-width="1" stroke-linejoin="round"><polygon points="{points([a,b,bb,aa])}" fill="{shade(c,.77)}"/><polygon points="{points([b,c1,cc,bb])}" fill="{shade(c,.90)}"/><polygon points="{points([aa,bb,cc,dd])}" fill="{c}"/></g>'

def objects(index):
    items=[]
    def add(x,z,w,d,h,c,base=0): items.append((x,z,w,d,h,c,base))
    if index==0:
        for side in [-1,1]: add(side*10.6,-14,6.4,1.1,7.2,"piedra")
        add(-5.5,-14,.9,1.5,5.8,"piedra"); add(5.5,-14,.9,1.5,5.8,"piedra")
        add(0,-14,11.8,1.1,.65,"piedra",5)
        # Apertura indicada en el alzado: el boceto no define la malla final del arco.
        add(-8,-1,6,1.5,1.5,"madera"); add(9,3,1,3.2,1.2,"madera")
        for x in [-4,2,8]: add(x,7,1,1.3,1.3,"piedra")
        add(8,19,5.6,5,1.5,"piedra")
    elif index in [1,3,7]:
        for row in range(5 if index!=3 else 4):
            for side in [-1,1]:
                z=-15+row*(6 if index==1 else 8 if index==3 else 7)
                add(side*6.5,z,2.8,1.6,.95,"crema" if index==1 else "madera")
                add(side*6.5,z-1.2,.65,.65,.7,"verde")
                if index==7: add(side*6.5,z+.25,1.2,.25,1.7,"azul")
        if index==1: add(0,20,16,6,.6,"madera"); add(0,28.7,11,.2,4,"crema")
        if index==3:
            for s in [-1,1]: add(s*6,22,5.4,1.5,1.5,"madera"); add(s*4,3,.8,.8,5,"crema")
        if index==7:
            for x in [-10,-5,0,5,10]: add(x,24,1.5,1.3,3,"oscuro")
    elif index==2:
        for z in [-14,-4,6,16]: add(-11,z,1.1,2.7,2.3,"crema"); add(10,z,3.6,3,2.6,"madera")
    elif index==4:
        for z in [-16,-12,-8,-4,0]:
            for s in [-1,1]: add(s*8.8,z,3.2,.85,1,"madera")
        add(0,23,17,8,.9,"madera"); add(-5,23,1.5,1.1,2,"madera")
    elif index==5:
        for z in [-16,-7,2,11]:
            for s in [-1,1]: add(s*7.5,z,.9,3,3.4,"madera"); add(s*11.7,z,.9,3,3.4,"verde")
        add(9,24,7,7,2,"madera")
        for x in [-7,0,7]: add(x,19,2.2,1.15,.9,"crema")
    elif index==6:
        for z in [-14,-4,6,16]:
            for s in [-1,1]: add(s*7.2,z,2.8,1.9,.3,"ambar"); add(s*7.2,z,1.4,.9,1.6,"piedra")
    elif index==8:
        for z in [-17,-8,1,10,19]:
            for s in [-1,1]: add(s*8.3,z,.2,5.5,3.4,"crema"); add(s*11,z,2.2,1.2,.9,"madera")
    else:
        for row in range(5):
            for s in [-1,1]: add(s*(7.4+row*.85),-8+row*6,3.6,4,.3+row*.25,"piedra")
        add(0,23,18,9,1,"madera"); add(0,28.7,12,.2,6,"oscuro")
    return sorted(items,key=lambda p:project(p[0],0,p[1])[1])

manifest=[]
for i,title in enumerate(TITLES):
    svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 900 650" role="img" aria-label="Boceto de '+escape(title)+'">',
         '<rect width="900" height="650" fill="#f4f0e6"/>',
         '<g fill="none" stroke="#d8d5c8" stroke-width=".5">'+''.join(f'<path d="M0 {y}H900"/>' for y in range(0,650,20))+'</g>',
         '<text x="40" y="45" font-family="Trebuchet MS, sans-serif" font-size="13" letter-spacing="3" fill="#667d65">IODP / CUADERNO DE CAMPUS</text>',
         f'<text x="40" y="87" font-family="Georgia,serif" font-size="34" fill="#243c42">{i+1:02} · {escape(title)}</text>',
         '<text x="40" y="112" font-family="Trebuchet MS,sans-serif" font-size="12" fill="#667d65">BOCETO DE INTENCIÓN · geometría simplificada, no captura de Unity</text>',
         box(0,2,28,56,.2,"crema"),box(-14,2,.25,56,1.4,"verde"),box(0,30,28,.3,4,"crema")]
    for item in objects(i): svg.append(box(*item))
    for j in range(3): svg.append(box(-3,-10+j*3,2.4,2,.55+j*.55,"ambar"))
    route=[project(0,.3,z) for z in range(-23,27,4)]
    svg.append(f'<polyline points="{points(route)}" fill="none" stroke="#e9ae49" stroke-width="4" stroke-dasharray="6 7"/>')
    for z,label,color in [(-23,"INICIO","#426b83"),(1,"CHECKPOINT","#4fa99e"),(26,"META","#4fa99e")]:
        x,y=project(0,.5,z)
        svg.append(f'<circle cx="{x}" cy="{y}" r="6" fill="{color}" stroke="#fff" stroke-width="2"/><text x="{x+12}" y="{y+4}" font-size="11" font-family="Trebuchet MS,sans-serif" fill="#243c42">{label}</text>')
    svg.append('<path d="M40 510H860" stroke="#b9b7a8"/>')
    for n,note in enumerate(NOTES[i]): svg.append(f'<text x="45" y="{542+n*25}" font-size="15" font-family="Trebuchet MS,sans-serif" fill="#37443b">{n+1}. {escape(note)}</text>')
    svg.append('<text x="680" y="620" font-family="Trebuchet MS,sans-serif" font-size="11" fill="#667d65">28 × 56 m / escala de prototipo</text></svg>')
    (OUT/f'{i+1:02}_{SLUGS[i]}.svg').write_text('\n'.join(svg),encoding='utf-8')
    manifest.append(dict(number=i+1,title=title,scene=f'IODP_{i+1:02}_{SLUGS[i]}',invented=i in [5,7],provisional=i==9,ideas=NOTES[i]))

(ROOT/'ambientes.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
cards=[]
for i,item in enumerate(manifest):
    label='Inventado' if item['invented'] else 'Foto pendiente' if item['provisional'] else 'Inspirado en fotos'
    cards.append(f'''<article id="nivel-{i+1}"><header><span>{i+1:02} / {label}</span><h2>{escape(item['title'])}</h2></header>
    <div class="pair"><figure><img src="../../Assets/_Project/IODP/Previews/{i+1:02}_{SLUGS[i]}.png" alt="Vista exportada de Unity de {escape(item['title'])}" onerror="this.parentNode.classList.add('pending');this.remove()"/><figcaption>Unity / vista del ambiente construido</figcaption></figure>
    <figure><img src="sketches/{i+1:02}_{SLUGS[i]}.svg" alt="Boceto {escape(item['title'])}"/><figcaption>Boceto de intención / SVG editable</figcaption></figure></div>
    <p>{' · '.join(escape(x) for x in item['ideas'])}</p><code>{item['scene']}.unity</code></article>''')
html='''<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>IODP · Cuaderno de ambientes</title>
<style>*{box-sizing:border-box}body{margin:0;background:#f4f0e6;color:#243c42;font:16px/1.6 system-ui,sans-serif}main{max-width:1500px;margin:auto;padding:55px 4vw}h1{font:clamp(45px,7vw,105px)/1.05 Georgia,serif;margin:15px 0 25px}h2{font:36px Georgia,serif;margin:6px 0 20px}.eyebrow,header span{font:12px system-ui;letter-spacing:2px;text-transform:uppercase;color:#667d65}.intro{max-width:750px}nav{display:flex;gap:15px;flex-wrap:wrap;padding:25px 0}a{color:inherit}article{border-top:1px solid #b9b7a8;padding:40px 0}.pair{display:grid;grid-template-columns:1.5fr 1fr;gap:20px}figure{margin:0;background:#e7e3d7;min-height:220px}img{width:100%;display:block;aspect-ratio:1.6;object-fit:contain}figcaption{padding:10px 15px;font-size:12px}.pending:before{content:'Vista del motor pendiente de exportar';display:block;padding:80px 30px;color:#667d65}code{font-size:12px}footer{border-top:1px solid #b9b7a8;padding-top:35px;color:#667d65}@media(max-width:800px){.pair{grid-template-columns:1fr}}</style>
<main><div class="eyebrow">Cum Laude / IODP / cuaderno 01</div><h1>Una escuela.<br>Diez mundos.</h1><p class="intro">Piedra, verde y madera. Tus fotos como punto de partida para construir una escuela ficticia. La vista del motor recoge los materiales y la escala de cada escena. El boceto documenta el diseño inicial. Son ambientes de prototipo: el combate y las mecánicas especiales se integrarán después.</p>'''
html+='<nav>'+''.join(f'<a href="#nivel-{i+1}">{i+1:02} {escape(t)}</a>' for i,t in enumerate(TITLES))+'</nav>'
html+='\n'.join(cards)+'<footer>Material original modular · vitrina de Sean Thomas y Eric Chadwick · madera de Dimitrios Savva · biblioteca, ordenadores y escaleras inventados · aula final provisional. <a href="CREDITOS_ARTE.md">Créditos de arte</a>.</footer></main></html>'
(ROOT/'index.html').write_text(html,encoding='utf-8')
print('10 bocetos SVG, ambientes.json y galería index.html creados.')
