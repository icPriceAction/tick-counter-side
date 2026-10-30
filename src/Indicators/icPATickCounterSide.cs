// =========================================================================================
// Tool Name:    icPA - Tick Counter Side
// Class:        icPATickCounterSide
// Category:     Indicator
// Version:      1.3.0
// Platform:     NinjaTrader 8.0+
// Author:       Istvan Cebrian
// Brand:        icPriceAction
// Website:      https://icpriceaction.com
// Repository:   https://github.com/icPriceAction/tick-counter-side
// YouTube:      https://youtube.com/@icPriceAction
//
// Credits:      Based on the TickCounter indicator included with NinjaTrader 8.
//               Portions Copyright (C) NinjaTrader, LLC <www.ninjatrader.com>.
//               Modifications Copyright (C) 2026 Istvan Cebrian / icPriceAction.
//
// License:      icPriceAction's modifications are free for personal trading use on
//               NinjaTrader 8. Commercial resale of this indicator is prohibited.
//               Portions derived from NinjaTrader's TickCounter remain the property of
//               NinjaTrader, LLC and are subject to NinjaTrader's own license terms.
//               No affiliation with or endorsement by NinjaTrader, LLC is implied.
//               See LICENSE in official repository.
//
// Description:  Shows the live tick count (or ticks remaining / percent complete) as text
//               beside the forming bar, anchored to an optional horizontal line at the last price.
//
// Changelog:
//   v1.3.0 (2026-10-30) - Initial public release. Cleaned and optimized by icPriceAction.
//   v1.3.0 (2026-10-30) - Moved to the icPA folder/namespace and renamed class to icPATickCounterSide.
//   v1.3.0 (2026-10-30) - Bar-type check cached once; price line and text redrawn only when they change.
//   v1.3.0 (2026-10-30) - One-time Output window warnings for non-tick charts and On Bar Close mode.
// =========================================================================================

#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.icPA
{
	public class icPATickCounterSide : Indicator
	{
		private const string LineTag = "TickCounterPriceLine";
		private const string TextTag = "TickCounterSideTag";

		// Cached once per load (bar type cannot change without a reload)
		private bool	isTickBased;
		private double	periodValue;

		// Redraw-on-change tracking
		private double	lastLinePrice;
		private double	lastTextPrice;
		private string	lastText;
		private bool	priceLineRemoved;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "Displays a live tick counter beside the current bar, anchored to an optional horizontal line at the last traded price. "
											+ "Requires a tick-based chart (Tick, or Heiken Ashi / Price on Volume / Volumetric built on Tick) and Calculate = On Each Tick. Tick Replay is not required. "
											+ "Based on NinjaTrader's TickCounter indicator. "
											+ "Developed by Istvan Cebrian | icPriceAction.com";
				Name						= "icPA - Tick Counter Side";
				Calculate					= Calculate.OnEachTick;
				BarsRequiredToPlot			= 0;
				MaximumBarsLookBack			= MaximumBarsLookBack.TwoHundredFiftySix;
				DisplayInDataBox			= false;
				DrawOnPricePanel			= true;
				IsChartOnly					= true;
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;

				// 1. General
				CountDown			= true;
				ShowPercent			= false;
				ShowPrefix			= false;

				// 2. Visuals
				ShowPriceLine		= true;
				LineWidth			= 2;
				LineStyle			= DashStyleHelper.Solid;
				LineColor			= Brushes.Black;
				CounterFont			= new SimpleFont("Arial", 16) { Bold = true };
				TextColor			= Brushes.Black;

				// 3. Offsets & Thresholds
				BarsOffset			= 1;
				VerticalPixelOffset	= -16;
			}
			else if (State == State.DataLoaded)
			{
				BarsPeriodType type = BarsPeriod.BarsPeriodType;

				isTickBased = type == BarsPeriodType.Tick
					|| ((type == BarsPeriodType.HeikenAshi
						|| type == BarsPeriodType.PriceOnVolume
						|| type == BarsPeriodType.Volumetric)
						&& BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Tick);

				periodValue = type == BarsPeriodType.Tick ? BarsPeriod.Value : BarsPeriod.BaseBarsPeriodValue;

				lastLinePrice		= double.NaN;
				lastTextPrice		= double.NaN;
				lastText			= null;
				priceLineRemoved	= false;
			}
			else if (State == State.Historical)
			{
				// One-time prerequisite warnings (this state is entered once per load)
				if (!isTickBased)
					Print(Name + ": This indicator needs a tick-based chart (Tick, or Heiken Ashi / Price on Volume / Volumetric built on Tick). The counter will show an error message on this chart.");

				if (Calculate == Calculate.OnBarClose)
					Print(Name + ": Calculate is set to On Bar Close. Set it to On Each Tick so the counter updates live.");
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0 || CurrentBar < BarsRequiredToPlot)
				return;

			// Only the last bar matters; skip older historical bars during load
			if (State == State.Historical && CurrentBar < Count - 1)
				return;

			double currentPrice = Close[0];

			// 1. Infinite horizontal price line (redrawn only when price changes)
			if (ShowPriceLine)
			{
				if (currentPrice != lastLinePrice)
				{
					Draw.HorizontalLine(this, LineTag, currentPrice, LineColor, LineStyle, LineWidth);
					lastLinePrice		= currentPrice;
					priceLineRemoved	= false;
				}
			}
			else if (!priceLineRemoved)
			{
				RemoveDrawObject(LineTag);
				priceLineRemoved = true;
			}

			// 2. Build counter text
			string textToDisplay;

			if (!isTickBased)
			{
				textToDisplay = NinjaTrader.Custom.Resource.TickCounterBarError;
			}
			else
			{
				double tickCount	= ShowPercent
										? (CountDown ? 1 - Bars.PercentComplete : Bars.PercentComplete)
										: (CountDown ? periodValue - Bars.TickCount : Bars.TickCount);
				string tickMsg		= ShowPercent ? tickCount.ToString("P0") : tickCount.ToString(CultureInfo.InvariantCulture);

				textToDisplay = ShowPrefix
					? (CountDown ? NinjaTrader.Custom.Resource.TickCounterTicksRemaining : NinjaTrader.Custom.Resource.TickCounterTickCount) + tickMsg
					: tickMsg;
			}

			// 3. Draw text beside the bar (only when text or price changed)
			if (currentPrice != lastTextPrice || !string.Equals(textToDisplay, lastText, StringComparison.Ordinal))
			{
				Draw.Text(this, TextTag, false, textToDisplay, -BarsOffset, currentPrice, VerticalPixelOffset, TextColor, CounterFont,
					TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 0);
				lastText		= textToDisplay;
				lastTextPrice	= currentPrice;
			}
		}

		#region Properties
		// --- 1. General ---
		[NinjaScriptProperty]
		[Display(Name = "Count Down", Description = "Show ticks remaining in the bar instead of ticks elapsed", Order = 1, GroupName = "1. General")]
		public bool CountDown { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Percent", Description = "Show the bar's progress as a percentage instead of a tick count", Order = 2, GroupName = "1. General")]
		public bool ShowPercent { get; set; }

		[Display(Name = "Show Prefix Label", Description = "Show the 'Tick count' / 'Ticks remaining' label before the number", Order = 3, GroupName = "1. General")]
		public bool ShowPrefix { get; set; }

		// --- 2. Visuals ---
		[Display(Name = "Show Price Line", Description = "Show an infinite horizontal line at the last traded price", Order = 1, GroupName = "2. Visuals")]
		public bool ShowPriceLine { get; set; }

		[Range(1, 20)]
		[Display(Name = "Line Width", Description = "Thickness in pixels of the price line", Order = 2, GroupName = "2. Visuals")]
		public int LineWidth { get; set; }

		[Display(Name = "Line Style", Description = "Dash style of the price line", Order = 3, GroupName = "2. Visuals")]
		public DashStyleHelper LineStyle { get; set; }

		[XmlIgnore]
		[Display(Name = "Line Color", Description = "Color of the price line", Order = 4, GroupName = "2. Visuals")]
		public Brush LineColor { get; set; }

		[Browsable(false)]
		public string LineColorSerializable
		{
			get { return Serialize.BrushToString(LineColor); }
			set { LineColor = Serialize.StringToBrush(value); }
		}

		[Display(Name = "Font", Description = "Font family, size and weight of the counter text", Order = 5, GroupName = "2. Visuals")]
		public SimpleFont CounterFont { get; set; }

		[XmlIgnore]
		[Display(Name = "Text Color", Description = "Color of the counter text", Order = 6, GroupName = "2. Visuals")]
		public Brush TextColor { get; set; }

		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}

		// --- 3. Offsets & Thresholds ---
		[Range(0, 30)]
		[Display(Name = "Bars Right Offset", Description = "Horizontal spacing in bars to the right of the current bar", Order = 1, GroupName = "3. Offsets & Thresholds")]
		public int BarsOffset { get; set; }

		[Range(-100, 100)]
		[Display(Name = "Vertical Pixel Offset", Description = "Pixel offset of the text relative to the price line (negative values shift it upward)", Order = 2, GroupName = "3. Offsets & Thresholds")]
		public int VerticalPixelOffset { get; set; }
		#endregion
	}
}