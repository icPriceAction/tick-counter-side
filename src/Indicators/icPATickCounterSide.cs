// =========================================================================================
// Tool Name:    icPA - Tick Counter Side
// Class:        icPATickCounterSide
// Category:     Indicator
// Version:      1.4.0
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
// License:      Free for personal, non-commercial trading use on NinjaTrader 8.
//               Commercial resale or distribution is prohibited. Modified versions may
//               not be redistributed publicly without written consent from icPriceAction.
//               Portions derived from NinjaTrader's TickCounter remain the property of
//               NinjaTrader, LLC and are subject to NinjaTrader's own license terms.
//               No affiliation with or endorsement by NinjaTrader, LLC is implied.
//               See LICENSE in official repository.
//
// Description:  Shows the live tick count (or ticks remaining / percent complete) as text
//               beside the forming bar, anchored to an optional horizontal line at the last
//               price, with an optional user-defined bracket size label.
//
// Changelog:
//   v1.4.0 (2026-11-03) - Added a configurable bracket size label: source (True Range / High-Low /
//                         Body), forming or last closed bar, ticks or points, 5 slots, overflow
//                         label and label format. Off by default; no ATR indicator needed.
//   v1.4.0 (2026-11-03) - Default Vertical Pixel Offset changed from -16 to +16 (counter above the
//                         price line); corrected the setting's description of the offset direction.
//   v1.4.0 (2026-11-03) - Updated the indicator description with support and prop firm links.
//   v1.3.0 (2026-10-30) - Initial public release. Cleaned and optimized by icPriceAction.
//   v1.3.0 (2026-10-30) - Removed the hard-coded ATR(1) bracket size label ([B1]/[B2]/[B3]/[NB]) and its parameter.
//   v1.3.0 (2026-10-30) - Moved to the icPA folder/namespace and renamed class to icPATickCounterSide.
//   v1.3.0 (2026-10-30) - Bar-type check cached once; price line and text redrawn only when they change.
//   v1.3.0 (2026-10-30) - One-time Output window warnings for non-tick charts and On Bar Close mode.
//   v1.3.0 (2026-10-30) - Added attribution to NinjaTrader's TickCounter and clarified license scope.
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
	public enum icPABracketSource
	{
		TrueRange,
		HighLow,
		Body
	}

	public enum icPABracketBar
	{
		FormingBar,
		LastClosedBar
	}

	public enum icPABracketUnits
	{
		Ticks,
		Points
	}

	public class icPATickCounterSide : Indicator
	{
		private const string	LineTag			= "TickCounterPriceLine";
		private const string	TextTag			= "TickCounterSideTag";
		private const string	DefaultFormat	= "[{0}]";
		private const int		SlotCount		= 5;

		// Cached once per load (bar type cannot change without a reload)
		private bool		isTickBased;
		private double		periodValue;

		// Bracket settings resolved once per load (no per-tick parsing or formatting)
		private int[]		slotMaxTicks;
		private string[]	slotText;
		private string		overflowText;
		private bool		slotsOutOfOrder;
		private bool		formatInvalid;

		// Redraw-on-change tracking
		private double		lastLinePrice;
		private double		lastTextPrice;
		private string		lastText;
		private bool		priceLineRemoved;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= "Displays a live tick counter beside the current bar, anchored to an optional horizontal line at the last traded price, "
											+ "with an optional bracket size label based on user-defined size slots. Requires a tick-based chart (Tick, or Heiken Ashi / Price on Volume / Volumetric built on Tick) and Calculate = On Each Tick.\r\n"
											+ "Based on NinjaTrader's TickCounter indicator. Developed by Istvan Cebrian | https://icpriceaction.com\n"
											+ "Support this free tool: https://buymeacoffee.com/icpriceaction | Prop Firm Discount Code: ICPAT (https://icpriceaction.com/prop-firms.html)";
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
				VerticalPixelOffset	= 16;

				// 4. Bracket Size (off by default; slot values tuned for ES in ticks)
				ShowBracketSize		= false;
				BracketSource		= icPABracketSource.TrueRange;
				BracketBar			= icPABracketBar.FormingBar;
				BracketUnits		= icPABracketUnits.Ticks;
				Slot1MaxSize		= 8;
				Slot1Label			= "B1";
				Slot2MaxSize		= 12;
				Slot2Label			= "B2";
				Slot3MaxSize		= 16;
				Slot3Label			= "B3";
				Slot4MaxSize		= 0;
				Slot4Label			= string.Empty;
				Slot5MaxSize		= 0;
				Slot5Label			= string.Empty;
				OverflowLabel		= "NB";
				LabelFormat			= DefaultFormat;
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

				ResolveBracketSlots();

				lastLinePrice		= double.NaN;
				lastTextPrice		= double.NaN;
				lastText			= null;
				priceLineRemoved	= false;
			}
			else if (State == State.Historical)
			{
				// One-time prerequisite and setting warnings (this state is entered once per load)
				if (!isTickBased)
					Print(Name + ": This indicator needs a tick-based chart (Tick, or Heiken Ashi / Price on Volume / Volumetric built on Tick). The counter will show an error message on this chart.");

				if (Calculate == Calculate.OnBarClose)
					Print(Name + ": Calculate is set to On Bar Close. Set it to On Each Tick so the counter updates live.");

				if (ShowBracketSize && slotsOutOfOrder)
					Print(Name + ": Bracket slot Max Size values are not in increasing order. Slots are checked from 1 to 5, so a slot whose Max Size is not larger than an earlier slot can never be used.");

				if (ShowBracketSize && formatInvalid)
					Print(Name + ": Bracket Label Format is invalid (use {0} for the label, for example [{0}]). The default format " + DefaultFormat + " is being used.");
			}
		}

		// Converts slot sizes to whole ticks and pre-formats every label once, so OnBarUpdate does no parsing or formatting.
		private void ResolveBracketSlots()
		{
			double[] maxSizes	= { Slot1MaxSize, Slot2MaxSize, Slot3MaxSize, Slot4MaxSize, Slot5MaxSize };
			string[] labels		= { Slot1Label, Slot2Label, Slot3Label, Slot4Label, Slot5Label };

			slotMaxTicks	= new int[SlotCount];
			slotText		= new string[SlotCount];
			slotsOutOfOrder	= false;
			formatInvalid	= false;

			string format = string.IsNullOrEmpty(LabelFormat) ? DefaultFormat : LabelFormat;
			try
			{
				string.Format(CultureInfo.InvariantCulture, format, "B1");
			}
			catch (FormatException)
			{
				format			= DefaultFormat;
				formatInvalid	= true;
			}

			int previousMax = 0;
			for (int i = 0; i < SlotCount; i++)
			{
				double size		= maxSizes[i];
				int ticks		= size <= 0 ? 0
								: BracketUnits == icPABracketUnits.Ticks
									? (int)Math.Round(size, MidpointRounding.AwayFromZero)
									: (int)Math.Round(size / TickSize, MidpointRounding.AwayFromZero);

				slotMaxTicks[i]	= ticks;
				slotText[i]		= FormatLabel(format, labels[i]);

				if (ticks > 0)
				{
					if (ticks <= previousMax)
						slotsOutOfOrder = true;
					else
						previousMax = ticks;
				}
			}

			overflowText = FormatLabel(format, OverflowLabel);
		}

		private static string FormatLabel(string format, string label)
		{
			// Empty label = no text appended; a single space separates it from the counter
			return string.IsNullOrEmpty(label) ? string.Empty : " " + string.Format(CultureInfo.InvariantCulture, format, label);
		}

		// Returns the pre-formatted bracket text for the selected bar, or an empty string.
		private string GetBracketText()
		{
			int barsAgo = BracketBar == icPABracketBar.FormingBar ? 0 : 1;
			if (CurrentBar < barsAgo)
				return string.Empty;

			double high	= High[barsAgo];
			double low	= Low[barsAgo];
			double size;

			switch (BracketSource)
			{
				case icPABracketSource.HighLow:
					size = high - low;
					break;
				case icPABracketSource.Body:
					size = Math.Abs(Open[barsAgo] - Close[barsAgo]);
					break;
				default:
					// True Range (identical to ATR(1)); first bar on the chart falls back to High - Low
					size = high - low;
					if (CurrentBar > barsAgo)
					{
						double prevClose = Close[barsAgo + 1];
						size = Math.Max(size, Math.Max(Math.Abs(high - prevClose), Math.Abs(low - prevClose)));
					}
					break;
			}

			// Round to whole ticks so floating-point noise cannot push a bar into the wrong bracket
			int sizeTicks = (int)Math.Round(size / TickSize, MidpointRounding.AwayFromZero);

			for (int i = 0; i < SlotCount; i++)
			{
				int max = slotMaxTicks[i];
				if (max > 0 && sizeTicks < max)
					return slotText[i];
			}

			return overflowText;
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

			// 2. Optional bracket size label
			string bracketStr = ShowBracketSize ? GetBracketText() : string.Empty;

			// 3. Build counter text
			string textToDisplay;

			if (!isTickBased)
			{
				textToDisplay = NinjaTrader.Custom.Resource.TickCounterBarError + bracketStr;
			}
			else
			{
				double tickCount	= ShowPercent
										? (CountDown ? 1 - Bars.PercentComplete : Bars.PercentComplete)
										: (CountDown ? periodValue - Bars.TickCount : Bars.TickCount);
				string tickMsg		= ShowPercent ? tickCount.ToString("P0") : tickCount.ToString(CultureInfo.InvariantCulture);

				textToDisplay = (ShowPrefix
					? (CountDown ? NinjaTrader.Custom.Resource.TickCounterTicksRemaining : NinjaTrader.Custom.Resource.TickCounterTickCount) + tickMsg
					: tickMsg) + bracketStr;
			}

			// 4. Draw text beside the bar (only when text or price changed)
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
		[Display(Name = "Vertical Pixel Offset", Description = "Pixel offset of the text relative to the price line (positive values shift it upward, negative values downward)", Order = 2, GroupName = "3. Offsets & Thresholds")]
		public int VerticalPixelOffset { get; set; }

		// --- 4. Bracket Size ---
		[Display(Name = "Show Bracket Size", Description = "Append a bracket label (e.g. [B1]) to the counter based on the measured bar size and the slots below", Order = 1, GroupName = "4. Bracket Size")]
		public bool ShowBracketSize { get; set; }

		[Display(Name = "Bracket Source", Description = "How the bar is measured: True Range (includes gaps from the previous close, same as ATR(1)), High - Low, or Body (Open to Close)", Order = 2, GroupName = "4. Bracket Size")]
		public icPABracketSource BracketSource { get; set; }

		[Display(Name = "Measure Bar", Description = "Measure the forming bar (updates live) or the last closed bar (fixed while the current bar forms)", Order = 3, GroupName = "4. Bracket Size")]
		public icPABracketBar BracketBar { get; set; }

		[Display(Name = "Units", Description = "Units for the slot Max Size values: Ticks or Points. Values are not converted when you switch units", Order = 4, GroupName = "4. Bracket Size")]
		public icPABracketUnits BracketUnits { get; set; }

		[Range(0, 100000)]
		[Display(Name = "Slot 1 Max Size", Description = "Bars smaller than this size get the Slot 1 label. 0 = slot unused", Order = 5, GroupName = "4. Bracket Size")]
		public double Slot1MaxSize { get; set; }

		[Display(Name = "Slot 1 Label", Description = "Label shown for Slot 1", Order = 6, GroupName = "4. Bracket Size")]
		public string Slot1Label { get; set; }

		[Range(0, 100000)]
		[Display(Name = "Slot 2 Max Size", Description = "Bars smaller than this size (and not in an earlier slot) get the Slot 2 label. 0 = slot unused", Order = 7, GroupName = "4. Bracket Size")]
		public double Slot2MaxSize { get; set; }

		[Display(Name = "Slot 2 Label", Description = "Label shown for Slot 2", Order = 8, GroupName = "4. Bracket Size")]
		public string Slot2Label { get; set; }

		[Range(0, 100000)]
		[Display(Name = "Slot 3 Max Size", Description = "Bars smaller than this size (and not in an earlier slot) get the Slot 3 label. 0 = slot unused", Order = 9, GroupName = "4. Bracket Size")]
		public double Slot3MaxSize { get; set; }

		[Display(Name = "Slot 3 Label", Description = "Label shown for Slot 3", Order = 10, GroupName = "4. Bracket Size")]
		public string Slot3Label { get; set; }

		[Range(0, 100000)]
		[Display(Name = "Slot 4 Max Size", Description = "Bars smaller than this size (and not in an earlier slot) get the Slot 4 label. 0 = slot unused", Order = 11, GroupName = "4. Bracket Size")]
		public double Slot4MaxSize { get; set; }

		[Display(Name = "Slot 4 Label", Description = "Label shown for Slot 4", Order = 12, GroupName = "4. Bracket Size")]
		public string Slot4Label { get; set; }

		[Range(0, 100000)]
		[Display(Name = "Slot 5 Max Size", Description = "Bars smaller than this size (and not in an earlier slot) get the Slot 5 label. 0 = slot unused", Order = 13, GroupName = "4. Bracket Size")]
		public double Slot5MaxSize { get; set; }

		[Display(Name = "Slot 5 Label", Description = "Label shown for Slot 5", Order = 14, GroupName = "4. Bracket Size")]
		public string Slot5Label { get; set; }

		[Display(Name = "Overflow Label", Description = "Label shown when the bar is at least as large as every used slot. Leave empty to show nothing", Order = 15, GroupName = "4. Bracket Size")]
		public string OverflowLabel { get; set; }

		[Display(Name = "Label Format", Description = "How the label is wrapped; {0} is replaced by the label (e.g. [{0}] shows [B1]). A space is always added before it", Order = 16, GroupName = "4. Bracket Size")]
		public string LabelFormat { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private icPA.icPATickCounterSide[] cacheicPATickCounterSide;
		public icPA.icPATickCounterSide icPATickCounterSide(bool countDown, bool showPercent)
		{
			return icPATickCounterSide(Input, countDown, showPercent);
		}

		public icPA.icPATickCounterSide icPATickCounterSide(ISeries<double> input, bool countDown, bool showPercent)
		{
			if (cacheicPATickCounterSide != null)
				for (int idx = 0; idx < cacheicPATickCounterSide.Length; idx++)
					if (cacheicPATickCounterSide[idx] != null && cacheicPATickCounterSide[idx].CountDown == countDown && cacheicPATickCounterSide[idx].ShowPercent == showPercent && cacheicPATickCounterSide[idx].EqualsInput(input))
						return cacheicPATickCounterSide[idx];
			return CacheIndicator<icPA.icPATickCounterSide>(new icPA.icPATickCounterSide(){ CountDown = countDown, ShowPercent = showPercent }, input, ref cacheicPATickCounterSide);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.icPA.icPATickCounterSide icPATickCounterSide(bool countDown, bool showPercent)
		{
			return indicator.icPATickCounterSide(Input, countDown, showPercent);
		}

		public Indicators.icPA.icPATickCounterSide icPATickCounterSide(ISeries<double> input , bool countDown, bool showPercent)
		{
			return indicator.icPATickCounterSide(input, countDown, showPercent);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.icPA.icPATickCounterSide icPATickCounterSide(bool countDown, bool showPercent)
		{
			return indicator.icPATickCounterSide(Input, countDown, showPercent);
		}

		public Indicators.icPA.icPATickCounterSide icPATickCounterSide(ISeries<double> input , bool countDown, bool showPercent)
		{
			return indicator.icPATickCounterSide(input, countDown, showPercent);
		}
	}
}

#endregion