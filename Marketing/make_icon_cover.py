# 用游戏自带的印章 Logo + 实机截图合成：图标 / 竖版封面 / 横版 banner
from PIL import Image, ImageDraw, ImageFilter, ImageFont

FONT = r"D:\GufaCodingNext\Assets\GufaCoding\Resources\GufaFont.otf"
SEAL = r"D:\GufaCodingNext\Assets\GufaCoding\Resources\GufaLogo.png"
SHOT_BANNER = r"D:\GufaCodingNext\Marketing\Screenshots\04-stage.png"
SHOT_COVER = r"D:\GufaCodingNext\Marketing\Screenshots\02-levels.png"
OUT = r"D:\GufaCodingNext\Marketing"

BG = (22, 22, 24)
COMMENT = (106, 153, 85)
TEXT = (226, 226, 226)
DIM = (150, 150, 150)
TEAL = (86, 227, 201)


def font(size):
    return ImageFont.truetype(FONT, size)


def fit(draw, text, max_w, size):
    while size > 14:
        f = font(size)
        b = draw.textbbox((0, 0), text, font=f)
        if b[2] - b[0] <= max_w:
            return f
        size -= 3
    return font(14)


def rounded(img, radius):
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, img.size[0], img.size[1]), radius=radius, fill=255)
    out = img.convert("RGBA")
    out.putalpha(mask)
    return out


def drop_shadow(size, radius, blur=24, alpha=180):
    w, h = size
    sh = Image.new("RGBA", (w + blur * 4, h + blur * 4), (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle(
        (blur * 2, blur * 2, blur * 2 + w, blur * 2 + h), radius=radius, fill=(0, 0, 0, alpha))
    return sh.filter(ImageFilter.GaussianBlur(blur))


def paste_center(canvas, layer, cx, cy):
    canvas.paste(layer, (int(cx - layer.width / 2), int(cy - layer.height / 2)), layer)


def glow_bg(w, h):
    """深色底 + 中心微光 + 极淡的代码行纹理"""
    img = Image.new("RGB", (w, h), BG)
    glow = Image.new("L", (w, h), 0)
    gd = ImageDraw.Draw(glow)
    r = int(min(w, h) * 0.62)
    gd.ellipse((w // 2 - r, h // 2 - r, w // 2 + r, h // 2 + r), fill=46)
    glow = glow.filter(ImageFilter.GaussianBlur(int(min(w, h) * 0.22)))
    img = Image.composite(Image.new("RGB", (w, h), (34, 34, 38)), img, glow)

    d = ImageDraw.Draw(img)
    f = font(max(14, int(h * 0.018)))
    y = int(h * 0.05)
    step = int(h * 0.045)
    i = 0
    while y < h:
        indent = int(w * (0.03 if i % 3 else 0.09))
        d.text((indent, y), "/* 古法编程 */ int main() { return 0; }", font=f, fill=(29, 29, 32))
        y += step
        i += 1
    return img


def chips_row(draw, canvas_w, chips, cy, size=34):
    f = font(size)
    widths = []
    for c in chips:
        b = draw.textbbox((0, 0), c, font=f)
        widths.append(b[2] - b[0] + 44)
    gap = 22
    total = sum(widths) + gap * (len(chips) - 1)
    x = (canvas_w - total) / 2
    for c, cw in zip(chips, widths):
        draw.rounded_rectangle((x, cy, x + cw, cy + 62), radius=31, outline=TEAL, width=3)
        draw.text((x + cw / 2, cy + 31), c, font=f, fill=TEAL, anchor="mm")
        x += cw + gap


seal = Image.open(SEAL).convert("RGBA")

# ---------------- 图标 ----------------
S = 1024
icon = glow_bg(S, S)
seal_icon = seal.copy()
seal_icon.thumbnail((int(S * 0.66), int(S * 0.66)), Image.LANCZOS)
seal_icon = seal_icon.rotate(-5, resample=Image.BICUBIC, expand=True)
paste_center(icon, drop_shadow(seal_icon.size, 40, blur=26, alpha=190), S / 2, S / 2 + 6)
paste_center(icon, seal_icon, S / 2, S / 2)
icon.convert("RGB").save(rf"{OUT}\icon-1024.png", optimize=True)
icon.resize((512, 512), Image.LANCZOS).convert("RGB").save(rf"{OUT}\icon-512.png", optimize=True)

# ---------------- 竖版封面 1080x1920 ----------------
W, H = 1080, 1920
cover = glow_bg(W, H)
d = ImageDraw.Draw(cover)

seal_c = seal.copy()
seal_c.thumbnail((225, 225), Image.LANCZOS)
seal_c = seal_c.rotate(-4, resample=Image.BICUBIC, expand=True)
paste_center(cover, drop_shadow(seal_c.size, 18, blur=18, alpha=170), W / 2, 215)
paste_center(cover, seal_c, W / 2, 215)

title_f = fit(d, "古法编程模拟器", 900, 100)
d.text((W / 2, 420), "古法编程模拟器", font=title_f, fill=(255, 255, 255), anchor="mm",
       stroke_width=6, stroke_fill=(0, 0, 0))
d.text((W / 2, 492), "// AI 时代，最后的手工程序员", font=font(36), fill=COMMENT, anchor="mm")

# 手机实机截图（用标题页：品牌感更强，且没有大色块）
cover_shot = Image.open(SHOT_COVER).convert("RGB")
mock_w = 600
mock = cover_shot.resize((mock_w, int(cover_shot.height * mock_w / cover_shot.width)), Image.LANCZOS)
mock = rounded(mock, 26)
mock_cy = 492 + 50 + mock.height / 2
paste_center(cover, drop_shadow(mock.size, 26, blur=28, alpha=200), W / 2, mock_cy)
paste_center(cover, mock, W / 2, mock_cy)

chips_row(d, W, ["找 Bug", "补代码", "掉头发", "背锅上线"], int(mock_cy + mock.height / 2) + 46)
d.text((W / 2, 1856), "第一章 · C 语言时代", font=font(30), fill=DIM, anchor="mm")
cover.save(rf"{OUT}\cover-1080x1920.png", optimize=True)

# ---------------- 横版 banner 1920x1080 ----------------
BW, BH = 1920, 1080
banner = glow_bg(BW, BH)
bd = ImageDraw.Draw(banner)

seal_b = seal.copy()
seal_b.thumbnail((180, 180), Image.LANCZOS)
seal_b = seal_b.rotate(-4, resample=Image.BICUBIC, expand=True)
paste_center(banner, seal_b, 210, 185)

bf = fit(bd, "古法编程模拟器", 980, 112)
bd.text((120, 400), "古法编程模拟器", font=bf, fill=(255, 255, 255), anchor="ls",
        stroke_width=6, stroke_fill=(0, 0, 0))
bd.text((124, 466), "// AI 时代，最后的手工程序员", font=font(38), fill=COMMENT, anchor="ls")

f2 = font(34)
bx, bcy = 124, 540
for c in ["找 Bug", "补代码", "掉头发", "背锅上线"]:
    b = bd.textbbox((0, 0), c, font=f2)
    cw = b[2] - b[0] + 44
    bd.rounded_rectangle((bx, bcy, bx + cw, bcy + 62), radius=31, outline=TEAL, width=3)
    bd.text((bx + cw / 2, bcy + 31), c, font=f2, fill=TEAL, anchor="mm")
    bx += cw + 22

bd.text((124, 720), "10 关 · 限时交付，点错扣头发", font=font(38), fill=TEXT, anchor="ls")
bd.text((124, 782), "没有 AI，没有自动补全，只有你和编译器。", font=font(34), fill=DIM, anchor="ls")

banner_shot = Image.open(SHOT_BANNER).convert("RGB")
mock2 = banner_shot.resize((int(banner_shot.width * 880 / banner_shot.height), 880), Image.LANCZOS)
mock2 = rounded(mock2, 24)
paste_center(banner, drop_shadow(mock2.size, 24, blur=28, alpha=200), 1490, BH / 2 + 10)
paste_center(banner, mock2, 1490, BH / 2 + 10)
banner.save(rf"{OUT}\banner-1920x1080.png", optimize=True)

print("done")
