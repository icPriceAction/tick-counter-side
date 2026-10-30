# icPA - Tick Counter Side for NinjaTrader 8

[![NinjaTrader 8](https://img.shields.io/badge/Platform-NinjaTrader%208-black.svg)](https://ninjatrader.com)
[![License: Personal Use](https://img.shields.io/badge/License-Personal%20Use-6E6E6E.svg)](#license)
[![YouTube Guide](https://img.shields.io/badge/YouTube-Watch%20Walkthrough-red.svg)](https://youtube.com/@icPriceAction)
[![Website](https://img.shields.io/badge/Website-icpriceaction.com-111213.svg)](https://icpriceaction.com)

A specialized Indicator engineered for Price Action day trading. It shows the live tick count of the forming bar right beside it, riding on an optional horizontal line at the last traded price, so you never have to look away from price to the corner of the chart.

---

## 📺 Video Walkthrough & Setup

Watch the complete breakdown of parameters, price action context, and chart examples on YouTube:

[![Watch the Video](assets/preview.png)](https://youtube.com/@icPriceAction)

> **[▶ Click here to watch the full YouTube walkthrough](https://youtube.com/@icPriceAction)**

---

## ⚡ Key Features

- **Counter Beside the Bar:** Displays ticks remaining (count down), ticks elapsed, or percent complete next to the current bar, moving with the last traded price.
- **Infinite Price Line:** Optional horizontal line at the last price, with configurable width, dash style and color.
- **Flexible Placement:** Shift the counter left/right in bars and up/down in pixels, with an optional "Tick count" / "Ticks remaining" prefix label.
- **PATS & Context Aligned:** Designed for Price Action principles (Mack / Thomas Wade) and disciplined futures execution.
- **Zero Chart Lag:** Native C# execution optimized to prevent UI freezes on high-frequency 2000-tick ES/MES charts.
- **Clean Visuals:** Complementary default palette that preserves candlestick clarity without chart clutter.
- **Fully Customizable:** Configure colors, thresholds, offsets, and alerts via the native NT8 settings panel.

---

## ⚠️ Prerequisites & Data Requirements

- **Bar type:** Tick-based charts only: **Tick**, or **Heiken Ashi**, **Price on Volume** or **Volumetric** built on Tick. On other bar types the indicator shows NinjaTrader's standard "tick-based bars only" message and prints one warning to the Output window.
- **Calculate mode:** **On Each Tick** (the default). If set to On Bar Close, the counter will not update live and a one-time warning is printed to the Output window.
- **Tick Replay:** Not required.

---

## 📥 Installation Guide

1. Head to the **[Releases](https://github.com/icPriceAction/tick-counter-side/releases)** tab of this repository.
2. Download the latest release `.zip` file (`icPriceAction_icPATickCounterSide_v1.3.0.zip`).
   - *Note: Do NOT extract or unzip the file.*
3. Open **NinjaTrader 8**.
4. In the Control Center, navigate to:
   `Tools` ➔ `Import` ➔ `NinjaScript Add-On...`
5. Select the downloaded `.zip` file and click **Open**.
6. A success dialog will appear confirming the tool has been imported.
7. Open any chart, press `Ctrl + I`, find **icPA - Tick Counter Side** in the **icPA** folder, and add it.

---

## ⚙️ Parameters & Configuration

| Parameter | Group | Type | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| Count Down | 1. General | Boolean | True | Show ticks remaining in the bar instead of ticks elapsed. |
| Show Percent | 1. General | Boolean | False | Show the bar's progress as a percentage instead of a tick count. |
| Show Prefix Label | 1. General | Boolean | False | Show the "Tick count" / "Ticks remaining" label before the number. |
| Show Price Line | 2. Visuals | Boolean | True | Show an infinite horizontal line at the last traded price. |
| Line Width | 2. Visuals | Integer (1–20) | 2 | Thickness in pixels of the price line. |
| Line Style | 2. Visuals | Dash style | Solid | Dash style of the price line. |
| Line Color | 2. Visuals | Color | Black | Color of the price line. |
| Font | 2. Visuals | Font | Arial, 16, Bold | Font family, size and weight of the counter text. |
| Text Color | 2. Visuals | Color | Black | Color of the counter text. |
| Bars Right Offset | 3. Offsets & Thresholds | Integer (0–30) | 1 | Horizontal spacing in bars to the right of the current bar. |
| Vertical Pixel Offset | 3. Offsets & Thresholds | Integer (-100–100) | -16 | Pixel offset of the text relative to the price line (negative values shift it upward). |

---

## 📝 Changelog

- **v1.3.0** (2026-10-30) - Initial public release. Cleaned and optimized by icPriceAction.
  - Removed the ATR(1) bracket size label and its parameter.
  - Moved to the icPA folder and renamed the class to `icPATickCounterSide`.
  - Bar-type check cached once; price line and text redrawn only when they change.
  - One-time Output window warnings for non-tick charts and On Bar Close mode.
  - Added attribution to NinjaTrader's TickCounter and clarified license scope.

---

## 🐛 Bug Reports & Feature Requests

Found an issue or have an enhancement idea?
Please submit an issue on the [Issues tab](https://github.com/icPriceAction/tick-counter-side/issues).

---

## License

This indicator is based on the **TickCounter** indicator included with NinjaTrader 8. Portions are Copyright © NinjaTrader, LLC and remain subject to NinjaTrader's own license terms. Modifications are Copyright © 2026 Istvan Cebrian / icPriceAction.

Free for personal, non-commercial trading use on NinjaTrader 8. Commercial resale or distribution is prohibited, and modified versions may not be redistributed publicly without written consent from icPriceAction. See [LICENSE](LICENSE).

---

## ⚖️ Disclaimer

Futures trading involves substantial risk of loss and is not suitable for every investor. All indicators, drawing tools, strategies, and documentation provided by **icPriceAction** are for educational and analytical purposes only. Nothing herein constitutes financial or investment advice. Automated strategies can lose money and may behave differently in live markets than in simulation or backtests.

NinjaTrader® is a registered trademark of NinjaTrader, LLC. icPriceAction is an independent developer with no official affiliation to NinjaTrader, LLC.