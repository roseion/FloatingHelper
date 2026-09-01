# verify-homepage.ps1
# 浮动助手主页 index.html 静态验证脚本
# 用途：在交付前校验主页的硬性需求与单文件自包含约束。
# 运行方式：powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\verify-homepage.ps1
$ErrorActionPreference = "Stop"

$htmlPath = Join-Path $PSScriptRoot "..\index.html"
$failCount = 0

function Assert($condition, $name, $detail = "") {
    if ($condition) {
        Write-Output ("[PASS] " + $name)
    } else {
        $msg = "[FAIL] " + $name
        if ($detail) { $msg += " :: " + $detail }
        Write-Output $msg
        $script:failCount++
    }
}

# ---------- 1. 文件存在且非空 ----------
if (-not (Test-Path $htmlPath)) {
    Write-Output ("[FAIL] index.html 不存在: " + $htmlPath)
    exit 1
}
$html = [IO.File]::ReadAllText($htmlPath, [Text.Encoding]::UTF8)
Assert ($html.Length -gt 10000) "index.html 非空且内容充分" ("length=" + $html.Length)

# ---------- 2. 头部：返回主页链接，新窗口打开 ----------
$headerBlock = [regex]::Match($html, '<header[^>]*class="site-header"[\s\S]*?</header>').Value
Assert ($headerBlock.Length -gt 0) "可提取到页眉元素"
$homeLinkInHeader = [regex]::IsMatch($headerBlock, '<a class="home-link"[^>]*href="https://www\.oldgao\.com"[^>]*target="_blank"[^>]*rel="noopener noreferrer"')
Assert $homeLinkInHeader "头部页眉区域包含返回主页链接 (www.oldgao.com, target=_blank)"

# ---------- 3. 下载地址为 GitHub ----------
Assert ([regex]::IsMatch($html, 'href="https://github\.com/roseion/FloatingHelper/releases"')) "下载链接指向 GitHub Releases"
Assert ([regex]::IsMatch($html, 'href="https://github\.com/roseion/FloatingHelper"')) "源码/仓库链接指向 GitHub 仓库"
$dlCount = ([regex]::Matches($html, 'github\.com/roseion/FloatingHelper')).Count
Assert ($dlCount -ge 4) "GitHub 链接出现多次（下载/仓库/文档）" ("count=" + $dlCount)

# ---------- 3.5 插件仓库链接 ----------
Assert ([regex]::IsMatch($html, 'href="https://github\.com/roseion/FloatingHelper\.Plugins\.Translate"')) "包含翻译插件仓库链接"
$pluginRepoCount = ([regex]::Matches($html, 'github\.com/roseion/FloatingHelper\.Plugins\.Translate')).Count
Assert ($pluginRepoCount -ge 2) "插件仓库链接出现在插件区块与页脚" ("count=" + $pluginRepoCount)

# ---------- 3.6 GitHub blob 链接分支名（仓库默认分支为 master） ----------
$badBranch = [regex]::Matches($html, 'github\.com/roseion/[^"#?]*/blob/main/')
Assert ($badBranch.Count -eq 0) "无指向 main 分支的 GitHub 链接（仓库默认分支是 master）" ("found=" + $badBranch.Count)
$goodBranch = [regex]::Matches($html, 'github\.com/roseion/FloatingHelper/blob/master/')
Assert ($goodBranch.Count -ge 2) "文档链接指向 master 分支" ("count=" + $goodBranch.Count)

# ---------- 4. 核心内容区块 ----------
foreach ($id in @("features", "smartopen", "plugin", "download")) {
    Assert ([regex]::IsMatch($html, 'id="' + $id + '"')) ("包含内容区块 #" + $id)
}
Assert ([regex]::IsMatch($html, '浮动助手')) "包含产品名称"
Assert ([regex]::IsMatch($html, 'FloatingHelper')) "包含英文名称"

# ---------- 5. 无 emoji（BMP 之外的星象平面字符即视为 emoji） ----------
$emojiMatches = [regex]::Matches($html, '[\uD800-\uDBFF]')
Assert ($emojiMatches.Count -eq 0) "HTML 中无 emoji" ("found=" + $emojiMatches.Count)

# ---------- 6. 无本地路径资源引用 ----------
Assert (-not [regex]::IsMatch($html, 'src="[A-Za-z]:[\\/]')) "无 Windows 本地绝对路径引用 (src)"
Assert (-not [regex]::IsMatch($html, 'href="[A-Za-z]:[\\/]')) "无 Windows 本地绝对路径引用 (href)"
Assert (-not [regex]::IsMatch($html, 'src="\.\.?/')) "无相对路径引用 (src)"
Assert (-not [regex]::IsMatch($html, 'href="\.\.?/')) "无相对路径引用 (href)"
Assert (-not [regex]::IsMatch($html, 'file://')) "无 file:// 协议引用"

# ---------- 7. 单文件自包含 ----------
Assert (-not [regex]::IsMatch($html, '<link[^>]+rel="stylesheet"[^>]+href="[^"]+\.css"')) "无外部本地 CSS 文件引用"
Assert (-not [regex]::IsMatch($html, '<script[^>]+src="[^"]+\.js"')) "无外部本地 JS 文件引用"
$imgTags = [regex]::Matches($html, '<img[^>]+src="([^"]+)"')
foreach ($m in $imgTags) {
    $src = $m.Groups[1].Value
    $ok = $src.StartsWith("data:") -or $src.StartsWith("https://") -or $src.StartsWith("http://")
    Assert $ok "图片资源为 data URI 或远程 URL" $src
}
# data URI 解码后魔数与声明的 MIME 一致
$dataUriMatches = [regex]::Matches($html, 'data:([a-z]+/[a-z0-9.+-]+);base64,([A-Za-z0-9+/=]+)')
foreach ($m in $dataUriMatches) {
    $mime = $m.Groups[1].Value
    $b64 = $m.Groups[2].Value
    try { $raw = [Convert]::FromBase64String($b64) } catch { Assert $false "data URI base64 可解码" $mime; continue }
    $magic = if ($raw.Length -ge 3) { '{0:X2}{1:X2}{2:X2}' -f $raw[0], $raw[1], $raw[2] } else { "" }
    $mimeOk = ($mime -eq "image/jpeg" -and $magic -eq "FFD8FF") -or ($mime -eq "image/png" -and $magic -eq "89504E")
    Assert $mimeOk ("data URI MIME 与内容一致: " + $mime) $magic
}

# ---------- 8. 字体镜像 ----------
Assert ([regex]::IsMatch($html, 'miaoda\.feishu\.cn/fonts/css2')) "字体使用自托管镜像"
Assert (-not [regex]::IsMatch($html, 'fonts\.googleapis\.com')) "未直连 Google Fonts"

# ---------- 9. 基础 HTML 结构 ----------
Assert ([regex]::IsMatch($html, '<!DOCTYPE html>')) "包含 DOCTYPE"
Assert ([regex]::IsMatch($html, '<html lang="zh-CN">')) "html 声明中文语言"
Assert ([regex]::IsMatch($html, '<meta charset="UTF-8">')) "包含 UTF-8 编码声明"
Assert ([regex]::IsMatch($html, '<meta name="viewport"')) "包含 viewport"
Assert ([regex]::IsMatch($html, '<title>[^<]+</title>')) "包含 title"
Assert ([regex]::IsMatch($html, 'rel="icon"')) "包含 favicon"
# 标签配平（粗粒度）
foreach ($tag in @("div", "section", "header", "footer", "main", "nav", "article", "a", "span", "svg", "ul", "li", "p", "h1", "h2", "h3")) {
    $open = ([regex]::Matches($html, "<" + $tag + "[\s>]")).Count
    $close = ([regex]::Matches($html, "</" + $tag + ">")).Count
    Assert ($open -eq $close) ("标签配平 <" + $tag + ">") ("open=" + $open + ", close=" + $close)
}

# ---------- 10. 关键行为项 ----------
Assert ([regex]::IsMatch($html, 'target="_blank"')) "存在新窗口打开链接"
Assert ([regex]::IsMatch($html, 'rel="noopener noreferrer"')) "外链包含 rel=noopener noreferrer"
Assert (-not [regex]::IsMatch($html, 'scrollIntoView')) "未使用 scrollIntoView"

Write-Output ""
if ($failCount -eq 0) {
    Write-Output ("验证通过：全部检查项 PASS（失败项 0）")
    exit 0
} else {
    Write-Output ("验证失败：共 " + $failCount + " 项未通过")
    exit 1
}
