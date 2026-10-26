from __future__ import annotations

import html
import re
from pathlib import Path

import markdown

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs" / "世界演化系统复刻设计文档.md"
OUTPUT = ROOT / "docs" / "世界演化系统复刻设计文档.html"

text = SOURCE.read_text(encoding="utf-8")
version_match = re.search(r"^> 版本：(v[\d.]+)", text, re.M)
if version_match is None:
    raise ValueError("Document version is missing")
version = html.escape(version_match.group(1))

md = markdown.Markdown(
    extensions=[
        "tables",
        "fenced_code",
        "toc",
        "sane_lists",
        "attr_list",
    ],
    extension_configs={
        "toc": {
            "toc_depth": "2-2",
            "permalink": False,
        }
    },
)
body = md.convert(text)
toc = md.toc

# The HTML hero already carries the document title; remove the Markdown H1.
body = re.sub(r"<h1[^>]*>.*?</h1>", "", body, count=1, flags=re.S)

# Add a copy button to code blocks.
body = body.replace(
    "<pre><code",
    '<div class="code-wrap"><button class="copy-btn" type="button">复制</button><pre><code',
)
body = body.replace("</code></pre>", "</code></pre></div>")

# Turn fenced mermaid blocks into renderable containers.
def _mermaid(match: re.Match[str]) -> str:
    code = html.unescape(match.group(1))
    return f'<div class="mermaid">{code}</div>'

body = re.sub(
    r'<div class="code-wrap"><button class="copy-btn" type="button">复制</button>'
    r'<pre><code class="language-mermaid">(.*?)</code></pre></div>',
    _mermaid,
    body,
    flags=re.S,
)

page = f"""<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>世界演化系统复刻设计文档</title>
<style>
:root {{
  --bg: #f7f5f0;
  --paper: #fffdf8;
  --ink: #202a33;
  --muted: #64717c;
  --line: #e4ded2;
  --accent: #0f6b64;
  --accent-soft: #e5f1ee;
  --warm: #a45a2a;
  --code: #f0ede6;
  --shadow: 0 18px 50px rgba(45, 55, 60, .08);
}}
* {{ box-sizing: border-box; }}
html {{ scroll-behavior: smooth; }}
body {{
  margin: 0;
  background:
    radial-gradient(circle at 15% 0%, rgba(15,107,100,.06), transparent 28rem),
    radial-gradient(circle at 90% 15%, rgba(164,90,42,.05), transparent 26rem),
    var(--bg);
  color: var(--ink);
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "PingFang SC",
    "Hiragino Sans GB", "Microsoft YaHei", sans-serif;
  line-height: 1.78;
  font-size: 15.5px;
}}
a {{ color: var(--accent); text-decoration: none; border-bottom: 1px solid rgba(15,107,100,.22); }}
a:hover {{ border-bottom-color: var(--accent); }}
.layout {{ display: grid; grid-template-columns: 264px minmax(0, 1fr); max-width: 1380px; margin: 0 auto; }}
aside {{
  position: sticky; top: 0; align-self: start; height: 100vh; overflow-y: auto;
  padding: 32px 22px 40px; border-right: 1px solid var(--line);
  background: rgba(255,253,248,.56); backdrop-filter: blur(8px);
}}
.brand {{ font-size: 12px; letter-spacing: .18em; color: var(--warm); font-weight: 700; }}
.aside-title {{ font-size: 20px; line-height: 1.35; margin: 10px 0 22px; font-weight: 750; }}
.toc {{ font-size: 13px; }}
.toc ul {{ list-style: none; margin: 0; padding: 0; }}
.toc li {{ margin: 2px 0; }}
.toc a {{ display: block; color: var(--muted); padding: 5px 10px; border-left: 2px solid transparent; border-bottom: 0; border-radius: 0 6px 6px 0; }}
.toc a:hover {{ color: var(--ink); background: rgba(15,107,100,.05); }}
.toc a.active {{ color: var(--accent); background: var(--accent-soft); border-left-color: var(--accent); font-weight: 650; }}
.aside-note {{ margin-top: 26px; padding-top: 18px; border-top: 1px solid var(--line); color: var(--muted); font-size: 12px; }}
main {{ min-width: 0; padding: 0 48px 80px; }}
.hero {{ padding: 64px 0 30px; border-bottom: 1px solid var(--line); margin-bottom: 34px; }}
.kicker {{ color: var(--accent); font-weight: 750; letter-spacing: .14em; font-size: 12px; }}
h1 {{ font-family: "Songti SC", "STSong", "SimSun", serif; font-size: clamp(32px, 4vw, 48px); line-height: 1.2; margin: 12px 0 16px; letter-spacing: -.02em; }}
.subtitle {{ color: var(--muted); max-width: 720px; font-size: 16px; }}
.hero-meta {{ display: flex; flex-wrap: wrap; gap: 8px; margin-top: 22px; }}
.pill {{ font-size: 12px; padding: 4px 10px; border: 1px solid var(--line); border-radius: 999px; background: var(--paper); color: var(--muted); }}
.content {{ max-width: 900px; }}
h2 {{ font-size: 27px; line-height: 1.35; margin: 2.4em 0 .8em; padding-top: .2em; border-top: 1px solid var(--line); letter-spacing: -.01em; }}
h2:first-of-type {{ border-top: 0; }}
h3 {{ font-size: 19px; margin: 1.8em 0 .6em; color: #24343d; }}
h4 {{ font-size: 16px; margin: 1.4em 0 .4em; }}
p {{ margin: .75em 0; }}
strong {{ color: #18272d; font-weight: 700; }}
blockquote {{ margin: 1.3em 0; padding: 14px 20px; background: var(--accent-soft); border-left: 3px solid var(--accent); border-radius: 0 8px 8px 0; color: #24504d; }}
blockquote p {{ margin: .25em 0; }}
ul, ol {{ padding-left: 1.7em; }}
li {{ margin: .32em 0; }}
hr {{ border: 0; border-top: 1px solid var(--line); margin: 2.6em 0; }}
table {{ width: 100%; border-collapse: collapse; margin: 1.2em 0 1.6em; font-size: 13.5px; background: var(--paper); box-shadow: 0 0 0 1px var(--line); border-radius: 8px; overflow: hidden; display: block; overflow-x: auto; }}
thead {{ background: #f0ece3; }}
th, td {{ padding: 10px 12px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }}
th {{ font-weight: 700; white-space: nowrap; }}
tr:last-child td {{ border-bottom: 0; }}
tbody tr:nth-child(even) {{ background: rgba(240,236,227,.35); }}
.code-wrap {{ position: relative; margin: 1.1em 0 1.4em; }}
pre {{ background: #20282c; color: #e9eee9; padding: 18px 18px; border-radius: 10px; overflow-x: auto; font-size: 12.5px; line-height: 1.6; box-shadow: var(--shadow); }}
pre code {{ font-family: "SFMono-Regular", Consolas, "Liberation Mono", monospace; background: transparent; padding: 0; color: inherit; }}
code {{ font-family: "SFMono-Regular", Consolas, "Liberation Mono", monospace; background: var(--code); padding: .12em .34em; border-radius: 4px; font-size: .9em; }}
.copy-btn {{ position: absolute; top: 8px; right: 8px; z-index: 2; border: 1px solid rgba(255,255,255,.16); background: rgba(255,255,255,.08); color: #dce7e2; border-radius: 5px; font-size: 11px; padding: 3px 8px; cursor: pointer; opacity: .65; }}
.copy-btn:hover {{ opacity: 1; background: rgba(255,255,255,.15); }}
.mermaid {{ background: var(--paper); border: 1px solid var(--line); border-radius: 10px; padding: 14px; text-align: center; overflow-x: auto; }}
.mermaid svg {{ max-width: 100%; height: auto; }}
.footer {{ margin-top: 54px; padding-top: 20px; border-top: 1px solid var(--line); color: var(--muted); font-size: 12px; }}
@media (max-width: 980px) {{
  .layout {{ display: block; }}
  aside {{ position: static; height: auto; border-right: 0; border-bottom: 1px solid var(--line); padding: 20px 22px; }}
  .aside-title {{ margin-bottom: 10px; }}
  .toc ul {{ columns: 2; column-gap: 14px; }}
  .aside-note {{ display: none; }}
  main {{ padding: 0 22px 60px; }}
  .hero {{ padding-top: 40px; }}
}}
@media (max-width: 560px) {{
  body {{ font-size: 15px; }}
  .toc ul {{ columns: 1; }}
  main {{ padding-left: 16px; padding-right: 16px; }}
  h2 {{ font-size: 23px; }}
  table {{ font-size: 12.5px; }}
}}
@media print {{
  aside, .copy-btn {{ display: none; }}
  body {{ background: white; font-size: 10.5pt; }}
  .layout {{ display: block; }}
  main {{ padding: 0; }}
  .content {{ max-width: none; }}
  h2 {{ break-after: avoid; }}
  pre, table, blockquote {{ break-inside: avoid; }}
}}
</style>
</head>
<body>
<div class="layout">
  <aside>
    <div class="brand">SYSTEM DESIGN · 复刻规格</div>
    <div class="aside-title">世界演化系统<br>复刻设计文档</div>
    <nav class="toc">{toc}</nav>
    <div class="aside-note">基于公开资料与原创工程推断编写。机制以当前版本为准，数值不照搬原作。</div>
  </aside>
  <main>
    <header class="hero">
      <div class="kicker">NPC 社会模拟 / 世界事件 / 工程落地</div>
      <h1>世界演化系统复刻设计文档</h1>
      <p class="subtitle">参照《鬼谷八荒》《太吾绘卷》的世界进程与 NPC 演化机制，给出一套可实施、可测试、可扩展的系统架构。</p>
      <div class="hero-meta">
        <span class="pill">版本 {version}</span>
        <span class="pill">2026-10-24</span>
        <span class="pill">行为级复刻</span>
        <span class="pill">C# / Unity / SQLite</span>
        <span class="pill">含 MVP 与验收标准</span>
      </div>
    </header>
    <article class="content">{body}</article>
    <footer class="footer">本文为原创工程规格；不包含原作代码、美术、文本或数值表。商业使用前请做法律审查。</footer>
  </main>
</div>
<script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
<script>
document.querySelectorAll('a[href^="http"]').forEach(a => {{
  a.target = '_blank';
  a.rel = 'noopener noreferrer';
}});
document.querySelectorAll('.copy-btn').forEach(btn => {{
  btn.addEventListener('click', async () => {{
    const code = btn.parentElement.querySelector('code');
    if (!code) return;
    try {{
      await navigator.clipboard.writeText(code.innerText);
      btn.textContent = '已复制';
      setTimeout(() => btn.textContent = '复制', 1200);
    }} catch (e) {{}}
  }});
}});
const links = [...document.querySelectorAll('.toc a')];
const targets = links.map(a => document.getElementById(a.getAttribute('href').slice(1))).filter(Boolean);
const io = new IntersectionObserver(entries => {{
  entries.forEach(entry => {{
    if (!entry.isIntersecting) return;
    links.forEach(a => a.classList.toggle('active', a.getAttribute('href') === '#' + entry.target.id));
  }});
}}, {{ rootMargin: '0px 0px -70% 0px', threshold: 0 }});
targets.forEach(t => io.observe(t));
if (window.mermaid) {{
  mermaid.initialize({{ startOnLoad: true, theme: 'neutral', securityLevel: 'loose' }});
}}
</script>
</body>
</html>
"""

OUTPUT.write_text(page, encoding="utf-8")
print(f"wrote {OUTPUT} ({OUTPUT.stat().st_size} bytes)")
