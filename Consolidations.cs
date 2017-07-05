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
namespace NinjaTrader.Indicator
{
    /// <summary>
    /// Find a consolidations
    /// </summary>
    [Description("Find a consolidations")]
    public class Consolidations : Indicator
    {
        #region Variables
        // Wizard generated variables
            private int myInt = 1; // Default setting for MyInt
            private bool myBool = true; // Default setting for MyBool
            private string myString = @"fff"; // Default setting for MyString
            private double myDouble = 1; // Default setting for MyDouble
		
        // User defined variables (add any user defined variables below)
		
		// описание структуры для хранения паттерна
        private struct myPatternType{
            public double	poc;                                                                  	// цена, по которой было больше всего проторговано объема в рамках консолидации
            public int		startBar;                                                               // номер первого бара консолидации
            public int		stopBar;                                                                // номер последнего бара консолидации
            public int    	status;                                                                 // статус паттерна 0=поиск, 1=найден первый бар возможной консолидации, 2=найден последний бар консолидации
			public int		counter;																// счетчик баров, из которой будет состоять консолидация
			public string	type; 																	// тип паттерна в шорт или в лонг
			public int		startImpulseSize;														// размер импульсной свечи, после которой будем искать консолидацию
            public double   consolidationHigh;                                                      // хай консолидации
            public double   consolidationLow;                                                       // лоу консолидации
        }   
		
		private myPatternType pattern;
		
		private int	minBarsFoConsolidation = 2;
		
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
            Overlay = true;
			AutoScale = false;
            CalculateOnBarClose = false;
			BarsRequired = 3;
			Add(new Plot(Color.FromKnownColor(KnownColor.Orange), PlotStyle.Line, "Plot0"));
            
			pattern.counter		= 0;
			pattern.consolidationLow = 1000000;
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
		#region getBarType
		// определяем тип свечи: bullish, bearish, bullish-doji, bearish-doji
		private string getBarType(int number){
			if (Close[number] > Open[number]){
				return "long";
			}
			if (Close[number] < Open[number]){
				return "short";
			}
			if ((Close[number] == Open[number]) && ((Close[number] - Low[number]) > (High[number] - Close[number]))){
				return "long-doji";
			}
			if ((Close[number] == Open[number]) && ((Close[number] - Low[number]) < (High[number] - Close[number]))){
				return "short-doji";
			}
			return "";
		}		
		#endregion getBarType
		//=========================================================================================================			
		
        //=========================================================================================================     
        #region getBarSize
        // Получаем величину тела свечи
        private int getBarSize(int number){
            return Convert.ToInt32(Math.Abs(priceToInt(High[number]) - priceToInt(Low[number])));
        }     
        #endregion getBarSize
        //=========================================================================================================		
        
        //=========================================================================================================     
        #region findMax
        // Находим максимум на заданном отрезке
        private double findMax(int startBar, int stopBar){
			double maxPrice = 0;
            for (int i = stopBar; i <= startBar; i++){
                if (High[i] > maxPrice){
                    maxPrice = High[i];
                }
            }
			return maxPrice;
        }     
        #endregion findMax
        //=========================================================================================================			
		
        //=========================================================================================================     
        #region findMin
        // Находим минимум на заданном отрезке
        private double findMin(int startBar, int stopBar){
			double minPrice = 1000000;
            for (int i = stopBar; i <= startBar; i++){
                if(Low[i] < minPrice){
                    minPrice = Low[i];
                }
            }
			return minPrice;
        }     
        #endregion findMin
        //=========================================================================================================		
		
        //=========================================================================================================     
        #region drawArea
        // Рисуем прямоугольник, который обводит найденную консолидацию
        private void drawArea(int startBar, int stopBar, string tag, int opacity){
            double minPrice = 1000000, maxPrice = 0;

            for (int i = stopBar; i <= startBar; i++){
                if(Low[i] < minPrice){
                    minPrice = Low[i];
                }
                
                if (High[i] > maxPrice){
                    maxPrice = High[i];
                }
            }
            minPrice -= TickSize;
            maxPrice += TickSize;
			if (pattern.type == "long") {
            	DrawRectangle("area-"+Time[startBar]+"__"+tag, false, startBar, minPrice, stopBar, maxPrice, Color.Red, Color.Red, opacity);
			} else if (pattern.type == "short") {
            	DrawRectangle("area-"+Time[startBar]+"__"+tag, false, startBar, minPrice, stopBar, maxPrice, Color.Green, Color.Green, opacity);
			} else if (pattern.type == "") {
            	//DrawRectangle("area-"+Time[startBar]+"__"+tag, false, startBar, minPrice, stopBar, maxPrice, Color.Yellow, Color.Yellow, opacity);
				DrawRectangle("area-"+Time[startBar]+"__cRange", false, startBar+1, minPrice, stopBar-1, maxPrice, Color.Yellow, Color.Yellow, 1);
			}
        }     
        #endregion drawArea
        //=========================================================================================================     
		
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate() {
            // Use this method for calculating your indicator values. Assign a value to each
            // plot below by replacing 'Close[0]' with your own formula.
            //Plot0.Set(Close[0]);
			if ((CurrentBars[0] < BarsRequired) || (CurrentBars[0] < BarsRequired)) return;
			
			// если мы уже нашли первый бар и нужно считать бары консолидации
			if (pattern.status == 1) {
				if (pattern.counter <= 9) {
					
					/*if (
							(
								(getBarType(1) == "short")
								&& (High[0] > High[1])
							)
							||
							(
								(getBarType(1) == "long")
								&& (Low[0] < Low[1])
							)						
					) {
						pattern.counter = 5;
						pattern.status = 0;
						
					}*/
					
					// проверяем текущий бар можно считать очередным баром в консолидаци?
					if (
						((priceToInt(getBarSize(0))*2) < (priceToInt(pattern.startImpulseSize)))
					) {
						// текущий бар соответствует требованиям бара в консолидации, считаем бары консолидации дальше
						//drawArea(pattern.startBar, pattern.stopBar, "myConsolidation", 2);
						pattern.startBar += 1;
                        pattern.stopBar = 0;
						pattern.counter += 1;
						
						double tmpCloseImpulseSize = (High[2]-Low[0])/TickSize;
						
						//Print (Time[0]+" +1 бар в консолидацию pattern.status=1 start="+pattern.startBar+" stop"+pattern.stopBar+" pattern.counter="+" pattern.startImpulseSize="+pattern.counter+pattern.startImpulseSize+" (High[2]-Low[0])/TickSize)="+tmpCloseImpulseSize+" pattern.startImpulseSize="+pattern.startImpulseSize);
						Print (Time[0]+" +1 бар в консолидацию pattern.consolidationHigh="+pattern.consolidationHigh+" pattern.consolidationLow"+pattern.consolidationLow);
                        // обновляем хай и лоу консолидации, если нужно
                        if (priceToInt(pattern.consolidationHigh) < priceToInt(High[0])) pattern.consolidationHigh = High[0];
                        if (priceToInt(pattern.consolidationLow) > priceToInt(Low[0])) pattern.consolidationLow = Low[0];
						
						// дальше нужно проверить, может быть консолидация закрывается не одним импульсным баром, а несколькими, как тут, например http://joxi.ru/L215RJC8LVLZ2X
						// проверим, если расстояние, которое прошли три бара равно или больше размеру импульсного бара, то тоже будем считать, что консолидация найдена
						if (
							(pattern.startImpulseSize <= (Convert.ToInt32((High[2]-Low[0])/TickSize))) 
						){
							// !!!АРКА СФОРМИРОВАЛАСЬ!!!!
							pattern.stopBar = 3;
							pattern.startBar -= 1;
							pattern.counter -= 3;
							pattern.consolidationHigh = findMax(pattern.startBar, pattern.stopBar);
							pattern.consolidationLow = findMin(pattern.startBar, pattern.stopBar);
							Print (Time[0]+" арка сформировалась, длина арки="+pattern.counter+" startBar="+pattern.startBar+" stopBar="+pattern.stopBar);
							DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar, pattern.consolidationLow-TickSize, pattern.stopBar, pattern.consolidationHigh+TickSize, Color.Yellow, Color.Yellow, 1);
				
							// обнуляем после формирования арки
							pattern.counter = 0;
							pattern.startBar = 0;
							pattern.startImpulseSize = 0;
							pattern.status = 0;
							pattern.stopBar = 0;
							pattern.type = "";
							pattern.consolidationLow = 1000000;
							pattern.consolidationHigh = 0;
						}

						
					} else {
						// текущий бар соответствует требованиям бара в консолидации
						
						// проверим, это просто патерн уже не получится сформировать ИЛИ появился импульсный закрывающий бар и паттерн сформирован и консолидация найдена?
						
						if (priceToInt(getBarSize(0)) > (priceToInt(getBarSize(1))*2)) {
							// если у нас есть закрывающий импульсный бар, то проверяем, есть необходимое количество баров в консолидации, минимум 2 бара
							// и также проверим, чтобы направление закрывающего импульсного бара было противоположным направлению открывающего импульсного бара
							if ((getBarType(0) != pattern.type) && (getBarType(0) != "short-doji") && (getBarType(0) != "long-doji")) {
								// итак, у нас очередной бар консолидации оказался больше в два или больше раз, чем предыдущий бар консолидации и он противоположного направления
								// если сравнивать направление с направлением открывающего импульсного бара. 
								
								// !!!АРКА СФОРМИРОВАЛАСЬ!!!!
								pattern.stopBar = 1;
								Print (Time[0]+" арка сформировалась, длина арки="+pattern.counter+" startBar="+pattern.startBar+" stopBar="+pattern.stopBar);
								DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar+1, Low[pattern.startBar]-TickSize, pattern.stopBar-1, High[pattern.stopBar]+TickSize, Color.Yellow, Color.Yellow, 1);
								
								// обнуляем после формирования арки
								pattern.counter = 0;
								pattern.startBar = 0;
								pattern.startImpulseSize = 0;
								pattern.status = 0;
								pattern.stopBar = 0;
								pattern.type = "";
							pattern.consolidationLow = 1000000;
							pattern.consolidationHigh = 0;
							}
						}
					}; 
				} else {
					// если баров консолидаци больше 5, то паттерн считаем не сформированным, и выставляем статус паттерна в 0
					pattern.counter = 5;
					pattern.status = 0;
					pattern.startBar = 0;
					pattern.stopBar = 0;
					Print (Time[0]+" за 10 баров паттерн не сформировался");
				}
			}
			
			// если мы находимся в поиске бара, с которого начнется консолидация
			if (pattern.status == 0) {
				//Print (Time[0]+" pattern.status=0");
				// если пред предыдущий бар больше предыдущего в 2 и более раз, то ничего не делаем
				//if (priceToInt(getBarSize(1)) > (priceToInt(getBarSize(2))*2)) return; 
				
				// если найден бар начала консолидации, т.е. мы обрабатываем нулевой бар, как на рисунке http://joxi.ru/brRgYnIJ38nQm1
				if (
						((priceToInt(getBarSize(0))*2) < (priceToInt(getBarSize(1))))
						//&& (priceToInt(getBarSize(1)) > (priceToInt(getBarSize(2))*2))
						//&& (getBarSize(0) > 10) 
				){
					Print (Time[0]+" pattern.status=1 первый бар консолидации="+getBarSize(0)+" импульсный бар="+getBarSize(1)+" бар перед импульсным="+getBarSize(2));
					pattern.status = 1;
					pattern.startBar = 1;
					pattern.counter = 1;
					pattern.startImpulseSize = getBarSize(1);
					pattern.consolidationHigh = High[0];
					pattern.consolidationLow = Low[0];
					
					
					//drawArea(pattern.startBar, pattern.stopBar, "myConsolidation", 2);
					if (Open[0] > Close[0]) {
						pattern.type = "short";
					}
					
					if (Open[0] > Close[0]) {
						pattern.type = "long";
					}
				}
			}
        }

        #region Properties
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries Plot0
        {
            get { return Values[0]; }
        }

        [Description("My temp int")]
        [GridCategory("Parameters")]
        public int MyInt
        {
            get { return myInt; }
            set { myInt = Math.Max(1, value); }
        }

        [Description("My temp bool")]
        [GridCategory("Parameters")]
        public bool MyBool
        {
            get { return myBool; }
            set { myBool = value; }
        }

        [Description("My temp string")]
        [GridCategory("Parameters")]
        public string MyString
        {
            get { return myString; }
            set { myString = value; }
        }

        [Description("My temp double")]
        [GridCategory("Parameters")]
        public double MyDouble
        {
            get { return myDouble; }
            set { myDouble = Math.Max(1, value); }
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
        private Consolidations[] cacheConsolidations = null;

        private static Consolidations checkConsolidations = new Consolidations();

        /// <summary>
        /// Find a consolidations
        /// </summary>
        /// <returns></returns>
        public Consolidations Consolidations(bool myBool, double myDouble, int myInt, string myString)
        {
            return Consolidations(Input, myBool, myDouble, myInt, myString);
        }

        /// <summary>
        /// Find a consolidations
        /// </summary>
        /// <returns></returns>
        public Consolidations Consolidations(Data.IDataSeries input, bool myBool, double myDouble, int myInt, string myString)
        {
            if (cacheConsolidations != null)
                for (int idx = 0; idx < cacheConsolidations.Length; idx++)
                    if (cacheConsolidations[idx].MyBool == myBool && Math.Abs(cacheConsolidations[idx].MyDouble - myDouble) <= double.Epsilon && cacheConsolidations[idx].MyInt == myInt && cacheConsolidations[idx].MyString == myString && cacheConsolidations[idx].EqualsInput(input))
                        return cacheConsolidations[idx];

            lock (checkConsolidations)
            {
                checkConsolidations.MyBool = myBool;
                myBool = checkConsolidations.MyBool;
                checkConsolidations.MyDouble = myDouble;
                myDouble = checkConsolidations.MyDouble;
                checkConsolidations.MyInt = myInt;
                myInt = checkConsolidations.MyInt;
                checkConsolidations.MyString = myString;
                myString = checkConsolidations.MyString;

                if (cacheConsolidations != null)
                    for (int idx = 0; idx < cacheConsolidations.Length; idx++)
                        if (cacheConsolidations[idx].MyBool == myBool && Math.Abs(cacheConsolidations[idx].MyDouble - myDouble) <= double.Epsilon && cacheConsolidations[idx].MyInt == myInt && cacheConsolidations[idx].MyString == myString && cacheConsolidations[idx].EqualsInput(input))
                            return cacheConsolidations[idx];

                Consolidations indicator = new Consolidations();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.MyBool = myBool;
                indicator.MyDouble = myDouble;
                indicator.MyInt = myInt;
                indicator.MyString = myString;
                Indicators.Add(indicator);
                indicator.SetUp();

                Consolidations[] tmp = new Consolidations[cacheConsolidations == null ? 1 : cacheConsolidations.Length + 1];
                if (cacheConsolidations != null)
                    cacheConsolidations.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheConsolidations = tmp;
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
        /// Find a consolidations
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.Consolidations Consolidations(bool myBool, double myDouble, int myInt, string myString)
        {
            return _indicator.Consolidations(Input, myBool, myDouble, myInt, myString);
        }

        /// <summary>
        /// Find a consolidations
        /// </summary>
        /// <returns></returns>
        public Indicator.Consolidations Consolidations(Data.IDataSeries input, bool myBool, double myDouble, int myInt, string myString)
        {
            return _indicator.Consolidations(input, myBool, myDouble, myInt, myString);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Find a consolidations
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.Consolidations Consolidations(bool myBool, double myDouble, int myInt, string myString)
        {
            return _indicator.Consolidations(Input, myBool, myDouble, myInt, myString);
        }

        /// <summary>
        /// Find a consolidations
        /// </summary>
        /// <returns></returns>
        public Indicator.Consolidations Consolidations(Data.IDataSeries input, bool myBool, double myDouble, int myInt, string myString)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.Consolidations(input, myBool, myDouble, myInt, myString);
        }
    }
}
#endregion
