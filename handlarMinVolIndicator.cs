#region Using declarations
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using System.Linq;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    /// <summary>
    /// It shows min volumes in a bar (less than 7 by default) by handlar.info 2017
    /// </summary>
    [Description("It shows min volumes in a bar (less than 7 by default) by handlar.info 2017")]
    public class handlarMinVolIndicator : Indicator
    {
		#region VARIABLES
		    private string version											= "1.0.3";
		    private bool   playSound                                        = true;
			private DataSeries iDayHigh; 
			private DataSeries iDayLow; 
			private bool showZones 											= true;	
	
			private int minVolume											= 7;
			private bool showOnlyTailed                                     = false;
		    private bool showVolumeValue 									= true;
		
			// описание структуры для хранения уровня
	        private struct myLevelType{
	            public double  		volume;                                                           			// цена уровня
	            public ILine	bar;                                                         			// горизонтальный бар для отображения уровня в профиле
	        }  			
			private myLevelType level;
			private Dictionary<double, myLevelType> vLevels = new Dictionary<double, myLevelType>();				// массив для хранения всех уровней и их объемов
		#endregion VARIABLES

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize() {
            Overlay				= true;
			AutoScale			= false;
			BarsRequired		= 5;
			
			Add(PeriodType.Tick, 1);
			iDayHigh = CurrentDayOHL().CurrentHigh;
			iDayLow = CurrentDayOHL().CurrentLow;
			
			Add(new Plot(Color.FromKnownColor(KnownColor.SkyBlue), PlotStyle.Dot, "MinVol1"));
            Add(new Plot(Color.FromKnownColor(KnownColor.SkyBlue), PlotStyle.Dot, "MinVol2"));
            Add(new Plot(Color.FromKnownColor(KnownColor.SkyBlue), PlotStyle.Dot, "MinVol3"));
            Add(new Plot(Color.FromKnownColor(KnownColor.SkyBlue), PlotStyle.Dot, "MinVol4"));
            Add(new Plot(Color.FromKnownColor(KnownColor.SkyBlue), PlotStyle.Dot, "MinVol5"));
            Add(new Plot(Color.FromKnownColor(KnownColor.SkyBlue), PlotStyle.Dot, "MinVol6"));
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
        /// Накапливаем объемы чтобы выбрать минимальные и показать
        /// </summary>
        private void runVolumeLadder(){
			//Print("volumeLadder is working");
			if(CurrentBars[1] > 0){
				if (vLevels.Count() == 0){
					level.volume = Volumes[1][0];
					level.bar = null;
				
					vLevels.Add(Closes[1][0], level);
				} else {
					if (!vLevels.ContainsKey(Closes[1][0])) {
						level.volume = Volumes[1][0];
						level.bar = null;
						vLevels.Add(Closes[1][0], level);
					} else {
						level = vLevels[Closes[1][0]];
						level.volume += Volumes[1][0];
						vLevels[Closes[1][0]] = level;
					}	
				}
			}
        }     
        #endregion runVolumeLadder
		//=========================================================================================================     
		
		//=========================================================================================================
        #region playSignalSound   
        /// <summary>
        /// Проигрывает нужный звук о найденном уровне
        /// </summary>
        private void playSignalSound(string direction){
			string instr = "";
			if (Instrument.FullName.StartsWith("6A")) instr = "6A";
			if (Instrument.FullName.StartsWith("6B")) instr = "6B";
			if (Instrument.FullName.StartsWith("6C")) instr = "6C";
			if (Instrument.FullName.StartsWith("6E")) instr = "6E";
			if (Instrument.FullName.StartsWith("6J")) instr = "6J";
			if (Instrument.FullName.StartsWith("6S")) instr = "6S";
			if (Instrument.FullName.StartsWith("CL")) instr = "CL";
			if (Instrument.FullName.StartsWith("ES")) instr = "ES";
			if (Instrument.FullName.StartsWith("GC")) instr = "GC";
			if (Instrument.FullName.StartsWith("NG")) instr = "NG";
			
			PlaySound(@"C:\Program Files (x86)\NinjaTrader 8\sounds\"+instr+"-"+direction+".wav");
		}
        #endregion playSignalSound
        //=========================================================================================================				
		
   	 	//=========================================================================================================     
        #region calcAndDrawMinVolume
        // Высчитываем и рисуем минимальный объем
        private void calcAndDrawMinVolume(){
			double priceMinVolume, valueMinVolume;
			int counter = 0;
			
			if (vLevels.Count() >0) {
				//Print("handlarMinVolIndicator ::" + Times[0][0]+"======");
				foreach(KeyValuePair<double, myLevelType> kvp in vLevels.OrderByDescending(key => key.Key)) {	
					if (counter > 5) return;
					//Print (kvp.Key+"  "+kvp.Value.volume);
					if (kvp.Value.volume < minVolume) {
						if (ShowOnlyTailed) {
							
							if (
									priceToInt(Opens[0][0]) < priceToInt(Closes[0][0])
									&& (priceToInt(kvp.Key) < priceToInt(Opens[0][0]))
							) {
								//PlotColors[counter][0] = PlotColors.;
								Values[counter][0] = kvp.Key;
								if (showVolumeValue){
									DrawText(kvp.Key.ToString() + "_" + kvp.Value.volume.ToString() + Times[0][0], kvp.Value.volume.ToString(), -1, kvp.Key, ChartControl.AxisColor);
								}
							}
							
							if (
									priceToInt(Opens[0][0]) > priceToInt(Closes[0][0])
									&& (priceToInt(kvp.Key) > priceToInt(Opens[0][0]))
							) {
								//PlotColors[counter][0] = Color.SkyBlue;
								Values[counter][0] = kvp.Key;
								if (showVolumeValue){
									DrawText(kvp.Key.ToString() + "_" + kvp.Value.volume.ToString() + Times[0][0], kvp.Value.volume.ToString(), -1, kvp.Key, ChartControl.AxisColor);
								}
							}
							
						} else {
							if (priceToInt(kvp.Key) <= priceToInt(Highs[0][0]) && priceToInt(kvp.Key) >= priceToInt(Lows[0][0])) {
								Values[counter][0] = kvp.Key;
								//PlotColors[counter][0] = Color.SkyBlue;
								if (showVolumeValue){
									DrawText(kvp.Key.ToString() + "_" + kvp.Value.volume.ToString() + Times[0][0], kvp.Value.volume.ToString(), -1, kvp.Key, ChartControl.AxisColor);
								}
							}
						}
						counter +=1;
					}
				}
				
				if (showZones) {
					// Если на свече есть минимальный объем

					if (
							isMinValueInHigh()	
							&& (
								(
									(priceToInt(High[1]) == priceToInt(CurrentDayOHL().CurrentHigh[1]))					// если предыдущий бар своим хаем сформировал хай дня
									&& (priceToInt(CurrentDayOHL().CurrentHigh[1]) == (priceToInt(CurrentDayOHL().CurrentHigh[0])))
								)
								|| (
									(priceToInt(High[2]) == priceToInt(CurrentDayOHL().CurrentHigh[2]))					// ИЛИ если второй слева бар своим хаем сформировал хай дня
									&& (priceToInt(CurrentDayOHL().CurrentHigh[2]) == (priceToInt(CurrentDayOHL().CurrentHigh[0])))
								)
							)
						) {
						// draw red zone
						//Print(Times[0][0]+" \ndayHighCur="+dayHighCur+" \ndayHighPrev="+dayHighPrev+" \ndayLowCur="+dayLowCur+" \ndayLowPrev="+dayLowPrev);
						DrawRectangle("mvShort-"+Times[0][1], false, 2, Highs[0][1], -1, Lows[0][0], Color.Red, Color.Red, 5);
						if (playSound) {playSignalSound("short");}
						return;
					}
					
					if (
							isMinValueInLow()
							&& (
								(
									(priceToInt(Low[1]) == priceToInt(CurrentDayOHL().CurrentLow[1]))						// если предыдущий бар своим лоем сформировал лой дня
									&& (priceToInt(CurrentDayOHL().CurrentLow[1]) == (priceToInt(CurrentDayOHL().CurrentLow[0])))
								)
								|| (
									(priceToInt(Low[2]) == priceToInt(CurrentDayOHL().CurrentLow[2]))						// ИЛИ если второй слева бар своим лоем сформировал лой дня
									&& (priceToInt(CurrentDayOHL().CurrentLow[2]) == (priceToInt(CurrentDayOHL().CurrentLow[0])))
								)
							)
						) {
						// draw green zone
						DrawRectangle("mvLong-"+Times[0][1], false, 2, Lows[0][1], -1, Math.Max(Highs[0][0],Highs[0][1]), Color.Green, Color.Green, 9);
						if (playSound) {playSignalSound("long");}
						return;
					}	
				}
			}
        }     
        #endregion calcAndDrawMinVolume
        //=========================================================================================================		
		
		//=========================================================================================================     
        #region isMinValueInLow
		/// <summary>
		/// Проверяем, находится ли минимальный объем на лое свечи, может случится так, что на свечке будет несколько минимальных объемов и если один из них все-таки снизу - это наш случай
		/// </summary>
		/// <returns></returns>
        private bool isMinValueInLow(){
			if (
				(
					(
						(priceToInt(Values[0][0]) > 0)														// если на текущем баре появилcя 1-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[0][0]) <= priceToInt(Open[0]))									// если минимальный объем появился на нижней тени свечи или на лое
					)
					|| (
						(priceToInt(Values[1][0]) > 0)														// если на текущем баре появилcя 2-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[1][0]) <= priceToInt(Open[0]))									// если минимальный объем появился на нижней тени свечи или на лое
					) 
					|| (
						(priceToInt(Values[2][0]) > 0)														// если на текущем баре появилcя 3-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[2][0]) <= priceToInt(Open[0]))									// если минимальный объем появился на нижней тени свечи или на лое
					) 
					|| (
						(priceToInt(Values[3][0]) > 0)														// если на текущем баре появилcя 4-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[3][0]) <= priceToInt(Open[0]))									// если минимальный объем появился на нижней тени свечи или на лое
					) 
					|| (
						(priceToInt(Values[4][0]) > 0)														// если на текущем баре появилcя 5-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[4][0]) <= priceToInt(Open[0]))									// если минимальный объем появился на нижней тени свечи или на лое
					) 
					|| (
						(priceToInt(Values[5][0]) > 0)														// если на текущем баре появилcя 6-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[5][0]) <= priceToInt(Open[0]))									// если минимальный объем появился на нижней тени свечи или на лое
					) 
				)
				&& (priceToInt(Open[0]) <= priceToInt(Close[0]))											// если это бычья свеча ИЛИ если это доджи
			) {
				return true;
			}
			return false;
            
        }
        #endregion isMinValueInLow
		//=========================================================================================================				
	
	
		//=========================================================================================================     
        #region isMinValueInHigh
		/// <summary>
		/// Проверяем, находится ли минимальный объем на хае свечи, может случится так, что на свечке будет несколько минимальных объемов и если один из них все-таки сверху - это наш случай
		/// </summary>
		/// <returns></returns>
        private bool isMinValueInHigh(){
			if (
				(
					(
						(priceToInt(Values[0][0]) > 0)															// если на текущем баре появилcя 1-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[0][0]) >= priceToInt(Open[0]))									// если минимальный объем появился на верхней тени свечи или на хае
					)
					|| (
						(priceToInt(Values[1][0]) > 0)															// если на текущем баре появилcя 2-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[1][0]) >= priceToInt(Open[0]))									// если минимальный объем появился на верхней тени свечи или на хае
					) 
					|| (
						(priceToInt(Values[2][0]) > 0)															// если на текущем баре появилcя 3-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[2][0]) >= priceToInt(Open[0]))									// если минимальный объем появился на верхней тени свечи или на хае
					) 
					|| (
						(priceToInt(Values[3][0]) > 0)															// если на текущем баре появилcя 4-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[3][0]) >= priceToInt(Open[0]))									// если минимальный объем появился на верхней тени свечи или на хае
					) 
					|| (
						(priceToInt(Values[4][0]) > 0)															// если на текущем баре появилcя 5-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[4][0]) >= priceToInt(Open[0]))									// если минимальный объем появился на верхней тени свечи или на хае
					) 
					|| (
						(priceToInt(Values[5][0]) > 0)															// если на текущем баре появилcя 6-й минимальный объем, меньше 7 (по настройкам индикатора)
						&& (priceToInt(Values[5][0]) >= priceToInt(Open[0]))									// если минимальный объем появился на верхней тени свечи или на хае
					) 
				)
				&& (priceToInt(Open[0]) >= priceToInt(Close[0]))										// если это медвежья свеча ИЛИ если это доджи
			) {
				return true;
			}
			return false;
            
        }
        #endregion isMinValueInHigh
		//=========================================================================================================				
		
       	//=========================================================================================================
        #region OnBarUpdate
         protected override void OnBarUpdate(){
			if ((CurrentBars[0] < BarsRequired) || (CurrentBars[1] < BarsRequired)) return;
			
			if (BarsInProgress == 0){
               	if(FirstTickOfBar){
					if (Bars.FirstBarOfSession){
						
						//Print("ПЕРВЫЙ ТИК НА ПЕРВОМ БАРЕ НОВОЙ СЕССИИ НА ОСНОВНОМ ТФ ВРЕМЯ "+Times[0][0].ToString());	
					}
					
					calcAndDrawMinVolume();
	
					if (vLevels.Count() >0) vLevels.Clear();
                }
            }
			

			if (BarsInProgress==1){	
				// запускаем запись информации по объемам
				runVolumeLadder();
			}
        }  
        #endregion OnBarUpdate
        //=========================================================================================================	
		
        #region Properties
	    
		[Description("03 Show volume value")]
        [GridCategory("01. MinVolumes Settings")]
        public int MinVolume
        {
            get { return minVolume; }
            set { minVolume = Math.Max(1, value); }
        }
		
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries MinVol1
        {
            get { return Values[0]; }
        }

        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries MinVol2
        {
            get { return Values[1]; }
        }

        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries MinVol3
        {
            get { return Values[2]; }
        }

        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries MinVol4
        {
            get { return Values[3]; }
        }

        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries MinVol5
        {
            get { return Values[4]; }
        }

        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries MinVol6
        {
            get { return Values[5]; }
        }
		
        [Description("Volume value will show hear the bar")]
        [GridCategory("Parameters")]
        public bool ShowVolumeValue
        {
            get { return showVolumeValue; }
            set { showVolumeValue = value; }
        }

        [Description("Will show only volumes in the tail of bar")]
        [GridCategory("Parameters")]
        public bool ShowOnlyTailed
        {
            get { return showOnlyTailed; }
            set { showOnlyTailed = value; }
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
        private handlarMinVolIndicator[] cachehandlarMinVolIndicator = null;

        private static handlarMinVolIndicator checkhandlarMinVolIndicator = new handlarMinVolIndicator();

        /// <summary>
        /// It shows min volumes in a bar (less than 7 by default) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        public handlarMinVolIndicator handlarMinVolIndicator(int minVolume, bool showOnlyTailed, bool showVolumeValue)
        {
            return handlarMinVolIndicator(Input, minVolume, showOnlyTailed, showVolumeValue);
        }

        /// <summary>
        /// It shows min volumes in a bar (less than 7 by default) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        public handlarMinVolIndicator handlarMinVolIndicator(Data.IDataSeries input, int minVolume, bool showOnlyTailed, bool showVolumeValue)
        {
            if (cachehandlarMinVolIndicator != null)
                for (int idx = 0; idx < cachehandlarMinVolIndicator.Length; idx++)
                    if (cachehandlarMinVolIndicator[idx].MinVolume == minVolume && cachehandlarMinVolIndicator[idx].ShowOnlyTailed == showOnlyTailed && cachehandlarMinVolIndicator[idx].ShowVolumeValue == showVolumeValue && cachehandlarMinVolIndicator[idx].EqualsInput(input))
                        return cachehandlarMinVolIndicator[idx];

            lock (checkhandlarMinVolIndicator)
            {
                checkhandlarMinVolIndicator.MinVolume = minVolume;
                minVolume = checkhandlarMinVolIndicator.MinVolume;
                checkhandlarMinVolIndicator.ShowOnlyTailed = showOnlyTailed;
                showOnlyTailed = checkhandlarMinVolIndicator.ShowOnlyTailed;
                checkhandlarMinVolIndicator.ShowVolumeValue = showVolumeValue;
                showVolumeValue = checkhandlarMinVolIndicator.ShowVolumeValue;

                if (cachehandlarMinVolIndicator != null)
                    for (int idx = 0; idx < cachehandlarMinVolIndicator.Length; idx++)
                        if (cachehandlarMinVolIndicator[idx].MinVolume == minVolume && cachehandlarMinVolIndicator[idx].ShowOnlyTailed == showOnlyTailed && cachehandlarMinVolIndicator[idx].ShowVolumeValue == showVolumeValue && cachehandlarMinVolIndicator[idx].EqualsInput(input))
                            return cachehandlarMinVolIndicator[idx];

                handlarMinVolIndicator indicator = new handlarMinVolIndicator();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.MinVolume = minVolume;
                indicator.ShowOnlyTailed = showOnlyTailed;
                indicator.ShowVolumeValue = showVolumeValue;
                Indicators.Add(indicator);
                indicator.SetUp();

                handlarMinVolIndicator[] tmp = new handlarMinVolIndicator[cachehandlarMinVolIndicator == null ? 1 : cachehandlarMinVolIndicator.Length + 1];
                if (cachehandlarMinVolIndicator != null)
                    cachehandlarMinVolIndicator.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cachehandlarMinVolIndicator = tmp;
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
        /// It shows min volumes in a bar (less than 7 by default) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.handlarMinVolIndicator handlarMinVolIndicator(int minVolume, bool showOnlyTailed, bool showVolumeValue)
        {
            return _indicator.handlarMinVolIndicator(Input, minVolume, showOnlyTailed, showVolumeValue);
        }

        /// <summary>
        /// It shows min volumes in a bar (less than 7 by default) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        public Indicator.handlarMinVolIndicator handlarMinVolIndicator(Data.IDataSeries input, int minVolume, bool showOnlyTailed, bool showVolumeValue)
        {
            return _indicator.handlarMinVolIndicator(input, minVolume, showOnlyTailed, showVolumeValue);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// It shows min volumes in a bar (less than 7 by default) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.handlarMinVolIndicator handlarMinVolIndicator(int minVolume, bool showOnlyTailed, bool showVolumeValue)
        {
            return _indicator.handlarMinVolIndicator(Input, minVolume, showOnlyTailed, showVolumeValue);
        }

        /// <summary>
        /// It shows min volumes in a bar (less than 7 by default) by handlar.info 2017
        /// </summary>
        /// <returns></returns>
        public Indicator.handlarMinVolIndicator handlarMinVolIndicator(Data.IDataSeries input, int minVolume, bool showOnlyTailed, bool showVolumeValue)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.handlarMinVolIndicator(input, minVolume, showOnlyTailed, showVolumeValue);
        }
    }
}
#endregion
