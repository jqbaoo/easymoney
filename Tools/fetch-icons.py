# -*- coding: utf-8 -*-
"""从 Iconify 批量拉界面图标，转成 Unity 能吃的 128x128 透明底 PNG。

为什么必须是白色：界面靠 Image.color 给图标染色（标签栏选中染主色、未选中染弱化色），
而 Image.color 与贴图是乘算关系——只有近白的贴图才能被染成任意颜色。
拉成黑色的图，乘完还是黑的，染色等于失效。

用法：
    python Tools/fetch-icons.py                 # 拉全部
    python Tools/fetch-icons.py --list          # 只打印映射，不下载
    python Tools/fetch-icons.py --only tab_record,cat_food
    python Tools/fetch-icons.py --set lucide    # 换图标库重拉（署名见 图标资源库.md）
"""

import argparse
import pathlib
import sys
import urllib.error
import urllib.request

try:
    import cairosvg
except ImportError:
    sys.exit("缺少 cairosvg，先跑：pip install cairosvg")

API = "https://api.iconify.design"
SIZE = 128
COLOR = "%23FFFFFF"

# Iconify 会把 urllib 的默认 UA（Python-urllib/3.x）挡在 403，必须自带一个。
# 内容不限，实测 curl/8.9.1 和 EasyMoney/1.0 都能过，只有不设 UA 会被拒。
UA = "EasyMoney-fetch-icons/1.0"

OUT_DIR = pathlib.Path(__file__).resolve().parent.parent / "Assets" / "Resources" / "Icons"

# 文件名（= IconNames.cs 里的常量）→ Iconify 图标 ID（不含图标库前缀）
#
# 前 9 个界面已经留好图标位，丢进去即生效；
# cat_* 那 15 个目前**没有任何页面调用**，要先接代码才会显示（见 IconNames.ForCategory）。
ICONS = {
    # ── 底部标签栏 ──
    "tab_record": "pencil-plus",
    "tab_list": "receipt",
    "tab_account": "wallet",
    "tab_report": "chart-pie",
    # ── 通用 ──
    "chevron_left": "chevron-left",
    "chevron_right": "chevron-right",
    "icon_add": "plus",
    "icon_edit": "pencil",
    "icon_delete": "trash",
    # ── 分类（待接代码）──
    "cat_food": "tools-kitchen-2",
    "cat_shopping": "shopping-cart",
    "cat_transport": "bus",
    "cat_housing": "home",
    "cat_entertainment": "device-gamepad-2",
    "cat_medical": "medical-cross",
    "cat_education": "school",
    "cat_communication": "phone",
    "cat_social": "heart-handshake",
    "cat_other": "dots",
    "cat_salary": "cash",
    "cat_bonus": "award",
    "cat_parttime": "briefcase",
    "cat_investment": "trending-up",
    "cat_redpacket": "gift",
}


def _fetch(icon_set, icon_id):
    """拉一个图标的 SVG 原始字节。图标不存在时 Iconify 返回 404。"""
    url = f"{API}/{icon_set}:{icon_id}.svg?color={COLOR}&width={SIZE}"
    request = urllib.request.Request(url, headers={"User-Agent": UA})
    with urllib.request.urlopen(request, timeout=30) as response:
        return response.read()


def main():
    parser = argparse.ArgumentParser(description="从 Iconify 拉取界面图标")
    parser.add_argument("--set", default="tabler", help="图标库前缀，默认 tabler")
    parser.add_argument("--out", default=str(OUT_DIR), help="输出目录")
    parser.add_argument("--only", default="", help="只处理这些名字，逗号分隔")
    parser.add_argument("--list", action="store_true", help="只打印映射，不下载")
    args = parser.parse_args()

    wanted = [s.strip() for s in args.only.split(",") if s.strip()]
    targets = {k: v for k, v in ICONS.items() if not wanted or k in wanted}

    if args.list:
        for name, icon_id in targets.items():
            print(f"{name:22} <- {args.set}:{icon_id}")
        print(f"\n共 {len(targets)} 个")
        return 0

    out_dir = pathlib.Path(args.out)
    out_dir.mkdir(parents=True, exist_ok=True)

    ok, failed = 0, []

    for name, icon_id in targets.items():
        try:
            svg = _fetch(args.set, icon_id)
        except urllib.error.HTTPError as e:
            hint = {404: "图标名不存在", 403: "被拒，检查 UA"}.get(e.code, "")
            failed.append(f"{name} <- {args.set}:{icon_id}  HTTP {e.code} {hint}".rstrip())
            continue
        except Exception as e:
            failed.append(f"{name} <- {args.set}:{icon_id}  {e}")
            continue

        cairosvg.svg2png(
            bytestring=svg,
            write_to=str(out_dir / f"{name}.png"),
            output_width=SIZE,
            output_height=SIZE,
        )
        ok += 1
        print(f"[{ok}/{len(targets)}] {name}.png")

    print(f"\n成功 {ok} 个 → {out_dir}")
    if failed:
        print(f"失败 {len(failed)} 个：")
        for line in failed:
            print("  " + line)
        return 1

    print("提醒：新图 Unity 会自动生成 .meta，记得一起提交；忘了就是引用全断。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
