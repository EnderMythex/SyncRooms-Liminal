"""Search ambientCG and Poly Haven catalogs. Usage: python search_assets.py <source> <query...>
source: acg | ph-tex | ph-model | ph-hdri
"""
import json
import sys
import urllib.parse
import urllib.request

UA = {"User-Agent": "SyncRooms-Liminal-AssetFetcher/1.0"}


def get_json(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60) as r:
        return json.load(r)


def search_acg(q):
    url = "https://ambientcg.com/api/v2/full_json?" + urllib.parse.urlencode(
        {"type": "Material", "q": q, "limit": 25, "sort": "Popular", "include": "tagData"})
    for a in get_json(url)["foundAssets"]:
        print(a["assetId"], "|", a.get("displayName", ""), "|", ",".join(a.get("tags", [])[:8]))


_PH_CACHE = {}


def search_ph(kind, q):
    if kind not in _PH_CACHE:
        _PH_CACHE[kind] = get_json(f"https://api.polyhaven.com/assets?t={kind}")
    words = q.lower().split()
    for k, v in _PH_CACHE[kind].items():
        hay = " ".join([k, v.get("name", ""), " ".join(v.get("tags", [])), " ".join(v.get("categories", []))]).lower()
        if all(w in hay for w in words):
            print(k, "|", v.get("name"), "|", ",".join(v.get("categories", [])[:5]))


if __name__ == "__main__":
    src, q = sys.argv[1], " ".join(sys.argv[2:])
    if src == "acg":
        search_acg(q)
    else:
        search_ph({"ph-tex": "textures", "ph-model": "models", "ph-hdri": "hdris"}[src], q)
