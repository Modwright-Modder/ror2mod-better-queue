"""Optional source for the committed 256x256 procedural package icon (Pillow)."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
scale = 4
image = Image.new("RGB", (256 * scale, 256 * scale), "#101b26")
draw = ImageDraw.Draw(image)
def box(bounds, fill, radius=0):
    coords = tuple(int(v * scale) for v in bounds)
    if radius:
        draw.rounded_rectangle(coords, radius=radius*scale, fill=fill)
    else:
        draw.rectangle(coords, fill=fill)
def line(points, color, width):
    draw.line([(int(x*scale),int(y*scale)) for x,y in points], fill=color, width=width*scale, joint="curve")
box((18,18,238,238), "#223642", 22)
for y, color in [(46,"#f4ecd7"),(94,"#72d6c3"),(142,"#ecb666")]:
    box((39,y,69,y+30),color,8)
    box((84,y+3,178,y+12),"#dfebe8",4)
    box((84,y+20,150,y+25),"#65838a",2)
line([(203,54),(216,54),(216,192),(51,192)],"#57e0c4",8)
line([(67,177),(51,192),(67,207)],"#57e0c4",8)
image.resize((256,256), Image.Resampling.LANCZOS).save(root/"thunderstore/icon.png")
