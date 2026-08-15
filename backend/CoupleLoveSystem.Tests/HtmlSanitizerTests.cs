using CoupleLoveSystem.Application.Services;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 验证富文本净化：拦截 XSS payload，保留合法排版标签。
/// </summary>
public class HtmlSanitizerTests
{
    private readonly HtmlSanitizerService _s = new();

    [Fact]
    public void Script_Tag_Removed()
        => Assert.DoesNotContain("<script", _s.Sanitize("<p>hi</p><script>alert(1)</script>"), System.StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void OnError_Attr_Removed()
        => Assert.DoesNotContain("onerror", _s.Sanitize("<img src=x onerror=alert(1)>"), System.StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Javascript_Protocol_Removed()
        => Assert.DoesNotContain("javascript:", _s.Sanitize("<a href=\"javascript:alert(1)\">x</a>"), System.StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Safe_Formatting_Kept()
        => Assert.Contains("<b>bold</b>", _s.Sanitize("<b>bold</b>"));

    [Fact]
    public void Img_Src_Removed_For_Dangerous_Scheme()
        // 非 http/https/data 的图片地址应被剥离 src
        => Assert.DoesNotContain("src=\"vbscript", _s.Sanitize("<img src=\"vbscript:msgbox(1)\">"), System.StringComparison.OrdinalIgnoreCase);
}
