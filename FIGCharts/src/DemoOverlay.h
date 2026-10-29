#pragma once

// Temporary screen-only overlay demonstration. Adjust these values and rebuild.
namespace demoOverlay {
inline constexpr bool enabled = true; // Set false to remove the demonstration.
inline constexpr int left = 0;        // Pixels from the chart image's left edge.
inline constexpr int bottom = 0;      // Pixels up from the chart image's bottom edge.
inline constexpr int height = 9;
inline constexpr COLORREF background_dark = RGB(24, 32, 43);
inline constexpr COLORREF background_light = RGB(255, 255, 255);

void paint(HDC dc, int chartX, int chartY, int chartWidth, int chartHeight, HFONT font, bool dark) {
    if (!enabled || chartWidth <= 0 || chartHeight <= 0) return;
    int saved = SaveDC(dc);
    IntersectClipRect(dc, chartX, chartY, chartX + chartWidth, chartY + chartHeight);
    RECT r{chartX + left, chartY + chartHeight - bottom - height,
           chartX + chartWidth, chartY + chartHeight - bottom};
    COLORREF background = dark ? background_dark : background_light;
    auto fill = CreateSolidBrush(background); FillRect(dc, &r, fill); DeleteObject(fill);
    auto edge = CreateSolidBrush(background); FrameRect(dc, &r, edge); DeleteObject(edge);
    RestoreDC(dc, saved);
}
}
