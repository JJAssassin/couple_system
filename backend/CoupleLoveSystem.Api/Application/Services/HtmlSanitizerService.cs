using Ganss.Xss;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// 富文本入库前净化。基于 Ganss.Xss（HtmlSanitizer）做白名单净化：
/// 仅放行安全排版标签/属性，默认拒绝一切；外链强制 rel=noopener noreferrer 防 reverse-tabnabbing。
/// 取代 DiaryService 原有的正则兜底。
/// </summary>
public sealed class HtmlSanitizerService
{
    private static readonly HtmlSanitizer _sanitizer = new();

    static HtmlSanitizerService()
    {
        _sanitizer.AllowedTags.Clear();
        foreach (var t in new[] { "p", "br", "b", "i", "u", "em", "strong", "ul", "ol", "li", "blockquote", "code", "pre", "h1", "h2", "h3", "span", "a", "img" })
            _sanitizer.AllowedTags.Add(t);

        _sanitizer.AllowedAttributes.Clear();
        // 全局属性白名单（本版本 AllowedAttributes 为 List<string>，按标签分组需走 AllowedAttributesOnElements）
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("rel");
        _sanitizer.AllowedAttributes.Add("src");
        _sanitizer.AllowedAttributes.Add("alt");

        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("http");
        // data: 协议在 PostProcessDom 中按标签收紧：仅 img 的 base64 内嵌图可用，
        // a 的 href 与非 img 的 src 上的 data: 全部剔除，杜绝 data:text/html 钓鱼/XSS（P2-8）
        _sanitizer.AllowedSchemes.Add("data");

        _sanitizer.PostProcessDom += (_, e) =>
        {
            // a 标签：强制 rel 防 reverse-tabnabbing；剔除 data: 钓鱼/XSS 链接
            foreach (var a in e.Document.GetElementsByTagName("a"))
            {
                a.SetAttribute("rel", "noopener noreferrer");
                var href = a.GetAttribute("href");
                if (href != null && href.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    a.RemoveAttribute("href");
            }
            // 其它可承载 src 的标签（iframe/embed/object/source/video/audio 等）：剔除 data: src，防脚本执行
            foreach (var tag in new[] { "iframe", "embed", "object", "source", "video", "audio", "script", "link", "image" })
            {
                foreach (var el in e.Document.GetElementsByTagName(tag))
                {
                    var src = el.GetAttribute("src");
                    if (src != null && src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        el.RemoveAttribute("src");
                }
            }
            // img 上的 data: 仅允许位图（png/jpeg/gif/webp），SVG 可携带脚本，剔除以防 XSS
            foreach (var img in e.Document.GetElementsByTagName("img"))
            {
                var src = img.GetAttribute("src");
                if (src != null && src.StartsWith("data:image/svg", StringComparison.OrdinalIgnoreCase))
                    img.RemoveAttribute("src");
            }
        };
    }

    public string Sanitize(string? raw) => raw is null ? string.Empty : _sanitizer.Sanitize(raw);
}
