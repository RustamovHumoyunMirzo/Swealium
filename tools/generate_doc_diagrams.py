"""Generate accessible, dependency-free SVG documentation figures. Run with --check for CI."""
from pathlib import Path
from html import escape
import sys

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "images"

def text(x, y, value, size=18, fill="#24364b", anchor="start", weight="400"):
    return f'<text x="{x}" y="{y}" font-size="{size}" fill="{fill}" text-anchor="{anchor}" font-weight="{weight}">{escape(value)}</text>'

def box(x, y, w, h, label, fill="#eaf2ff", stroke="#6286ba", radius=12):
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{radius}" fill="{fill}" stroke="{stroke}" stroke-width="2"/>'
            + text(x+w/2, y+h/2+6, label, 19, anchor="middle", weight="600"))

def arrow(x1, y1, x2, y2, color="#6286ba"):
    return f'<path d="M{x1} {y1} L{x2} {y2}" fill="none" stroke="{color}" stroke-width="2.5" marker-end="url(#arrow)"/>'

def frame(title, description, body, height=580):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="1040" height="{height}" viewBox="0 0 1040 {height}" role="img" aria-labelledby="title description">
<title id="title">{escape(title)}</title>
<desc id="description">{escape(description)}</desc>
<defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10z" fill="#6286ba"/></marker></defs>
<rect width="1040" height="{height}" fill="#f8fafc"/>
<g font-family="Segoe UI, Arial, sans-serif">
{text(36, 48, title, 28, weight="700")}
{body}
</g></svg>
'''

figures = {}
body = ""
for x, label in [(40, "Native event queue"), (380, "Window ID routing"), (720, "Input + visual tree")]:
    body += box(x, 100, 280, 76, label)
body += arrow(320,138,380,138) + arrow(660,138,720,138)
body += arrow(860,176,860,242)
for x, label, fill in [(40,"CPU Skia surface","#e6f5ef"), (380,"Widget Draw callbacks","#e6f5ef"), (720,"Logical measure / arrange","#e6f5ef")]:
    body += box(x, 245, 280, 76, label, fill, "#4a9784")
body += arrow(720,283,660,283)+arrow(380,283,320,283)
body += arrow(180,321,180,388)
body += box(40,390,280,76,"Upload RGBA texture","#fff2d9","#bd8a35")
body += box(380,390,280,76,"SDL presentation","#fff2d9","#bd8a35")
body += box(720,390,280,76,"OS window / display","#fff2d9","#bd8a35")
body += arrow(320,428,380,428)+arrow(660,428,720,428)
body += text(40,525,"One UI loop. Separate surface, texture, and input tree per window.",20)
body += text(40,554,"Layout is cached; visible raster frames currently render continuously.",17)
figures["rendering-pipeline.svg"] = frame("Application-to-pixel pipeline", "Events route by portable window ID into logical layout and widget callbacks, then CPU pixels upload to SDL presentation.", body)

body = box(42,96,956,414,"","#eef1f6","#99a7b6")
body += text(66,126,"Container border box",20,weight="600")
body += '<rect x="140" y="157" width="750" height="310" rx="10" fill="#d7eee7" stroke="#4a9784" stroke-width="2"/>'
body += text(160,185,"Container padding",19)
body += '<rect x="250" y="205" width="530" height="224" rx="10" fill="#fff2d9" stroke="#bd8a35" stroke-width="2" stroke-dasharray="8 5"/>'
body += text(270,232,"Child margin (outside)",18)
body += '<rect x="300" y="250" width="430" height="140" rx="18" fill="#dce9ff" stroke="#6286ba" stroke-width="2"/>'
body += text(515,280,"Child Bounds / actual size",19,anchor="middle",weight="600")
body += '<rect x="345" y="302" width="340" height="58" rx="8" fill="#ffffff" stroke="#4a9784" stroke-width="2"/>'
body += text(515,337,"Child content (inside padding)",18,anchor="middle")
body += text(42,546,"DesiredSize includes margin. ActualWidth/Height include padding, but exclude margin.",18)
figures["box-model.svg"] = frame("Layout box model", "Nested regions distinguish container padding, a child's outer margin, its actual border box, and its own content area.",body)

body = ""
for x,y,label in [(40,100,"Vertical"),(550,100,"Horizontal"),(40,330,"Overlay"),(550,330,"Absolute")]:
    body += box(x,y,450,190,"","#ffffff","#99a7b6")
    body += text(x+18,y+28,label,21,weight="600")
for i in range(3):
    body += box(65,145+i*43,380,31,chr(65+i),"#e6f5ef","#4a9784",5)
    body += box(575+i*126,155,112,90,chr(65+i),"#e6f5ef","#4a9784",5)
body += text(470,196,"gap",14,anchor="end")
body += box(80,386,200,90,"A","#eaf2ff")
body += box(190,415,200,82,"B / higher Z","#e6f5ef","#4a9784")
body += box(575,384,110,50,"X=0, Y=0","#eaf2ff")
body += box(737,435,230,55,"X=160, Y=50","#e6f5ef","#4a9784")
body += text(40,557,"Flow order follows insertion. Paint/hit order follows ZIndex. Only stacks use Gap/Grow.",17)
figures["layout-modes.svg"] = frame("Four container layout modes", "Vertical and horizontal stacks allocate sequential slots, overlay shares slots, absolute uses offsets; z order is independent.",body)

body = box(55,105,470,370,"")
body += text(78,142,"Window: logical coordinates",20,weight="600")
body += box(105,175,365,242,"","#e6f5ef","#4a9784")
body += text(122,204,"Panel at (10, 20)",18)
body += box(160,245,230,95,"Button at (5, 6)")
body += '<circle cx="190" cy="280" r="7" fill="#be493d"/>'
body += text(92,512,"Window point (20, 30) becomes button-local (5, 4).",17)
body += text(600,117,"Hit-test down; bubble up",24,weight="600")
for y,label in [(160,"Root: ancestor"),(285,"Panel: parent"),(410,"Button: Source")]:
    body += box(620,y,350,70,label)
body += arrow(795,285,795,230)+arrow(795,410,795,355)
body += text(600,525,"CurrentTarget changes; Position is local.",17)
body += text(600,550,"Handled stops later bubbling.",17)
figures["input-routing.svg"] = frame("Input routing and local coordinates", "A pointer hit targets a nested button; events bubble to panel and root while local coordinates change and the window point remains constant.",body)

body = box(55,105,380,80,"Independent editor A")
body += box(55,280,170,72,"Owned tools")
body += box(265,280,170,72,"Modal dialog","#fff2d9","#bd8a35")
body += box(265,420,170,72,"Nested dialog","#fff2d9","#bd8a35")
body += arrow(150,185,140,280)+arrow(340,185,350,280)+arrow(350,352,350,420)
body += box(590,105,390,80,"Independent editor B")
body += text(590,239,"Own tree, input, surface, and renderer.",18)
body += text(590,279,"Unaffected by editor A's modality.",18)
body += text(590,359,"Closing A destroys descendants first.",18,"#a23f35")
body += text(590,399,"B remains alive until separately closed.",18)
body += text(55,548,"Ownership is a separate native-window tree, not the widget visual tree.",18)
figures["window-ownership.svg"] = frame("Window ownership and modality", "Two independent editors exist; one owns tools and a nested modal chain. Only its group is blocked and closing it destroys descendants before their owner.",body)

body = box(40,105,430,110,"window.Id : uint")
body += text(60,256,"Portable SDL event-routing identity.",20)
body += text(60,292,"Use for dispatch/logging, not OS calls.",18)
body += box(560,105,440,110,"NativeWindowHandle","#fff2d9","#bd8a35")
body += box(570,260,200,74,"Kind","#fff2d9","#bd8a35")
body += box(790,260,200,74,"Value","#fff2d9","#bd8a35")
body += box(570,360,420,70,"Display (X11 / Wayland)","#fff2d9","#bd8a35")
body += text(570,470,"Borrowed OS pointer or numeric identifier.",18)
body += text(570,502,"Query again before native use; never free.",18)
body += text(40,554,"One query API does not make HWND, X11 Window, and wl_surface interchangeable.",18)
figures["native-identifiers.svg"] = frame("Routing identity versus native interop", "The portable routing ID is separate from a borrowed native handle descriptor with system kind, value, and optional display connection.",body)

body = '<rect x="80" y="120" width="390" height="280" fill="#fbe9e6" stroke="#be493d" stroke-dasharray="8 6" stroke-width="2"/>'
body += '<path d="M180 120 H450 Q470 120 470 140 V320 Q470 400 390 400 H90 Q80 400 80 390 V220 Q80 120 180 120Z" fill="#dce9ff" stroke="#6286ba" stroke-width="3"/>'
for x,y in [(92,132),(458,388)]:
    body += f'<circle cx="{x}" cy="{y}" r="8" fill="#be493d"/>'
body += '<circle cx="270" cy="250" r="8" fill="#2c8b76"/>'
body += text(100,445,"Red: cut-outs do not hover/click.",20,"#a23f35")
body += text(100,479,"Green: inside the analytic shape.",20,"#287c69")
body += text(555,138,"Clockwise constructor order",24,weight="600")
for y,label in [(205,"1. Top-left"),(250,"2. Top-right"),(295,"3. Bottom-right"),(340,"4. Bottom-left")]:
    body += text(580,y,label,22)
body += text(555,418,"Adjacent radii share one fit scale.",18)
body += text(555,452,"Drawing, clipping, and input agree.",18)
body += text(40,548,"Rounded parent clipping also excludes descendant input in the same cut-outs.",18)
figures["rounded-hit-testing.svg"] = frame("Asymmetric radii and hit regions", "Four independent corner radii define a shared visible and analytic input shape. Red points in removed corners do not activate the widget.",body)

body = box(60,120,904,296,"","#eef1f6","#99a7b6")
body += text(160,98,"Fixed: 160",19,anchor="middle",weight="600")
body += text(368,98,"Fraction: 1",19,anchor="middle",weight="600")
body += text(720,98,"Fraction: 2",19,anchor="middle",weight="600")
body += box(80,140,864,60,"Content row — Header: GridColumnSpan = 3")
body += box(80,216,160,180,"Sidebar","#e6f5ef","#4a9784")
body += box(256,216,688,180,"Body: row 1, column 1, column span 2","#e6f5ef","#4a9784")
body += '<path d="M480 216 V396" stroke="#6286ba" stroke-dasharray="7 6" stroke-width="2"/>'
body += text(60,460,"Spans include internal gaps; margins are inside the complete cell slot.",19)
body += text(60,500,"Track limits constrain sizes. Fraction weights divide the remaining finite budget.",18)
body += text(60,540,"An undefined axis begins with one fractional track; extra implicit tracks size to content.",17)
figures["grid-layout.svg"] = frame("Grid tracks and spanning cells", "A content-sized spanning header sits above a fixed sidebar and a body spanning one-share and two-share fractional columns. Padding and gaps surround the track slots.",body)

OUT.mkdir(parents=True, exist_ok=True)
for name, value in figures.items():
    target = OUT / name
    if "--check" in sys.argv:
        if not target.exists() or target.read_text(encoding="utf-8") != value:
            raise SystemExit(f"SVG is stale: {target}. Run this generator.")
    else:
        target.write_text(value, encoding="utf-8", newline="\n")
print(f"{'Verified' if '--check' in sys.argv else 'Generated'} {len(figures)} SVG diagrams.")

