"""Generate original geometric OpenType test fonts. Requires fonttools (tested with 4.60.1).

No third-party outlines: the rectangles/triangles below are created for Swealium tests.
The committed binary is used by tests; fonttools is only required to regenerate it.
"""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString
from fontTools.designspaceLib import DesignSpaceDocument, AxisDescriptor, SourceDescriptor
from fontTools.varLib import build
from fontTools.ttLib import TTFont, TTCollection
from io import BytesIO
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "tests/Swealium.Tests/Fixtures/Fonts/SwealiumTestVariable.ttf"
ORDER = [".notdef", "space", "A", "V", "f", "i", "fi", "A.alt", "zero", "one"]


def master(path, weight):
    fb = FontBuilder(1000, isTTF=True)
    fb.setupGlyphOrder(ORDER)
    fb.setupCharacterMap({32:"space", 65:"A", 86:"V", 102:"f", 105:"i", 48:"zero", 49:"one"})
    width = 600 if weight == 400 else 900
    glyphs, metrics = {}, {}
    for name in ORDER:
        pen = TTGlyphPen(None)
        advance = 300 if name == "space" else width
        if name != "space":
            if name == "A.alt":
                points = [(60,0), (width-60,0), (width/2,700)]
            else:
                points = [(60,0), (width-60,0), (width-60,700), (60,700)]
            pen.moveTo(points[0])
            for point in points[1:]:
                pen.lineTo(point)
            pen.closePath()
        glyphs[name] = pen.glyph()
        metrics[name] = (advance, 60 if name != "space" else 0)
    fb.setupGlyf(glyphs)
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=800, descent=-200)
    fb.setupNameTable({"familyName":"Swealium Test Variable", "styleName":"Regular" if weight==400 else "Bold",
                       "uniqueFontIdentifier":f"SwealiumTest-{weight}", "fullName":f"Swealium Test {weight}",
                       "psName":f"SwealiumTest-{weight}", "version":"Version 1.0"})
    fb.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800, usWinDescent=200,
                usWeightClass=weight, sxHeight=500, sCapHeight=700)
    fb.setupPost()
    fb.setupMaxp()
    addOpenTypeFeaturesFromString(fb.font, """
        languagesystem DFLT dflt;
        languagesystem latn dflt;
        languagesystem latn SRB;
        feature liga { sub f i by fi; } liga;
        feature kern { pos A V -120; } kern;
        feature ss01 { sub A by A.alt; } ss01;
        feature tnum { pos one <0 0 100 0>; } tnum;
        feature locl { script latn; language SRB; sub V by A.alt; } locl;
    """)
    fb.font["head"].created = fb.font["head"].modified = 3039529600
    fb.font.recalcTimestamp = False
    fb.font.save(path)


directory = ROOT / "artifacts" / "font-fixture-masters"
directory.mkdir(parents=True, exist_ok=True)
design = DesignSpaceDocument()
axis = AxisDescriptor()
axis.name, axis.tag = "Weight", "wght"
axis.minimum, axis.default, axis.maximum = 400, 400, 900
design.addAxis(axis)
for weight in [400, 900]:
    path = directory / f"master-{weight}.ttf"
    master(path, weight)
    source = SourceDescriptor()
    source.path = str(path)
    source.name = f"master-{weight}"
    source.location = {"Weight":weight}
    source.copyInfo = source.copyLib = source.copyFeatures = weight == 400
    design.addSource(source)
font, _, _ = build(design)
font["head"].created = font["head"].modified = 3039529600
font.recalcTimestamp = False
output = BytesIO()
font.save(output)
content = output.getvalue()
if "--check" in sys.argv:
    if not TARGET.exists() or TARGET.read_bytes() != content:
        raise SystemExit("Font fixture differs; regenerate with this script.")
else:
    TARGET.parent.mkdir(parents=True, exist_ok=True)
    TARGET.write_bytes(content)
print(f"{'Verified' if '--check' in sys.argv else 'Generated'} {TARGET.name}: {len(content)} bytes")
collection = TTCollection()
collection.fonts = [TTFont(directory / f"master-{weight}.ttf", recalcTimestamp=False) for weight in [400, 900]]
output = BytesIO()
collection.save(output)
content = output.getvalue()
target = TARGET.with_name("SwealiumTestCollection.ttc")
if "--check" in sys.argv:
    if not target.exists() or target.read_bytes() != content:
        raise SystemExit("Collection font fixture differs; regenerate with this script.")
else:
    target.write_bytes(content)
print(f"{'Verified' if '--check' in sys.argv else 'Generated'} {target.name}: {len(content)} bytes")
