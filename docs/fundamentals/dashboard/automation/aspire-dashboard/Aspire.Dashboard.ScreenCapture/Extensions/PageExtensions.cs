﻿global using InlineStyle = (string PropertyName, string Style);

namespace Aspire.Dashboard.ScreenCapture.Extensions;

internal static class PageExtensions
{
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web);
    private static readonly string s_relativeMediaPath = "../../../../../../media";
    
    public static async Task SaveExploreScreenshotAsync(
        this IPage page, string fileName, Clip? clip = null)
    {
        await page.ScreenshotAsync(new()
        {
            Clip = clip,
            Path = $"{s_relativeMediaPath}/explore/{fileName}"
        });
    }

    public static async Task<string> GetResourceEndpointAsync(this IPage page, int row = 4)
    {
        var endpoint = FluentDataGridSelector.Grid.Body.Row(row).Cell(5).Descendant("> div > div:nth-child(1) > a");
        var url = await page.Locator(endpoint).TextContentAsync();

        Assert.NotNull(url);

        return url;
    }

    public static async Task RedactElementTextAsync(this IPage page, string selector)
    {
        await page.WaitForSelectorAsync(selector);

        await page.EvaluateAsync($$"""
            const el = document.querySelector('{{selector}}');
            if (el) {
              const text = el.textContent;
              el.textContent = text.replace(/(^\S+)|(\S+$)/g, '█');
            } else {
              console.error('Element not found: {{selector}}');
            }
            """);
    }

    public static async Task BlurElementTextAsync(this IPage page, string selector)
    {
        await page.WaitForSelectorAsync(selector);

        await page.EvaluateAsync($$"""
            const el = document.querySelector('{{selector}}');
            if (el) {
              el.style.textDecoration = 'line-through';
              el.style.filter = 'blur(.2rem)';
            } else {
              console.error('Element not found: {{selector}}');
            }
            """);
    }

    public static async Task AdjustSplitPanelsGridTemplateAsync(
        this IPage page, string selector = "split-panels", string gridTemplateRows = "0.25fr 5px 0.75fr")
    {
        await page.EvaluateAsync($$"""
            const el = document.querySelector('{{selector}}');
            if (el) {
              el.style.gridTemplateRows="{{gridTemplateRows}}";
            } else {
              console.error('Element not found: {{selector}}');
            }
            """);
    }

    public static async Task ClickAndDragShadowRootElementAsync(
        this IPage page, string hostSelector, string shadowSelector, MouseMovement mouseMovement)
    {
        var shadowHost = page.Locator(hostSelector);

        var source = await shadowHost.EvaluateHandleAsync(
            $"el => el.shadowRoot.querySelector('{shadowSelector}')");

        var element = source.AsElement();
        if (element is null)
        {
            return;
        }

        // Hover, click, and drag
        await element.HoverAsync();
        await page.Mouse.DownAsync();

        var (x, y) = mouseMovement;

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.UpAsync();
    }

    public static async Task ApplyInlineStyleAsync(this IPage page, string selector, params IEnumerable<InlineStyle> styles)
    {
        await page.WaitForSelectorAsync(selector);

        await page.EvaluateAsync($$"""
            const el = document.querySelector('{{selector}}');
            if (el) {
              {{string.Join("", styles.Select(s => $"el.style.{s.PropertyName} = '{s.Style}';"))}}
            } else {
              console.error('Element not found: {{selector}}');
            }
            """);
    }

    public static async Task HighlightElementAsync(this IPage page, string selector)
    {
        await page.WaitForSelectorAsync(selector);

        await page.EvaluateAsync($$"""
            const el = document.querySelector('{{selector}}');
            if (el) {
              el.style.borderRadius = 0;
              el.style.border = '3px solid red';
            } else {
              console.error('Element not found: {{selector}}');
            }
            """);
    }

    public static async Task HighlightElementsAsync(this IPage page, params string[] selectors)
    {
        var array = JsonSerializer.Serialize(selectors, s_options);

        foreach (var selector in selectors)
        {
            await page.WaitForSelectorAsync(selector);
        }

        await page.EvaluateAsync($$"""
            for (const selector of {{array}}) {
              const el = document.querySelector(selector);
              if (el) {
                el.style.borderRadius = 0;
                el.style.border = '3px solid red';
              } else {
                console.error('Element not found: ' + selector);
              }
            }
            """);
    }

    public static async Task LoginAsync(this IPage page, string token)
    {
        var response = await page.GotoAsync(
          $"/login?t={token}", new() { WaitUntil = WaitUntilState.NetworkIdle });

        Assert.NotNull(response);
        Assert.True(response.Ok, $"Failed to navigate to login page: {response.Status}");

        await Assertions.Expect(page).ToHaveURLAsync("/");
    }

    public static async Task LoginAndWaitForRunningResourcesAsync(this IPage page, string token, bool waitForRunning = true)
    {
        await page.LoginAsync(token);

        if (waitForRunning)
        {
            var cacheStateSpan = FluentDataGridSelector.Grid.Body.Row(2).Cell(2).Descendant("> div > span");
            var apiStateSpan = FluentDataGridSelector.Grid.Body.Row(3).Cell(2).Descendant("> div > span");
            var webStateSpan = FluentDataGridSelector.Grid.Body.Row(4).Cell(2).Descendant("> div > span");

            var cache = page.Locator(cacheStateSpan);
            var api = page.Locator(apiStateSpan);
            var web = page.Locator(webStateSpan);

            var options = new LocatorAssertionsToHaveTextOptions
            {
                Timeout = 17_500
            };

            await Assertions.Expect(api).ToHaveTextAsync("Running", options);
            await Assertions.Expect(web).ToHaveTextAsync("Running", options);
            await Assertions.Expect(cache).ToHaveTextAsync("Running", options);
        }
    }

    public static async Task WaitForSettingsFlyoutAsync(this IPage page, bool trimVersionToStable = true)
    {
        await page.WaitForSelectorAsync(DashboardSelectors.SettingsDialog.SettingsDialogHeading);

        if (trimVersionToStable)
        {
            await page.EvaluateAsync("""
                const el = document.querySelector('#SettingsDialog .version');
                if (el) {
                    el.innerText = el.innerText.substring(0, el.innerText.indexOf('-'));
                } else {
                    console.error('Element not found: #SettingsDialog .version');
                }
                """);
        }
    }
}

internal readonly record struct MouseMovement(int X, int Y)
{
    public static implicit operator MouseMovement((int x, int y) tuple) => new(tuple.x, tuple.y);
}
