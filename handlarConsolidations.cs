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
    /// Find the consolidations
    /// </summary>
    [Description("Find the consolidations")]
    public class handlarConsolidations : Indicator
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
			public double	startImpulseLow;														// лой импульсной свечи, после которой будем искать консолидацию
			public double	startImpulseHigh;														// хай импульсной свечи, после которой будем искать консолидацию
            public double   consolidationHigh;                                                      // хай консолидации
            public double   consolidationLow;                                                       // лоу консолидации
			public double	ATRfactorStart;															// во сколько раз разница между ATR стартового импульсного бара и предыдущего бара больше, чем разница между ATR предыдущего и пред-предыдущего бара  http://joxi.ru/VrwyMeHOay5y2X
			public double	ATRfactorStop;															// во сколько раз разница между ATR стопового импульсного бара и предыдущего бара больше, чем разница между ATR предыдущего и пред-предыдущего бара  
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
        #region ATRToInt
        /// <summary>
        /// Преобразуем значение ATR double в int для сравнения, т.к. возникают баги с числами с плавающей точкой
        /// </summary>
        private int ATRToInt(double p){
            //return Convert.ToInt32(p*10000/TickSize);
            if (
                    Instrument.ToString().StartsWith("6A") 
                    || Instrument.ToString().StartsWith("6C") 
                    || Instrument.ToString().StartsWith("6E")
                    || Instrument.ToString().StartsWith("6S")
                ) {
                return Convert.ToInt32(p*1000000);
            }
            if (Instrument.ToString().StartsWith("6J")) {
                return Convert.ToInt32(p*10000000);
            }
            if (Instrument.ToString().StartsWith("NG")) {
                return Convert.ToInt32(p*100000);
            }
            if (
                    Instrument.ToString().StartsWith("CL")
                    || Instrument.ToString().StartsWith("ES")   
					|| Instrument.ToString().StartsWith("6B")
                ) {
                return Convert.ToInt32(p*10000);
            }
            if (
                    Instrument.ToString().StartsWith("GC")  
                ) {
                return Convert.ToInt32(p*100);
            }
            return 0;
        }     
        #endregion ATRToInt
        //========================================================================================================= 		
		
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
		
        //=========================================================================================================     
        #region clearPattern
        // Обнуляем паттерн
        private void clearPattern(){
			pattern.counter = 0;
			pattern.startBar = 0;
			pattern.startImpulseSize = 0;
			pattern.startImpulseHigh = 0;
			pattern.startImpulseLow = 0;
			pattern.status = 0;
			pattern.stopBar = 0;
			pattern.type = "";
			pattern.consolidationLow = 1000000;
			pattern.consolidationHigh = 0;	
			return;
        }     
        #endregion clearPattern
        //=========================================================================================================		
				
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate() {
            // Use this method for calculating your indicator values. Assign a value to each
            // plot below by replacing 'Close[0]' with your own formula.
            //Plot0.Set(Close[0]);
			if ((CurrentBars[0] < BarsRequired) || (CurrentBars[0] < BarsRequired)) return;
			
			if (FirstTickOfBar) {
				// если мы уже нашли первый бар и нужно считать бары консолидации
				if (pattern.status == 1) {
					if (pattern.counter <= 9) {
						
						// проверяем текущий бар можно считать очередным баром в консолидаци?
						// чтобы бар считать очередным баром в консолидации он должен быть
						// в ШОРТ
						// 1) лой свечи не должен заходить в нижнюю треть стартового импульсного бара, как тут  http://joxi.ru/YmEgNpI035jqA6
						// 2) лой свечи не должен быть выше хая стартового импульсного бара как тут http://joxi.ru/J2bx3XuXozvlr6
						// в ЛОНГ аналогично
						if (
							//((priceToInt(getBarSize(0))) <= (priceToInt(pattern.startImpulseSize*0.65)))
							// шорт
							(
								//(getBarType(pattern.startBar+1) == "bullish")														
								pattern.type == "short"
								&& (priceToInt(Low[0]) >= priceToInt(pattern.startImpulseLow + (pattern.startImpulseSize/3)*TickSize))
								&& (priceToInt(Low[0]) <= priceToInt(pattern.startImpulseHigh))
							)
							||
							// лонг
							(
								//(getBarType(pattern.startBar+1) == "bearish")														
								pattern.type == "long"
								&& (priceToInt(High[0]) <= priceToInt(pattern.startImpulseHigh - (pattern.startImpulseSize/3)*TickSize))
								//&& (priceToInt(High[0]) >= priceToInt(pattern.startImpulseLow))
							)
						) {
							// текущий бар соответствует требованиям бара в консолидации, считаем бары консолидации дальше
							// но сначала проверим, если ATRfactor зашкаливает, значит считаем это завершающей свечей импульсной
							
							// считаем ATRfactorStop
							pattern.ATRfactorStop = Math.Abs((ATRToInt(ATR(14)[0]) - ATRToInt(ATR(14)[1]))) / ( (Math.Abs((ATRToInt(ATR(14)[1]) - ATRToInt(ATR(14)[2]))) > 0) ? (Math.Abs((ATRToInt(ATR(14)[1]) - ATRToInt(ATR(14)[2])))) : 1 );
							
							if (	
								(pattern.ATRfactorStop > 2)
								&& (pattern.type == getBarType(0))
							) {
							
								// !!!АРКА СФОРМИРОВАЛАСЬ!!!! 
								// ЗАКРЫТИЕ ИМПУЛЬСНЫМ БАРОМ
								// осталось проверить условие по ATR
								pattern.stopBar = 1;
								
								// считаем ATRfactorStart
								pattern.ATRfactorStart = Math.Abs((ATRToInt(ATR(14)[pattern.startBar+1]) - ATRToInt(ATR(14)[pattern.startBar+2]))) / ( (Math.Abs((ATRToInt(ATR(14)[pattern.startBar+2]) - ATRToInt(ATR(14)[pattern.startBar+3]))) > 0) ? (Math.Abs((ATRToInt(ATR(14)[pattern.startBar+2]) - ATRToInt(ATR(14)[pattern.startBar+3])))) : 1  );
								
								//if (pattern.ATRfactorStart >= 2 && pattern.ATRfactorStop >= 2) {
								
									Print (Time[0]+" арка сформировалась, длина арки="+pattern.counter+" ATRfactorStart="+pattern.ATRfactorStart+" ATRfactorStop="+pattern.ATRfactorStop+" startBar="+pattern.startBar+" stopBar="+pattern.stopBar +" [закрытие импульсным баром]");
									if (pattern.type == "short") {
										DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar, pattern.consolidationLow, pattern.stopBar, pattern.consolidationHigh, Color.Red, Color.Red, 2);
									}
									
									if (pattern.type == "long") {
										DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar, pattern.consolidationLow, pattern.stopBar, pattern.consolidationHigh, Color.Green, Color.Green, 2);
									} 
								/*} else {
									Print (Time[0]+" арка сформировалась, но ATRfactor не достаточные"+" ATRfactorStart="+pattern.ATRfactorStart+" ATRfactorStop="+pattern.ATRfactorStop);
								}*/
								// обнуляем после формирования арки
								clearPattern();	
								
								
							} else {
							
								
								//drawArea(pattern.startBar, pattern.stopBar, "myConsolidation", 2);
								pattern.startBar += 1;
								pattern.stopBar = 0;
								pattern.counter += 1;
								
								double tmpCloseImpulseSize = (High[2]-Low[0])/TickSize;
								
								//Print (Time[0]+" +1 бар в консолидацию pattern.status=1 start="+pattern.startBar+" stop"+pattern.stopBar+" pattern.counter="+" pattern.startImpulseSize="+pattern.counter+pattern.startImpulseSize+" (High[2]-Low[0])/TickSize)="+tmpCloseImpulseSize+" pattern.startImpulseSize="+pattern.startImpulseSize);
								
								// обновляем хай и лоу консолидации, если нужно
								if (priceToInt(pattern.consolidationHigh) < priceToInt(High[0])) pattern.consolidationHigh = High[0];
								if (priceToInt(pattern.consolidationLow) > priceToInt(Low[0])) pattern.consolidationLow = Low[0];
								Print (Time[0]+" +1 бар в консолидацию, всего баров="+pattern.counter+" pattern.consolidationHigh="+pattern.consolidationHigh+" pattern.consolidationLow"+pattern.consolidationLow);
								
								
								// дальше нужно проверить, может быть консолидация закрывается не одним импульсным баром, а несколькими, как тут, например http://joxi.ru/L215RJC8LVLZ2X
								// проверим, если 
								// для ШОРТА
								// 1) расстояние, которое прошли три бара равно или больше размеру импульсного бара, 
								// 2) лой третьего проверяемого бара ниже лоя стартового импульсного бара,
								// то тоже будем считать, что консолидация найдена
								// причем, pattern.counter должен быть не меньше 5 (3 это закрывающие несколько баров и минимум 2 для самой консолидации)
								// для ЛОНГов аналогично
								
								if (
									// шорт
									(
										//(getBarType(pattern.startBar+1) == "bullish")
										pattern.type == "short"
										&& (pattern.startImpulseSize <= (Convert.ToInt32((High[2]-Low[0])/TickSize))) 	// 1)
										&& (priceToInt(Low[0]) < priceToInt(pattern.startImpulseLow))					// 2)
										&& pattern.counter >=5
									)
									||
									// лонг
									(
										//(getBarType(pattern.startBar+1) == "bearish")
										(pattern.type == "long")
										&& (pattern.startImpulseSize <= (Convert.ToInt32((High[0]-Low[2])/TickSize))) 	// 1)
										&& (priceToInt(High[0]) > priceToInt(pattern.startImpulseHigh))					// 2)
										&& pattern.counter >=5
									)
								){
									// !!!АРКА СФОРМИРОВАЛАСЬ!!!!
									// закрытие несколькими барами
									pattern.stopBar = 3;
									pattern.startBar -= 1;
									pattern.counter -= 3;
									pattern.consolidationHigh = findMax(pattern.startBar, pattern.stopBar);
									pattern.consolidationLow = findMin(pattern.startBar, pattern.stopBar);
									Print (Time[0]+" арка сформировалась, длина арки="+pattern.counter+" startBar="+pattern.startBar+" stopBar="+pattern.stopBar+" [закрытие несколькими барами]");
									if (pattern.type == "short") {
										DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar, pattern.consolidationLow-TickSize, pattern.stopBar, pattern.consolidationHigh+TickSize, Color.Red, Color.Red, 2);
									}
									if (pattern.type == "long") {
										DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar, pattern.consolidationLow-TickSize, pattern.stopBar, pattern.consolidationHigh+TickSize, Color.Green, Color.Green, 2);
									}
									
									// обнуляем после формирования арки
									clearPattern();
								}	
								
								
								
								// также нужно проверить, может быть в ходе поиска очередного бара консолидации появится новый стартовый импульсный бар?
								// причем размер нового импульсного бара должен быть больше чем размер текущего стартового импульсного бара
								if (
								((priceToInt(getBarSize(0))) <= (priceToInt(getBarSize(1)*0.8)))
								//&& (priceToInt(getBarSize(1)) > (priceToInt(getBarSize(2))*2))
								&& (getBarSize(0) > pattern.startImpulseSize) 
								){
									Print (Time[0]+" pattern.status=1 импульсный бар="+getBarSize(1)+ " первый бар консолидации="+getBarSize(0)+" начинаем считать бары");
									pattern.status = 1;
									pattern.startBar = 1;
									pattern.counter = 1;
									pattern.startImpulseSize = getBarSize(1);
									pattern.startImpulseHigh = High[1];
									pattern.startImpulseLow = Low[1];
									pattern.consolidationHigh = High[0];
									pattern.consolidationLow = Low[0];
									
									
									//drawArea(pattern.startBar, pattern.stopBar, "myConsolidation", 2);
									if (Open[1] < Close[1]) {
										pattern.type = "short";
									}
									
									if (Open[1] > Close[1]) {
										pattern.type = "long";
									}
								}
							}
						} else {
							// текущий бар НЕ соответствует требованиям бара в консолидации
							
							// проверим, это просто патерн уже не получится сформировать ИЛИ появился импульсный закрывающий бар и паттерн сформирован и консолидация найдена?
							//Print (Time[0]+" бар не подходит для консолидации getBarSize(0)="+getBarSize(0)+" pattern.startImpulseSize="+pattern.startImpulseSize+" getBarType(0)="+getBarType(0)+" pattern.type="+pattern.type);
						
							if (
									(priceToInt(getBarSize(0)*2) > (priceToInt(getBarSize(1))))
							) {
								// если у нас есть закрывающий импульсный бар, то проверяем, есть необходимое количество баров в консолидации, минимум 2 бара
								// и также проверим, чтобы направление закрывающего импульсного бара было противоположным направлению открывающего импульсного бара
								
								if (
									(getBarType(0) == pattern.type)
									/*&& (
										(
											(pattern.type == "long")
											&& (priceToInt(Low[0]) < priceToInt(pattern.startImpulseHigh))
										)
										||
										(
											(pattern.type == "short")
											&& (priceToInt(High[0]) > priceToInt(pattern.startImpulseLow))
										)
									)*/
								) {
									// итак, у нас очередной бар консолидации оказался больше в два или больше раз, чем предыдущий бар консолидации и он противоположного направления
									// если сравнивать направление с направлением открывающего импульсного бара. 
									
									Print (Time[0]+" текущий бар импульсный закрывающий pattern.type="+pattern.type+" getBarType(0)="+getBarType(0)+ " getBarSize(0)="+ getBarSize(0));
									if (pattern.counter >=2) {
										
										// !!!АРКА СФОРМИРОВАЛАСЬ!!!! 
										// ЗАКРЫТИЕ ИМПУЛЬСНЫМ БАРОМ
										// осталось проверить условие по ATR
										pattern.stopBar = 1;
										
										// считаем ATRfactorStart
										pattern.ATRfactorStart = Math.Abs((ATRToInt(ATR(14)[pattern.startBar+1]) - ATRToInt(ATR(14)[pattern.startBar+2]))) / ( (Math.Abs((ATRToInt(ATR(14)[pattern.startBar+2]) - ATRToInt(ATR(14)[pattern.startBar+3]))) > 0) ? (Math.Abs((ATRToInt(ATR(14)[pattern.startBar+2]) - ATRToInt(ATR(14)[pattern.startBar+3])))) : 1  );
										// считаем ATRfactorStop
										pattern.ATRfactorStop = Math.Abs((ATRToInt(ATR(14)[0]) - ATRToInt(ATR(14)[1]))) / ( (Math.Abs((ATRToInt(ATR(14)[1]) - ATRToInt(ATR(14)[2]))) > 0) ? (Math.Abs((ATRToInt(ATR(14)[1]) - ATRToInt(ATR(14)[2])))) : 1 );
							
										//if (pattern.ATRfactorStart >= 2 && pattern.ATRfactorStop >= 2) {
										
											Print (Time[0]+" арка сформировалась, длина арки="+pattern.counter+" ATRfactorStart="+pattern.ATRfactorStart+" ATRfactorStop="+pattern.ATRfactorStop+" startBar="+pattern.startBar+" stopBar="+pattern.stopBar +" [закрытие импульсным баром]");
											if (pattern.type == "short") {
												DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar, pattern.consolidationLow, pattern.stopBar, pattern.consolidationHigh, Color.Red, Color.Red, 2);
											}
											
											if (pattern.type == "long") {
												DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar, pattern.consolidationLow, pattern.stopBar, pattern.consolidationHigh, Color.Green, Color.Green, 2);
											} 
										/*} else {
											Print (Time[0]+" арка сформировалась, но ATRfactor не достаточные"+" ATRfactorStart="+pattern.ATRfactorStart+" ATRfactorStop="+pattern.ATRfactorStop);
										}*/
										// обнуляем после формирования арки
										clearPattern();
										
									} else if (pattern.counter == 1){
										//Print (Time[0]+"!!! арка сформировалась, НО длина арки=1 startBar="+pattern.startBar+" stopBar="+pattern.stopBar);
										/*if (pattern.type == "short") {
											DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar+1, Low[pattern.startBar], pattern.stopBar, High[pattern.stopBar+1], Color.Red, Color.Red, 2);
										}
										if (pattern.type == "long") {
											DrawRectangle("area-"+Time[0]+"__cRange", false, pattern.startBar+1, Low[pattern.startBar], pattern.stopBar, High[pattern.stopBar+1], Color.Green, Color.Green, 2);
										}*/
										// обнуляем после формирования арки
										clearPattern();
									}
								} else {
									Print (Time[0]+" бар не подходит для консолидации");
									clearPattern();
								}
							} else {
								Print (Time[0]+" бар не подходит для консолидации, паттерн не сформирован");
								clearPattern();
							}
						}; 
					} else {
						// если баров консолидаци больше 5, то паттерн считаем не сформированным, и выставляем статус паттерна в 0
						clearPattern();
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
							((priceToInt(getBarSize(0))) <= (priceToInt(getBarSize(1)*0.8)))
							//&& (priceToInt(getBarSize(1)) > (priceToInt(getBarSize(2))*2))
							//&& (getBarSize(0) > 10) 
					){
						Print (Time[0]+" pattern.status=1 импульсный бар="+getBarSize(1)+ " первый бар консолидации="+getBarSize(0)+" начинаем считать бары");
						pattern.status = 1;
						pattern.startBar = 1;
						pattern.counter = 1;
						pattern.startImpulseSize = getBarSize(1);
						pattern.startImpulseHigh = High[1];
						pattern.startImpulseLow = Low[1];
						pattern.consolidationHigh = High[0];
						pattern.consolidationLow = Low[0];
						
						
						//drawArea(pattern.startBar, pattern.stopBar, "myConsolidation", 2);
						if (Open[1] < Close[1]) {
							pattern.type = "short";
						}
						
						if (Open[1] > Close[1]) {
							pattern.type = "long";
						}
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
        private handlarConsolidations[] cachehandlarConsolidations = null;

        private static handlarConsolidations checkhandlarConsolidations = new handlarConsolidations();

        /// <summary>
        /// Find the consolidations
        /// </summary>
        /// <returns></returns>
        public handlarConsolidations handlarConsolidations(bool myBool, double myDouble, int myInt, string myString)
        {
            return handlarConsolidations(Input, myBool, myDouble, myInt, myString);
        }

        /// <summary>
        /// Find the consolidations
        /// </summary>
        /// <returns></returns>
        public handlarConsolidations handlarConsolidations(Data.IDataSeries input, bool myBool, double myDouble, int myInt, string myString)
        {
            if (cachehandlarConsolidations != null)
                for (int idx = 0; idx < cachehandlarConsolidations.Length; idx++)
                    if (cachehandlarConsolidations[idx].MyBool == myBool && Math.Abs(cachehandlarConsolidations[idx].MyDouble - myDouble) <= double.Epsilon && cachehandlarConsolidations[idx].MyInt == myInt && cachehandlarConsolidations[idx].MyString == myString && cachehandlarConsolidations[idx].EqualsInput(input))
                        return cachehandlarConsolidations[idx];

            lock (checkhandlarConsolidations)
            {
                checkhandlarConsolidations.MyBool = myBool;
                myBool = checkhandlarConsolidations.MyBool;
                checkhandlarConsolidations.MyDouble = myDouble;
                myDouble = checkhandlarConsolidations.MyDouble;
                checkhandlarConsolidations.MyInt = myInt;
                myInt = checkhandlarConsolidations.MyInt;
                checkhandlarConsolidations.MyString = myString;
                myString = checkhandlarConsolidations.MyString;

                if (cachehandlarConsolidations != null)
                    for (int idx = 0; idx < cachehandlarConsolidations.Length; idx++)
                        if (cachehandlarConsolidations[idx].MyBool == myBool && Math.Abs(cachehandlarConsolidations[idx].MyDouble - myDouble) <= double.Epsilon && cachehandlarConsolidations[idx].MyInt == myInt && cachehandlarConsolidations[idx].MyString == myString && cachehandlarConsolidations[idx].EqualsInput(input))
                            return cachehandlarConsolidations[idx];

                handlarConsolidations indicator = new handlarConsolidations();
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

                handlarConsolidations[] tmp = new handlarConsolidations[cachehandlarConsolidations == null ? 1 : cachehandlarConsolidations.Length + 1];
                if (cachehandlarConsolidations != null)
                    cachehandlarConsolidations.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cachehandlarConsolidations = tmp;
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
        /// Find the consolidations
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.handlarConsolidations handlarConsolidations(bool myBool, double myDouble, int myInt, string myString)
        {
            return _indicator.handlarConsolidations(Input, myBool, myDouble, myInt, myString);
        }

        /// <summary>
        /// Find the consolidations
        /// </summary>
        /// <returns></returns>
        public Indicator.handlarConsolidations handlarConsolidations(Data.IDataSeries input, bool myBool, double myDouble, int myInt, string myString)
        {
            return _indicator.handlarConsolidations(input, myBool, myDouble, myInt, myString);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Find the consolidations
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.handlarConsolidations handlarConsolidations(bool myBool, double myDouble, int myInt, string myString)
        {
            return _indicator.handlarConsolidations(Input, myBool, myDouble, myInt, myString);
        }

        /// <summary>
        /// Find the consolidations
        /// </summary>
        /// <returns></returns>
        public Indicator.handlarConsolidations handlarConsolidations(Data.IDataSeries input, bool myBool, double myDouble, int myInt, string myString)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.handlarConsolidations(input, myBool, myDouble, myInt, myString);
        }
    }
}
#endregion
