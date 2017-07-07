#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator {
    /// <summary>
    /// It draws simple VWAP (dayly and weekly) by handlar.info 2017
    /// </summary>
    [Description("It draws simple VWAP (dayly and weekly) by handlar.info 2017")]
    public class handlarVWAPIndicator : Indicator {
        #region Variables
			// VWAP Settings			
			public long cumVolume									= 0;
			public long cumVolumeWeekly								= 0;
		
			public long curVolume									= 0;
			public long curVolumeWeekly								= 0;
		
			public double tPrice									= 0;
			public double VPvalue		                            = 0;
			public double VPvalueWeekly		                        = 0;
		
			public double cumVPvalue	                            = 0; 
			public double cumVPvalueWeekly	                        = 0; 
		
			public double VWAPvalue									= 0;
			public double VWAPvalueWeekly							= 0;
		
			public Color dVWAPPOCBrush;
			public Color wVWAPPOCBrush;
		
		    public double dTrend									= 0;
			public double wTrend									= 0;
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize(){
			Add(PeriodType.Tick, 1);
			dVWAPPOCBrush = Color.Transparent;
			// Adds a blue historgram style plot
			Add(new Plot(new Pen(Color.White, 1),  PlotStyle.Line, "dVWAP"));
			Add(new Plot(new Pen(Color.SkyBlue, 3), PlotStyle.Line, "wVWAP"));
			
			Add(new Plot(new Pen(Color.White, 1), PlotStyle.Block, "dVWAPTrend"));
			Add(new Plot(new Pen(Color.White, 1), PlotStyle.Block, "wVWATrend"));
			
			
            Overlay				= true;
			AutoScale           = false;
			BarsRequired 		= 5; // Do not plot until the 11th bar on the chart
        }

        //=========================================================================================================     
        #region priceToInt
        /// <summary>
        /// Преобразуем цену double в int для сравнения, т.к. возникают баги с числами с плавающей точкой
        /// </summary>
		private int priceToInt(double p){
            return Convert.ToInt32(p/TickSize);
        } 
		#endregion priceToInt
        //=========================================================================================================			
		
        //=========================================================================================================     
        #region runVolumeLadder
        /// <summary>
        /// Накапливаем объемы для расчета VWAP
        /// </summary>
        private void runVolumeLadder(){
			if(CurrentBars[1] > 0){
				// Накапливаем объем на свече
				//Print (Times[1][0]+"Накапливаем объем на свече "+curVolume);
				curVolume += Convert.ToInt64(Volumes[1][0]);
				curVolumeWeekly += Convert.ToInt64(Volumes[1][0]);
			}
        }     
        #endregion runVolumeLadder
		//=========================================================================================================   		
		
  	 	//=========================================================================================================     
        #region calcAndDrawVWAP
        /// <summary>
        /// Высчитываем и рисуем VWAP
		/// http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:vwap_intraday
        /// </summary>
        private void calcAndDrawVWAP(){
			if (curVolume >0) {
				tPrice = (Highs[0][0] + Lows[0][0] + Closes[0][0])/3;
				//Print(Times[0][0]+"Средняя цена на прошлом баре "+tPrice);
				
				// шаг 2. Умножаем среднюю цену на суммарный объем прошлого бара
				VPvalue = tPrice * curVolume;
				VPvalueWeekly = tPrice * curVolumeWeekly;
				//Print (Times[0][0]+"Сколько уже объема нсчитал тиковый график "+curVolume);
				
				// шаг 3. Накапливаем значение VPvalue в течении сессии
				cumVPvalue += VPvalue;
				cumVPvalueWeekly += VPvalueWeekly;	
				
				// шаг 4. Накапливаем объем за сессию
				cumVolume += curVolume;
				cumVolumeWeekly +=curVolumeWeekly;
				
				// шаг 5. Получаем значение VWAP на прошлой свече
				VWAPvalue = Math.Floor((cumVPvalue / cumVolume)*(1/TickSize))/(1/TickSize);
				VWAPvalueWeekly = Math.Floor((cumVPvalueWeekly / cumVolumeWeekly)*(1/TickSize))/(1/TickSize);
				
				//Print(Instrument.FullName.ToString()+" [Indicator VWAP] || "+Times[0][0]+"VWAP VWAPvalue [0][0] = "+VWAPvalue);	
				Values[0][0] = VWAPvalue;
				Values[1][0] = VWAPvalueWeekly;
					
				
				// Окрашиваем в цвет тренад прошлого бара текущий, если нет явного сигнала, что больше или меньше вивап хая и лоя
				PlotColors[0][0] = dVWAPPOCBrush;
				PlotColors[2][0] = dVWAPPOCBrush;
				PlotColors[1][0] = wVWAPPOCBrush;
				PlotColors[3][0] = wVWAPPOCBrush;
				
				
				Values[2][0] = dTrend;
				Values[3][0] = wTrend;
				// окрашиваем VWAP, чтобы приблизительно понять тренд
				if (priceToInt(Values[0][0]) > priceToInt(Highs[0][0])) {
					dVWAPPOCBrush = Color.Red;
					PlotColors[0][0] = dVWAPPOCBrush;
					PlotColors[2][0] = dVWAPPOCBrush;
					dTrend = -1;
					Values[2][0] = dTrend;
				}
				
				if (priceToInt(Values[0][0]) < priceToInt(Lows[0][0])) {
					dVWAPPOCBrush = Color.Green;
					PlotColors[0][0] = dVWAPPOCBrush;
					PlotColors[2][0] = dVWAPPOCBrush;
					dTrend = 1;
					Values[2][0] = dTrend;
				}
				
				// окрашиваем недельный VWAP, чтобы приблизительно понять тренд
				if (priceToInt(Values[1][0]) > priceToInt(Highs[1][0])) {
					wVWAPPOCBrush = Color.Red;
					PlotColors[1][0] = wVWAPPOCBrush;
					PlotColors[3][0] = wVWAPPOCBrush;
					wTrend = -1;
					Values[3][0] = wTrend;
				}
				
				if (priceToInt(Values[1][0]) < priceToInt(Lows[1][0])) {
					wVWAPPOCBrush = Color.Green;
					PlotColors[1][0] = wVWAPPOCBrush;
					PlotColors[3][0] = wVWAPPOCBrush;
					wTrend = 1;
					Values[3][0] = wTrend;
				}
				
				// Обнулим накопленный объем за свечу
				curVolume = 0;	
			}
        }     
        #endregion calcAndDrawVWAP
        //=========================================================================================================			
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate(){
			if ((CurrentBars[0] < BarsRequired) || (CurrentBars[1] < BarsRequired)) return;
			
			if (BarsInProgress == 0){
				if(FirstTickOfBar){
					if (Bars.FirstBarOfSession){
						//Print("ПЕРВЫЙ ТИК НА ПЕРВОМ БАРЕ НОВОЙ СЕССИИ НА ОСНОВНОМ ТФ ВРЕМЯ "+Times[0][0].ToString());	
					}
					calcAndDrawVWAP();
                }
            }
			
			if (BarsInProgress==1){	
				if (Bars.FirstBarOfSession){
					// обнуляем накопленные данные для построения VWAP
					cumVPvalue = 0;
					cumVolume = 0;
					if (Times[0][0].DayOfWeek == DayOfWeek.Friday) {
						// обнуляем недельные данные для построения недельного VWAP
						cumVPvalueWeekly = 0;
						cumVolumeWeekly = 0;
					}
				} 
				
				// запускаем запись информации по объемам
				runVolumeLadder();
			}			
        }

        #region Properties
		
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries dVWAP
        {
            get { return Values[0]; }
        }
		
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries wVWAP
        {
            get { return Values[1]; }
        }		
		
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries dVWAPTrend
        {
            get { return Values[2]; }
        }
		
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries wVWAPTrend
        {
            get { return Values[3]; }
        }		
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private handlarVWAPIndicator[] cachehandlarVWAPIndicator = null;

        private static handlarVWAPIndicator checkhandlarVWAPIndicator = new handlarVWAPIndicator();

        /// <summary>
        /// It draws simple VWAP (dayly and weekly) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        public handlarVWAPIndicator handlarVWAPIndicator()
        {
            return handlarVWAPIndicator(Input);
        }

        /// <summary>
        /// It draws simple VWAP (dayly and weekly) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        public handlarVWAPIndicator handlarVWAPIndicator(Data.IDataSeries input)
        {
            if (cachehandlarVWAPIndicator != null)
                for (int idx = 0; idx < cachehandlarVWAPIndicator.Length; idx++)
                    if (cachehandlarVWAPIndicator[idx].EqualsInput(input))
                        return cachehandlarVWAPIndicator[idx];

            lock (checkhandlarVWAPIndicator)
            {
                if (cachehandlarVWAPIndicator != null)
                    for (int idx = 0; idx < cachehandlarVWAPIndicator.Length; idx++)
                        if (cachehandlarVWAPIndicator[idx].EqualsInput(input))
                            return cachehandlarVWAPIndicator[idx];

                handlarVWAPIndicator indicator = new handlarVWAPIndicator();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                Indicators.Add(indicator);
                indicator.SetUp();

                handlarVWAPIndicator[] tmp = new handlarVWAPIndicator[cachehandlarVWAPIndicator == null ? 1 : cachehandlarVWAPIndicator.Length + 1];
                if (cachehandlarVWAPIndicator != null)
                    cachehandlarVWAPIndicator.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cachehandlarVWAPIndicator = tmp;
                return indicator;
            }
        }
    }
}

// This namespace holds all market analyzer column definitions and is required. Do not change it.
namespace NinjaTrader.MarketAnalyzer
{
    public partial class Column : ColumnBase
    {
        /// <summary>
        /// It draws simple VWAP (dayly and weekly) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.handlarVWAPIndicator handlarVWAPIndicator()
        {
            return _indicator.handlarVWAPIndicator(Input);
        }

        /// <summary>
        /// It draws simple VWAP (dayly and weekly) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        public Indicator.handlarVWAPIndicator handlarVWAPIndicator(Data.IDataSeries input)
        {
            return _indicator.handlarVWAPIndicator(input);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// It draws simple VWAP (dayly and weekly) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.handlarVWAPIndicator handlarVWAPIndicator()
        {
            return _indicator.handlarVWAPIndicator(Input);
        }

        /// <summary>
        /// It draws simple VWAP (dayly and weekly) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        public Indicator.handlarVWAPIndicator handlarVWAPIndicator(Data.IDataSeries input)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.handlarVWAPIndicator(input);
        }
    }
}
#endregion
