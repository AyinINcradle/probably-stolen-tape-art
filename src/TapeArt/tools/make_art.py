"""生成模组像素美术：艺术品灯牌 32x16，磁带播放机核心 16x16。"""
import os
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Textures", "Items")
os.makedirs(OUT, exist_ok=True)


def sign() -> Image.Image:
    """2x2 格的霓虹灯牌：淡紫外框 + 里面的磁带。32x32 = 2x2 个 16px 格。"""
    img = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    glow = (190, 140, 255, 55)
    neon = (214, 176, 255, 255)
    neon_dim = (214, 176, 255, 110)
    hot = (245, 228, 255, 255)
    body = (26, 14, 40, 255)

    # 外发光
    d.rectangle([1, 2, 30, 30], fill=glow)
    d.rectangle([2, 1, 29, 31], fill=glow)

    # 外框：一圈霓虹线条，四角切掉
    d.rectangle([3, 3, 28, 28], outline=neon)
    for x, y in ((3, 3), (28, 3), (3, 28), (28, 28)):
        d.point((x, y), (0, 0, 0, 0))
    # 内侧第二道细线，做出双层灯管的层次
    d.rectangle([5, 5, 26, 26], outline=neon_dim)

    # 顶部挂架
    d.line([15, 1, 15, 3], fill=neon)
    d.line([16, 1, 16, 3], fill=neon)
    d.line([13, 1, 18, 1], fill=neon_dim)

    # 磁带本体
    d.rectangle([7, 9, 24, 23], fill=body)
    d.rectangle([7, 9, 24, 23], outline=neon)

    # 上方标签条
    d.line([9, 11, 22, 11], fill=neon_dim)
    d.line([9, 12, 15, 12], fill=neon_dim)

    # 左右卷盘
    d.ellipse([9, 14, 14, 19], outline=hot)
    d.ellipse([17, 14, 22, 19], outline=hot)
    d.point((11, 16), hot)
    d.point((19, 16), hot)

    # 底部磁带窗
    d.rectangle([11, 20, 20, 22], outline=neon_dim)
    d.line([12, 21, 19, 21], fill=neon_dim)

    # 外框内的小装饰点
    d.point((4, 6), hot)
    d.point((27, 6), hot)
    d.point((4, 25), hot)
    d.point((27, 25), hot)
    return img


def core() -> Image.Image:
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    pin = (150, 118, 198, 255)
    chip_body = (74, 52, 96, 255)
    chip_edge = (196, 152, 255, 255)
    chip_core = (236, 214, 255, 255)

    # 引脚
    d.line([3, 2, 12, 2], fill=pin)
    d.line([3, 13, 12, 13], fill=pin)
    for i in range(3):
        y = 4 + i * 3
        d.line([1, y, 2, y], fill=pin)
        d.line([13, y, 14, y], fill=pin)

    # 芯片本体
    d.rectangle([3, 3, 12, 12], fill=chip_body, outline=chip_edge)
    d.line([6, 6, 9, 6], fill=chip_core)
    d.line([6, 8, 9, 8], fill=chip_core)
    d.line([5, 10, 10, 10], fill=chip_edge)
    return img


def preview() -> Image.Image:
    """创意工坊封面：干净构图，只保留灯牌 + 大字号中文标题 + 小字号英文。"""
    from PIL import ImageFont, ImageFilter

    W, H = 512, 288
    img = Image.new("RGBA", (W, H), (16, 11, 24, 255))
    d = ImageDraw.Draw(img)

    # 背景：干净的纵向渐变，不加网格
    for y in range(H):
        t = y / (H - 1)
        d.line([(0, y), (W, y)], fill=(int(16 + 10 * t), int(11 + 8 * t), int(24 + 18 * t), 255))

    # 标题后方的一团柔光
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([96, 6, 416, 210], fill=(150, 100, 235, 70))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(48)))

    # 灯牌本体（等比放大，居中偏上）
    sign_img = sign().resize((128, 128), Image.NEAREST)
    halo = Image.new("RGBA", (200, 200), (0, 0, 0, 0))
    ImageDraw.Draw(halo).ellipse([20, 20, 180, 180], fill=(190, 140, 255, 90))
    img.alpha_composite(halo.filter(ImageFilter.GaussianBlur(26)), (156, 4))
    img.alpha_composite(sign_img, (192, 28))

    cn_bold = ImageFont.truetype("C:/Windows/Fonts/msyhbd.ttc", 52)
    cn_small = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 22)
    en = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 13)

    d.text((W / 2, 186), "磁带艺术", fill=(238, 226, 255, 255), font=cn_bold, anchor="mm")
    d.text((W / 2, 226), "可能是磁带增强", fill=(198, 168, 240, 255), font=cn_small, anchor="mm")
    d.text((W / 2, 256), "T A P E   A R T   ·   M A Y B E   T A P E   E N H A N C E M E N T",
           fill=(140, 120, 170, 255), font=en, anchor="mm")
    return img



def icon() -> Image.Image:
    """16x16 磁带小图标，用作升级图标的兜底。"""
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    body = (34, 22, 48, 255)
    edge = (206, 172, 255, 255)
    reel = (240, 230, 255, 255)
    dim = (170, 140, 210, 255)

    d.rectangle([1, 3, 14, 12], fill=body)
    d.rectangle([1, 3, 14, 12], outline=edge)
    d.line([3, 5, 12, 5], fill=dim)
    d.ellipse([3, 7, 6, 10], outline=reel)
    d.ellipse([9, 7, 12, 10], outline=reel)
    d.point((4, 8), reel)
    d.point((10, 8), reel)
    d.rectangle([6, 10, 9, 11], outline=dim)
    return img
if __name__ == "__main__":
    base = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    sign().save(os.path.join(OUT, "tape_art_sign.png"))
    core().save(os.path.join(OUT, "tape_art_core.png"))
    icon().save(os.path.join(OUT, "tape_art_icon.png"))
    preview().convert("RGB").save(os.path.join(base, "preview.png"))
    for name in sorted(os.listdir(OUT)):
        print(name, os.path.getsize(os.path.join(OUT, name)), "bytes")
    print("preview.png", os.path.getsize(os.path.join(base, "preview.png")), "bytes")

