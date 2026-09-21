from pathlib import Path
from html.parser import HTMLParser
import json,re
R=Path(r'C:\Projects\paopao\APIReplaceReports\bubblosaicpmge-20260916')
class Check(HTMLParser):
    def __init__(self):super().__init__();self.ids=[];self.links=[];self.tables=0;self.rows=0;self.lang=None
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if 'id' in a:self.ids.append(a['id'])
        if tag=='a':self.links.append(a.get('href',''))
        if tag=='table':self.tables+=1
        if tag=='tr':self.rows+=1
        if tag=='html':self.lang=a.get('lang')
p=R/'API替换对比报告.html';text=p.read_text(encoding='utf-8');c=Check();c.feed(text);c.close()
assert len(c.ids)==len(set(c.ids))
assert c.lang=='zh-CN'
assert all(x[1:] in c.ids for x in c.links if x.startswith('#'))
assert all((R/x).is_file() for x in c.links if x and not x.startswith(('#','https:','http:')))
assert 'MergePB@91Saic' not in text
assert text.count('id="attribution"')==1
assert len(re.findall(r'FtMaGp → FtMaGp',text))>=1
out={'status':'Passed','utf8':True,'language':c.lang,'tables':c.tables,'rows':c.rows,'uniqueIds':True,'allInternalLinksResolve':True,'allEvidenceFilesExist':True,'secretRedaction':True,'review':'HTML structure/data checks; no browser screenshot validation'}
(R/'report-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(out))
