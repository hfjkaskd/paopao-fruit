from pathlib import Path
import sys
import json
import hashlib

sys.path.insert(0, 'C:/Users/pc/AppData/Local/Temp/orchard-font-diagnostics-libs')
import UnityPy
from UnityPy.classes import PPtr

project = Path(__file__).resolve().parents[2]
folder = Path(__file__).resolve().parent
base = project / 'Library/com.unity.addressables/aa/Android/Android'
env = UnityPy.load(*[str(base / x) for x in (folder/'bundles.txt').read_text(encoding='utf-8-sig').splitlines() if x.strip()])

def deref(reader, value):
    return PPtr(**value, assetsfile=reader.assets_file).deref()

def identity(reader):
    return (reader.assets_file.name, reader.path_id)

def texture_info(reader):
    obj = reader.read()
    return dict(file=reader.assets_file.name, id=reader.path_id, name=obj.m_Name,
                width=obj.m_Width, height=obj.m_Height, format=obj.m_TextureFormat,
                sha256=hashlib.sha256(obj.image_data).hexdigest())

report = dict(fonts=[], materials=[], labels=[])
for o in env.objects:
    if o.type.name == 'MonoBehaviour':
        d=o.read_typetree()
        if d.get('m_Name')=='Baloo2 ExtraBold SDF':
            atlas=deref(o,d['m_AtlasTextures'][0])
            material=deref(o,d['material'])
            report['fonts'].append(dict(file=o.assets_file.name,id=o.path_id,atlas=texture_info(atlas),
                material=identity(material),glyphs=len(d['m_GlyphTable']),characters=len(d['m_CharacterTable']),
                entries=[x for x in d['m_CharacterTable'] if x['m_Unicode'] in map(ord,'Withdraw')]))
        if d.get('m_text') in ['Retirar','Cash withdrawal','Saque em dinheiro']:
            font=deref(o,d['m_fontAsset'])
            material=deref(o,d['m_sharedMaterial'])
            report['labels'].append(dict(id=o.path_id,text=d['m_text'],font=identity(font),fontName=font.read_typetree().get('m_Name'),material=identity(material),materialName=material.read_typetree().get('m_Name')))
    elif o.type.name=='Material':
        d=o.read_typetree()
        if d.get('m_Name') in ['CashDisabledTitle','ReferenceTitleWood','ReferenceTitleGreen']:
            t=dict(d['m_SavedProperties']['m_TexEnvs'])['_MainTex']['m_Texture']
            report['materials'].append(dict(name=d['m_Name'],file=o.assets_file.name,id=o.path_id,
                atlas=texture_info(deref(o,t))))

(folder/'bundle-atlas-report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
