"""Generate CREDITS.md from the asset manifests.  python make_credits.py"""
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
T = os.path.join(ROOT, "Tools")

lines = ["# Credits", "",
         "SyncRooms : Liminal uses the following third-party assets. Thank you to their authors.", "",
         "Level concepts are inspired by the [Backrooms Wiki](https://backrooms-wiki.wikidot.com/) "
         "(Level 4 \"Abandoned Office\", Level 5 \"Terror Hotel\", Level 37 \"Sublimity\", Level 94 \"Motion\"), "
         "licensed CC BY-SA 3.0.", ""]

lines += ["## Textures (CC0)", "", "| Material | Source | Asset |", "|---|---|---|"]
for name, spec in json.load(open(os.path.join(T, "materials.json"))).items():
    if name.startswith("_"):
        continue
    if spec["src"] == "acg":
        lines.append(f"| {name} | ambientCG | [{spec['id']}](https://ambientcg.com/view?id={spec['id']}) |")
    else:
        lines.append(f"| {name} | Poly Haven | [{spec['id']}](https://polyhaven.com/a/{spec['id']}) |")
lines += ["", "Generated textures (L5_Damask, L5_HotelCarpet, Almond Water label) are procedural, built on top of the CC0 scans above.", ""]

lines += ["## 3D models (CC0, Poly Haven)", ""]
for group, items in json.load(open(os.path.join(T, "models.json"))).items():
    if group.startswith("_"):
        continue
    links = ", ".join(f"[{k}](https://polyhaven.com/a/{k})" for k in items)
    lines.append(f"- **{group}**: {links}")
lines += ["", "All other models (office, hotel, town, pool modules, items) were modelled procedurally in Blender (`Tools/blender/`).", ""]

lines += ["## Sounds (Freesound)", "", "| File | Title | Author | License |", "|---|---|---|---|"]
credits = json.load(open(os.path.join(T, "sound_credits.json")))
for fn, c in sorted(credits.items()):
    lic = "CC0" if c["license"] == "CC0" else "[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)"
    lines.append(f"| {fn} | [{c['title']}]({c['url']}) | {c['author']} | {lic} |")
lines += ["", "## Fonts", "",
          "- IBM Plex Mono — IBM, SIL Open Font License 1.1",
          "- Special Elite — Astigmatic, Apache License 2.0", ""]
with open(os.path.join(ROOT, "CREDITS.md"), "w", encoding="utf-8") as f:
    f.write("\n".join(lines))
print("CREDITS.md written:", len(lines), "lines")
