"""Fetch sound effects from Freesound (public HQ previews), CC0 first, CC-BY fallback.
  python fetch_sounds.py [only=name1,name2]
Writes Assets/_Project/Audio/<name>[_n].mp3 and Tools/sound_credits.json
"""
import html
import json
import os
import re
import sys
import time
import urllib.parse
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Audio")
CREDITS = os.path.join(ROOT, "Tools", "sound_credits.json")
UA = {"User-Agent": "Mozilla/5.0 (SyncRooms-Liminal asset fetcher)"}
LICENSES = [("CC0", '"Creative Commons 0"'), ("CC-BY 4.0", '"Attribution"')]

args = dict(a.split("=", 1) for a in sys.argv[1:] if "=" in a)
ONLY = set(args["only"].split(",")) if "only" in args else None

ITEM = re.compile(
    r'data-similar-sounds-modal-url="/people/(?P<user>[^/]+)/sounds/(?P<id>\d+)/similar/.*?'
    r'data-mp3="(?P<mp3>[^"]+)".*?data-title="(?P<title>[^"]*)".*?data-duration="(?P<dur>[\d.]+)".*?'
    r'data-num-downloads="(?P<dl>\d+)"', re.S)


def get(url):
    for attempt in range(4):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60) as r:
                return r.read()
        except Exception:
            if attempt == 3:
                raise
            time.sleep(2 + attempt * 3)


def search(query, lic_filter, dmin, dmax):
    f = f"license:{lic_filter} duration:[{dmin} TO {dmax}]"
    url = "https://freesound.org/search/?" + urllib.parse.urlencode({"q": query, "f": f, "s": "Downloads"})
    page = get(url).decode("utf-8", "replace")
    res = []
    for m in ITEM.finditer(page):
        d = m.groupdict()
        d["dur"] = float(d["dur"])
        d["dl"] = int(d["dl"])
        d["title"] = html.unescape(d["title"])
        if dmin <= d["dur"] <= dmax:
            res.append(d)
    res.sort(key=lambda x: -x["dl"])
    return res


def main():
    with open(os.path.join(ROOT, "Tools", "sounds.json")) as f:
        spec = json.load(f)
    credits = {}
    if os.path.exists(CREDITS):
        with open(CREDITS) as f:
            credits = json.load(f)
    for name, entry in spec.items():
        if name.startswith("_") or (ONLY and name not in ONLY):
            continue
        query, dmin, dmax, count = entry
        picked = []
        for lic_name, lic_filter in LICENSES:
            for r in search(query, lic_filter, dmin, dmax):
                if len(picked) >= count:
                    break
                r["license"] = lic_name
                picked.append(r)
            if len(picked) >= count:
                break
        if not picked:
            print(f"[FAIL] {name}: no results for '{query}'")
            continue
        for i, r in enumerate(picked):
            fn = f"{name}.mp3" if count == 1 else f"{name}_{i + 1}.mp3"
            path = os.path.join(OUT, fn)
            os.makedirs(os.path.dirname(path), exist_ok=True)
            hq = r["mp3"].replace("-lq.mp3", "-hq.mp3")
            with open(path, "wb") as f:
                f.write(get(hq))
            credits[fn] = {"title": r["title"], "author": r["user"], "license": r["license"],
                           "url": f"https://freesound.org/people/{r['user']}/sounds/{r['id']}/",
                           "duration": r["dur"]}
            print(f"[ok] {fn} <- '{r['title']}' by {r['user']} ({r['license']}, {r['dur']:.1f}s)")
        time.sleep(1.0)
    with open(CREDITS, "w") as f:
        json.dump(credits, f, indent=1, sort_keys=True)


main()
