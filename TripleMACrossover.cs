#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class TripleMACrossover : Indicator
    {
        private EMA emaFast;
        private EMA emaMedium;
        private EMA emaSlow;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "EMA Fast Period", Order = 1, GroupName = "Parameters")]
        public int EMAFastPeriod { get; set; } = 3;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "EMA Medium Period", Order = 2, GroupName = "Parameters")]
        public int EMAMediumPeriod { get; set; } = 8;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "EMA Slow Period", Order = 3, GroupName = "Parameters")]
        public int EMASlowPeriod { get; set; } = 14;

        [NinjaScriptProperty]
        [Display(Name = "Show Signals", Order = 4, GroupName = "Parameters")]
        public bool ShowSignals { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "Paint Candle Colors", Order = 5, GroupName = "Parameters")]
        public bool PaintCandleColors { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "Paint Background", Order = 6, GroupName = "Parameters")]
        public bool PaintBackground { get; set; } = false;

        // --- Bar Color Configuration ---
        [XmlIgnore]
        [NinjaScriptProperty]
        [Display(Name = "Bar Green Color", Order = 7, GroupName = "Bar Colors")]
        public Brush BarGreenColor { get; set; } = Brushes.DarkGreen;
        [Browsable(false)]
        public string BarGreenColorSerialize
        {
            get { return Serialize.BrushToString(BarGreenColor); }
            set { BarGreenColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [NinjaScriptProperty]
        [Display(Name = "Bar Red Color", Order = 8, GroupName = "Bar Colors")]
        public Brush BarRedColor { get; set; } = Brushes.Maroon;
        [Browsable(false)]
        public string BarRedColorSerialize
        {
            get { return Serialize.BrushToString(BarRedColor); }
            set { BarRedColor = Serialize.StringToBrush(value); }
        }

        // --- Candle Neutral Color (unchanged) ---
        [XmlIgnore]
        [NinjaScriptProperty]
        [Display(Name = "White Color", Order = 9, GroupName = "Parameters")]
        public Brush WhiteColor { get; set; } = Brushes.White;
        [Browsable(false)]
        public string WhiteColorSerialize
        {
            get { return Serialize.BrushToString(WhiteColor); }
            set { WhiteColor = Serialize.StringToBrush(value); }
        }

        // --- Long Signal Configuration ---
        [XmlIgnore]
        [NinjaScriptProperty]
        [Display(Name = "Long Signal Color", Order = 10, GroupName = "Long Signal")]
        public Brush LongSignalColor { get; set; } = Brushes.LimeGreen;
        [Browsable(false)]
        public string LongSignalColorSerialize
        {
            get { return Serialize.BrushToString(LongSignalColor); }
            set { LongSignalColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Long Signal Offset (ticks)", Order = 11, GroupName = "Long Signal")]
        public double LongSignalOffset { get; set; } = 20;

        // --- Short Signal Configuration ---
        [XmlIgnore]
        [NinjaScriptProperty]
        [Display(Name = "Short Signal Color", Order = 12, GroupName = "Short Signal")]
        public Brush ShortSignalColor { get; set; } = Brushes.LimeGreen;
        [Browsable(false)]
        public string ShortSignalColorSerialize
        {
            get { return Serialize.BrushToString(ShortSignalColor); }
            set { ShortSignalColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Short Signal Offset (ticks)", Order = 13, GroupName = "Short Signal")]
        public double ShortSignalOffset { get; set; } = 20;

        // --- Background Color Configuration ---
        [XmlIgnore]
        [NinjaScriptProperty]
        [Display(Name = "Background Green Color", Order = 14, GroupName = "Background Colors")]
        public Brush BackgroundGreenColor { get; set; } = Brushes.DarkGreen;
        [Browsable(false)]
        public string BackgroundGreenColorSerialize
        {
            get { return Serialize.BrushToString(BackgroundGreenColor); }
            set { BackgroundGreenColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [NinjaScriptProperty]
        [Display(Name = "Background Red Color", Order = 15, GroupName = "Background Colors")]
        public Brush BackgroundRedColor { get; set; } = Brushes.Maroon;
        [Browsable(false)]
        public string BackgroundRedColorSerialize
        {
            get { return Serialize.BrushToString(BackgroundRedColor); }
            set { BackgroundRedColor = Serialize.StringToBrush(value); }
        }

        /// <summary>
        /// Exposes the indicator's numeric signal (+1 for long, -1 for short, 0 otherwise)
        /// as a Series<double>. Your strategies can access it via myIndicator.SignalValue[0].
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> SignalValue
        {
            get { return Values[0]; }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description              = "Triple EMA Crossover with diamond signals and optional candle/background painting.";
                Name                     = "TripleMACrossover";
                Calculate                = Calculate.OnBarClose;
                IsOverlay                = true;
                DisplayInDataBox         = true;
                DrawOnPricePanel         = true;
                DrawHorizontalGridLines  = true;
                DrawVerticalGridLines    = true;
                PaintPriceMarkers        = true;
                ScaleJustification       = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;

                // Add a hidden plot (Series<double>) for strategy access
                AddPlot(Brushes.Transparent, "SignalValue");
            }
            else if (State == State.DataLoaded)
            {
                // Initialize EMAs
                emaFast   = EMA(EMAFastPeriod);
                emaMedium = EMA(EMAMediumPeriod);
                emaSlow   = EMA(EMASlowPeriod);
            }
        }

        protected override void OnBarUpdate()
        {
            // Ensure enough bars exist
            if (CurrentBar < Math.Max(Math.Max(EMAFastPeriod, EMAMediumPeriod), EMASlowPeriod))
                return;

            // Reset the indicator value each bar (it can be overridden by any of the cross conditions)
            double indicatorValue = 0;

            // ----------------------------------------------
            // 1) Fast crossing above Medium (and Medium > Slow)
            // ----------------------------------------------
            if (CrossAbove(emaFast, emaMedium, 1) && emaMedium[0] > emaSlow[0])
            {
                indicatorValue = 1;
                if (ShowSignals)
                {
                    double offset = TickSize * LongSignalOffset;
                    Draw.Diamond(
                        this,
                        "LongSignalFastMed" + CurrentBar,
                        true,
                        0,
                        Low[0] - offset,
                        LongSignalColor
                    );
                }
            }
            // ----------------------------------------------
            // 2) Fast crossing below Medium (and Medium < Slow)
            // ----------------------------------------------
            if (CrossBelow(emaFast, emaMedium, 1) && emaMedium[0] < emaSlow[0])
            {
                indicatorValue = -1;
                if (ShowSignals)
                {
                    double offset = TickSize * ShortSignalOffset;
                    Draw.Diamond(
                        this,
                        "ShortSignalFastMed" + CurrentBar,
                        true,
                        0,
                        High[0] + offset,
                        ShortSignalColor
                    );
                }
            }

            // ----------------------------------------------
            // 3) Medium crossing above Slow (only if Fast > Medium)
            // ----------------------------------------------
            if (CrossAbove(emaMedium, emaSlow, 1) && emaFast[0] > emaMedium[0])
            {
                indicatorValue = 1;
                if (ShowSignals)
                {
                    double offset = TickSize * LongSignalOffset;
                    Draw.Diamond(
                        this,
                        "LongSignalMedSlow" + CurrentBar,
                        true,
                        0,
                        Low[0] - offset,
                        LongSignalColor
                    );
                }
            }
            // ----------------------------------------------
            // 4) Medium crossing below Slow (only if Fast < Medium)
            // ----------------------------------------------
            if (CrossBelow(emaMedium, emaSlow, 1) && emaFast[0] < emaMedium[0])
            {
                indicatorValue = -1;
                if (ShowSignals)
                {
                    double offset = TickSize * ShortSignalOffset;
                    Draw.Diamond(
                        this,
                        "ShortSignalMedSlow" + CurrentBar,
                        true,
                        0,
                        High[0] + offset,
                        ShortSignalColor
                    );
                }
            }

            // Update the final signal value for this bar
            Values[0][0] = indicatorValue;

            // Optional candle painting using the bar colors
            if (PaintCandleColors)
            {
                Brush candleBrush;
                if (emaFast[0] > emaMedium[0] && emaFast[0] > emaSlow[0])
                    candleBrush = BarGreenColor;
                else if (emaFast[0] < emaMedium[0] && emaFast[0] < emaSlow[0])
                    candleBrush = BarRedColor;
                else
                    candleBrush = WhiteColor;

                BarBrush           = candleBrush;
                CandleOutlineBrush = candleBrush;
            }

            // Optional background painting using the background colors
            if (PaintBackground)
            {
                if (emaFast[0] > emaMedium[0] && emaFast[0] > emaSlow[0])
                    BackBrush = BackgroundGreenColor;
                else if (emaFast[0] < emaMedium[0] && emaFast[0] < emaSlow[0])
                    BackBrush = BackgroundRedColor;
                else
                    BackBrush = Brushes.Transparent;
            }
            else
            {
                BackBrush = Brushes.Transparent;
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TripleMACrossover[] cacheTripleMACrossover;
		public TripleMACrossover TripleMACrossover(int eMAFastPeriod, int eMAMediumPeriod, int eMASlowPeriod, bool showSignals, bool paintCandleColors, bool paintBackground, Brush barGreenColor, Brush barRedColor, Brush whiteColor, Brush longSignalColor, double longSignalOffset, Brush shortSignalColor, double shortSignalOffset, Brush backgroundGreenColor, Brush backgroundRedColor)
		{
			return TripleMACrossover(Input, eMAFastPeriod, eMAMediumPeriod, eMASlowPeriod, showSignals, paintCandleColors, paintBackground, barGreenColor, barRedColor, whiteColor, longSignalColor, longSignalOffset, shortSignalColor, shortSignalOffset, backgroundGreenColor, backgroundRedColor);
		}

		public TripleMACrossover TripleMACrossover(ISeries<double> input, int eMAFastPeriod, int eMAMediumPeriod, int eMASlowPeriod, bool showSignals, bool paintCandleColors, bool paintBackground, Brush barGreenColor, Brush barRedColor, Brush whiteColor, Brush longSignalColor, double longSignalOffset, Brush shortSignalColor, double shortSignalOffset, Brush backgroundGreenColor, Brush backgroundRedColor)
		{
			if (cacheTripleMACrossover != null)
				for (int idx = 0; idx < cacheTripleMACrossover.Length; idx++)
					if (cacheTripleMACrossover[idx] != null && cacheTripleMACrossover[idx].EMAFastPeriod == eMAFastPeriod && cacheTripleMACrossover[idx].EMAMediumPeriod == eMAMediumPeriod && cacheTripleMACrossover[idx].EMASlowPeriod == eMASlowPeriod && cacheTripleMACrossover[idx].ShowSignals == showSignals && cacheTripleMACrossover[idx].PaintCandleColors == paintCandleColors && cacheTripleMACrossover[idx].PaintBackground == paintBackground && cacheTripleMACrossover[idx].BarGreenColor == barGreenColor && cacheTripleMACrossover[idx].BarRedColor == barRedColor && cacheTripleMACrossover[idx].WhiteColor == whiteColor && cacheTripleMACrossover[idx].LongSignalColor == longSignalColor && cacheTripleMACrossover[idx].LongSignalOffset == longSignalOffset && cacheTripleMACrossover[idx].ShortSignalColor == shortSignalColor && cacheTripleMACrossover[idx].ShortSignalOffset == shortSignalOffset && cacheTripleMACrossover[idx].BackgroundGreenColor == backgroundGreenColor && cacheTripleMACrossover[idx].BackgroundRedColor == backgroundRedColor && cacheTripleMACrossover[idx].EqualsInput(input))
						return cacheTripleMACrossover[idx];
			return CacheIndicator<TripleMACrossover>(new TripleMACrossover(){ EMAFastPeriod = eMAFastPeriod, EMAMediumPeriod = eMAMediumPeriod, EMASlowPeriod = eMASlowPeriod, ShowSignals = showSignals, PaintCandleColors = paintCandleColors, PaintBackground = paintBackground, BarGreenColor = barGreenColor, BarRedColor = barRedColor, WhiteColor = whiteColor, LongSignalColor = longSignalColor, LongSignalOffset = longSignalOffset, ShortSignalColor = shortSignalColor, ShortSignalOffset = shortSignalOffset, BackgroundGreenColor = backgroundGreenColor, BackgroundRedColor = backgroundRedColor }, input, ref cacheTripleMACrossover);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TripleMACrossover TripleMACrossover(int eMAFastPeriod, int eMAMediumPeriod, int eMASlowPeriod, bool showSignals, bool paintCandleColors, bool paintBackground, Brush barGreenColor, Brush barRedColor, Brush whiteColor, Brush longSignalColor, double longSignalOffset, Brush shortSignalColor, double shortSignalOffset, Brush backgroundGreenColor, Brush backgroundRedColor)
		{
			return indicator.TripleMACrossover(Input, eMAFastPeriod, eMAMediumPeriod, eMASlowPeriod, showSignals, paintCandleColors, paintBackground, barGreenColor, barRedColor, whiteColor, longSignalColor, longSignalOffset, shortSignalColor, shortSignalOffset, backgroundGreenColor, backgroundRedColor);
		}

		public Indicators.TripleMACrossover TripleMACrossover(ISeries<double> input , int eMAFastPeriod, int eMAMediumPeriod, int eMASlowPeriod, bool showSignals, bool paintCandleColors, bool paintBackground, Brush barGreenColor, Brush barRedColor, Brush whiteColor, Brush longSignalColor, double longSignalOffset, Brush shortSignalColor, double shortSignalOffset, Brush backgroundGreenColor, Brush backgroundRedColor)
		{
			return indicator.TripleMACrossover(input, eMAFastPeriod, eMAMediumPeriod, eMASlowPeriod, showSignals, paintCandleColors, paintBackground, barGreenColor, barRedColor, whiteColor, longSignalColor, longSignalOffset, shortSignalColor, shortSignalOffset, backgroundGreenColor, backgroundRedColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TripleMACrossover TripleMACrossover(int eMAFastPeriod, int eMAMediumPeriod, int eMASlowPeriod, bool showSignals, bool paintCandleColors, bool paintBackground, Brush barGreenColor, Brush barRedColor, Brush whiteColor, Brush longSignalColor, double longSignalOffset, Brush shortSignalColor, double shortSignalOffset, Brush backgroundGreenColor, Brush backgroundRedColor)
		{
			return indicator.TripleMACrossover(Input, eMAFastPeriod, eMAMediumPeriod, eMASlowPeriod, showSignals, paintCandleColors, paintBackground, barGreenColor, barRedColor, whiteColor, longSignalColor, longSignalOffset, shortSignalColor, shortSignalOffset, backgroundGreenColor, backgroundRedColor);
		}

		public Indicators.TripleMACrossover TripleMACrossover(ISeries<double> input , int eMAFastPeriod, int eMAMediumPeriod, int eMASlowPeriod, bool showSignals, bool paintCandleColors, bool paintBackground, Brush barGreenColor, Brush barRedColor, Brush whiteColor, Brush longSignalColor, double longSignalOffset, Brush shortSignalColor, double shortSignalOffset, Brush backgroundGreenColor, Brush backgroundRedColor)
		{
			return indicator.TripleMACrossover(input, eMAFastPeriod, eMAMediumPeriod, eMASlowPeriod, showSignals, paintCandleColors, paintBackground, barGreenColor, barRedColor, whiteColor, longSignalColor, longSignalOffset, shortSignalColor, shortSignalOffset, backgroundGreenColor, backgroundRedColor);
		}
	}
}

#endregion
