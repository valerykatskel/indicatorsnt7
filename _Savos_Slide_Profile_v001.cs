#region Using declarations
 using System;
 using System.IO;
 using System.ComponentModel;
 using System.Diagnostics;
 using System.Drawing;
 using System.Drawing.Drawing2D;
 using System.Xml.Serialization;
 using NinjaTrader.Cbi;
 using NinjaTrader.Data;
 using NinjaTrader.Gui.Chart;
 using System.Collections;
 using System.Collections.Generic;
 using System.Linq;
 using System.Windows.Forms;
#endregion
	
 // This namespace holds all indicators and is required. Do not change it.
 namespace NinjaTrader.Indicator
 {     
     /// <summary>
     /// Скользящий индикатор ПОКов и ПРОФИЛЕЙ. Прижат к правому краю и рассчитывает профиль для заданного в настройках количества ПОСЛЕДНИХ баров
     /// </summary>
    [Description("Slide-Range Profile with VPOC and ValueArea")]
    public class _Savos_Slide_Profile_v001 : Indicator
    {

		#region VARIABLES
		//******************************************************************************************************************
		
		// === СЕКЦИЯ ДЕБАГА === //
		//---------------------------
		private bool debugPerformance 		= true;				// проверять производительность в РеКалк и в ПЛОТ?
		//---------------------------
		// === СЕКЦИЯ ДЕБАГА === //
		
		
		// === НАСТРОЙКИ, ДОСТУПНЫЕ ПОЛЬЗОВАТЕЛЮ В ПАНЕЛИ СВОЙСТВ ИНДИКАТОРА === //
		//---------------------------------------------------------------------------
		private int initProfileBars1		= 60;				// сколько последних баров учитывать при построении нашего скользящего профиля
		private int initProfileBars2		= 20;				// сколько последних баров учитывать при построении нашего скользящего профиля
		//private bool useStandartPlot		= false;			// по умолчанию - рисуем сами, а стандартный плот делаем прозрачным
		private bool useStandartPlot		= true;				// хотя нет - по умолчанию как раз пусть ниндзя за нас рисует!!!
		
		private string instrument			= "";				// торгуемый на данном графике инструмент

		private int P1_vpocLineSize			= 3;				// толщина ПОК-линии слева от профиля (который сам по себе справа графика)
		private	Color P1_vpocLineColor		= Color.Yellow; 	// цвет основной линии ПОК
		private DashStyle P1_vpocLineStyle 	= DashStyle.Solid;	// и стиль этой линии
		
		private int P2_vpocLineSize			= 1;				// толщина ПОК-линии слева от профиля (который сам по себе справа графика)
		private	Color P2_vpocLineColor		= Color.OrangeRed; 	// цвет основной линии ПОК
		private DashStyle P2_vpocLineStyle 	= DashStyle.Dash;	// и стиль этой линии
		//---------------------------------------------------------------------------
		// === НАСТРОЙКИ, ДОСТУПНЫЕ ПОЛЬЗОВАТЕЛЮ В ПАНЕЛИ СВОЙСТВ ИНДИКАТОРА === //
		
		// === СПИСКИ-ХРАНИЛИЩА-КОПИЛКИ === //
		//-------------------------------------
		// Рабочая лошадка №1... поуровневый (по цене) список ОБЪЕМОВ текущей свечки. 
		// На каждой новой свече основного таймфрейма он обнуляется и заполняется заново и заново и заново...
		private Dictionary<Int32, double> vol_price = new Dictionary<Int32, double>();	// в списке ЦЕНА будет не в баксах а в ТИКАХ!!!
		
		// Рабочая лошадка №2... поуровневый (по цене) список ОБЪЕМОВ перерасчитываемой свечки. 
		// Используется при циклических вычислениях по историческим данным
		private Dictionary<Int32, double> work_vol_price = new Dictionary<Int32, double>();	// в списке ЦЕНА будет не в баксах а в ТИКАХ!!!

		// для наших ДИАПАЗОННЫХ ПОКов пробуем такой вариант - по каждой свечке основного бара собираем СПИСОК объемов по ценовым уровням (как для ПОКа свечки)
		// НО! не обнуляем этот список на каждой новой свече, а сохраняем список в "списке списков"
		private Dictionary<Int32, Dictionary<Int32, double>> bar_vol_price = new Dictionary<Int32, Dictionary<Int32,double>>();	// в списке ЦЕНА будет не в баксах а в ТИКАХ!!!
		//--------------------------------------
		// === СПИСКИ-ХРАНИЛИЩА-КОПИЛКИ === //


		// СТАРШИЙ БРАТ
		private int		P1_profileBars;	// ширина профиля в барах
		private double	P1_VpocPrice;
		private double	P1_VpocVol;			
		private Dictionary<Int32, double> P1_vol_price;
		//private Dictionary<Int32, double> P1_bars_price;

		// МЛАДШИЙ БРАТ
		private int		P2_profileBars;	// ширина профиля в барах
		private double	P2_VpocPrice;
		private double	P2_VpocVol;			
		private Dictionary<Int32, double> P2_vol_price;
		//private Dictionary<Int32, double> P2_bars_price;
		
		// РАБОЧИЕ ПЕРЕМЕННЫЕ
		private int PlotSize1;
		private Color PlotColor1;
		private DashStyle PlotStyle1;
		private double curBar1;
		private int PlotSize2;
		private Color PlotColor2;
		private DashStyle PlotStyle2;
		private double curBar2;
		
		// === ВСЕ ОСТАЛЬНОЕ, НЕ НАСТОЛЬКО ВАЖНОЕ === //
		//------------------------------------------------
		private Pen myPen;
		// переменные для отслеживания сдвижек графика (масштабирование, скролл, изменени размеров окна и т.д.)
		private Rectangle prevBounds;
		private double prevMinPrice		= 0;
		private double prevMaxPrice		= 3000;
		private bool rangesChanged		= false;
		private bool chartChanged		= true;					// двигался ли чарт - сжимался, скроллировался и так далее...
		//------------------------------------------------
		// === ВСЕ ОСТАЛЬНОЕ, НЕ НАСТОЛЬКО ВАЖНОЕ === //

				
		//******************************************************************************************************************
		#endregion VARIABLES

		#region INIT
        protected override void Initialize()
        {
            Overlay = true;
			AutoScale = false;
            CalculateOnBarClose = false;
			
			Add(PeriodType.Tick,1);		// ВОТ ключевой ход - мы просто на график любого таймфрейма добавляем еще один - с периодом ОДИН ТИК 
										// и таким образом можем КАЖДЫЙ ТИК анализировать и собирать информацию в том числе и по истории!!!
										// Только пока не ясно - работает ли данный подход с ДЕЛЬТОЙ (то есть доступна ли нам информация о бидах и асках)???
						
			BarsRequired = 0;
			ClearOutputWindow();
			
			instrument = Instrument.MasterInstrument.Name;
			
			Add(new Plot(P1_vpocLineColor, "VPOC1"));
			Plots[0].Pen.DashStyle = P1_vpocLineStyle;
			Plots[0].Pen.Width = P1_vpocLineSize;
			
			Add(new Plot(P2_vpocLineColor, "VPOC2"));
			Plots[1].Pen.DashStyle = P2_vpocLineStyle;
			Plots[1].Pen.Width = P2_vpocLineSize;
			
		} // END of Initialize()
				
		protected override void OnStartUp()
		{	

			P1_vol_price	= new Dictionary<Int32, double>();
			//P1_bars_price	= new Dictionary<Int32, double>();
			P1_profileBars	= initProfileBars1;

			P2_vol_price	= new Dictionary<Int32, double>();
			//P2_bars_price	= new Dictionary<Int32, double>();
			P2_profileBars	= initProfileBars2;
			
			PlotSize1		= P1_vpocLineSize;
			PlotColor1		= P1_vpocLineColor;
			PlotStyle1		= P1_vpocLineStyle;

			PlotSize2		= P2_vpocLineSize;
			PlotColor2		= P2_vpocLineColor;
			PlotStyle2		= P2_vpocLineStyle;
			
			// а чем мы там, собственно, торгуем-то?
			instrument = Instrument.MasterInstrument.Name;
			
		} // END of OnStartUp()
		#endregion INIT
		
		#region ON_BAR_UPDATE
		protected override void OnBarUpdate()
        {
			

			// Cбор тиковых данных и распределение их по структурам для хранения
			#region MAKE_ALL_LADDERS
			/**/
			// ЕДИНИЦА обозначает, что это событие OnBarUpdate было вызвано не для свечки основного таймфрейма нашего графика
			// а для добавленного 1-тикового таймфрейма. Именно тут можно обрабатывать поступающие данные ПОТИКОВО!!!
			if (BarsInProgress==1) {
				if (!vol_price.ContainsKey(Convert.ToInt32(Closes[1][0]/TickSize))) {
					// Если в нашем "хранилище" еще нет соответствующей "полочки" или "ящика" (короче, нужной ячейки) - то мы ее добавляем!
					// Это значит, что на основном таймфрейме в нашей текущей свечке на этом ценовом уровне пока еще не было сделок и данный тик принес
					// первые данные, например, нужную нам информацию по объемам.
                	vol_price.Add(Convert.ToInt32(Closes[1][0]/TickSize),Volumes[1][0]);
				} else {
					// А если такая ячейка уже есть, значит мы к имеющимся там данным добавляем очередную порцию!
					// То есть накапливаем обьем на этом ценовом уровне в нашей свечке на основном таймфрейме.
                    vol_price[Convert.ToInt32(Closes[1][0]/TickSize)]+=Volumes[1][0];
				}
			}
			
			
			// НОЛЬ обозначает, что событие OnBarUpdate было вызвано как раз для основного таймфрейма - то есть именно свечка нашего графика
			// получила порцию изменений и тут можно с этим что-то делать. Ну что там мы обычно в этом событии и собираемся...
			if (BarsInProgress==0 && CurrentBars[0] > 10) {
				if(FirstTickOfBar) {
					// Ну тут все понятно - это ПЕРВЫЙ тик свечки на ОСНОВНОМ таймфрейме, то есть самое начало нашей свечи
					int prevBar = CurrentBars[0] - 1; // Это номер только что закрывшейся свечки, данные по которой мы сейчас как раз и обрабатываем
						
					//------------
					#region BAR_VOL-Ladder_PREV
					//------------
					// Создаем СПИСОК БАРОВ, в каждой записи которого будут СПИСКИ ОБЪЕМОВ на всех ценовых уровнях бара
					// То есть - пробуем вложить список в список
					if(!bar_vol_price.ContainsKey(prevBar)) {
						// Если в нашем списке еще нет записи по предыдущей свечке - создаем ее
						bar_vol_price.Add(prevBar,new Dictionary<Int32, double>());
					} else {
						// Если же эта запись есть (чего вообще-то быть не должно!!!) - очищаем ее
						//bar_vol_price[prevBar].Clear();
					}
					// Теперь у нас нужная запись есть и она чистая - можно ее заполнять
					foreach(KeyValuePair<int, double> kvp in vol_price.OrderByDescending(key => key.Value))
					{
						// В цикле проходим по всем записям обьемов и цен, накопленных при обработке тиковых данных
						if(!bar_vol_price[prevBar].ContainsKey(kvp.Key)) {
							// На всякий случай не просто добавляем новую "запись в записи", а предварительно убеждаемся, что ее нет и добавление не вызовет ошибку
							bar_vol_price[prevBar].Add(kvp.Key, kvp.Value);
						} else {
							// Если же эта запись есть - чего опять-таки не должно быть - перезаписываем ее содержимое
							bar_vol_price[prevBar][kvp.Key] = kvp.Value;
						}
					}
					//Print("VolPrice Ladder's Count = " + vol_price.Count);
					//Print("Bar " + prevBar + " Ladder's Count = " + bar_vol_price[prevBar].Count);
					#endregion BAR_VOL-Ladder_PREV
					
					//------------
					// И вот ТОЛЬКО ТЕПЕРЬ после сохранения списка по ПРЕДУДУЩЕЙ свечке в нашем "списке списков" - теперь можно этот рабочий список обнулить
					// для того, чтобы в нем начала накапливаться новая история - уже по текущей, только что открывшейся свечке основного таймфрейма на нашем графике
					vol_price.Clear();
				} else {
					// Это НЕ первый тик, то есть мы уже ранее обработали все ПРЕДЫДУЩИЕ свечки, а сейчас имеем дело с ТЕКУЩЕЙ постоянно меняющейся свечкой
					int curBar = CurrentBars[0];
					//Print("CurBarNum = " + curBar);
					
					//------------
					#region BAR_VOL-Ladder_CUR
					//------------
					// Постоянно обновляем в нашем СПИСКЕ БАРОВ данные о текущей свечке
					if(!bar_vol_price.ContainsKey(curBar)) {
						// Если в нашем списке еще нет записи по текущей свечке - создаем ее
						bar_vol_price.Add(curBar,new Dictionary<Int32, double>());
					} else {
						// Если же эта запись есть очищаем для заполнения обновленными данными
						bar_vol_price[curBar].Clear();
					}
					// Теперь у нас нужная запись есть и она чистая - можно ее заполнять
					foreach(KeyValuePair<int, double> kvp in vol_price.OrderByDescending(key => key.Value))
					{
						// В цикле проходим по всем записям обьемов и цен, накопленных при обработке тиковых данных
						if(!bar_vol_price[curBar].ContainsKey(kvp.Key)) {
							// На всякий случай не просто добавляем новую "запись в записи", а предварительно убеждаемся, что ее нет и добавление не вызовет ошибку
							bar_vol_price[curBar].Add(kvp.Key, kvp.Value);
						} else {
							// Если же эта запись есть - чего опять-таки не должно быть - перезаписываем ее содержимое
							bar_vol_price[curBar][kvp.Key] = kvp.Value;
						}
					}
					#endregion BAR_VOL-Ladder_CUR
				}
			}
			/**/
			#endregion MAKE_ALL_LADDERS
			
			// То, что нужно для PLOT'a
			
			#region TO_PLOT_BIG
			//----------------
			if(FirstTickOfBar && (CurrentBars[0] > 20 + initProfileBars1) && (BarsInProgress == 0))
			{
				//Print("===============================");
				// На исторической части графика надо вычислять ПОК последних initProfileBars баров вручную - если мы хотим рисовать PLOT конечно!!!
				try {
					P1_vol_price.Clear();
					
					for(int i = CurrentBars[0] - initProfileBars1 + 1; i <= CurrentBars[0]; i++)
					{
						//достанем из "списка списков" нужный нам список-свечку
						//Print("Bar num: " + i);
						if(!bar_vol_price.ContainsKey(i)) {
							// Если по проверяемому бару нет структуры цена-объем, пропускаем его
							//Print("   Нет данных");
						} else {
							// Если же нужные данные есть - организуем внутренний цикл и суммируем все объемы ПО ОЧЕРЕДНОМУ БАРУ-СВЕЧКЕ
							//Print("   Есть данные");
							foreach(KeyValuePair<Int32, double> kvp in bar_vol_price[i])
							{
								//Print("   Key:" + kvp.Key + " || Value: " + kvp.Value);
								// У нас в R.work_vol_price теперь находятся данные по всем объемно-ценовым уровням ПРОВЕРЯЕМОЙ СВЕЧКИ
								if (!P1_vol_price.ContainsKey(kvp.Key)) {
									// Если в нашем "хранилище" еще нет соответствующей "полочки" или "ящика" (короче, нужной ячейки) - то мы ее добавляем!
									P1_vol_price.Add(kvp.Key,kvp.Value);
								} else {
									// А если такая ячейка уже есть, значит мы ее добавили на другом баре и теперь вновь к ней подошли - добавляем (накапливаем) объемы
									P1_vol_price[kvp.Key] += kvp.Value;
								}
							}
						}
					}
					// Получили "ладдер" для нашего диапазона - общий поценовой список объемов, без разделения на бары
					foreach(KeyValuePair<Int32, double> kvp in P1_vol_price.OrderByDescending(key => key.Value))
					{
						// перебираем этот список от самого мелкого объема до самого крупного - таким образом находим ПОК!
						P1_VpocPrice	= kvp.Key * TickSize;
						break;	//Весь цикл, собственно, затевался только ради сортировки, так что получив первые же данные (самые максимальные) мы цикл прерываем!!!
					}
					// Сохраняем для возможности вывода PLOT'a
					VPOC1[0] = P1_VpocPrice;
					
					if(!useStandartPlot)
					{
						curBar1 = CurrentBars[0];
						if(VPOC1[0] == VPOC1[1])
						{
							// Если значение НЕ менялось - просто рисуем линию
							DrawLine("Plot10"+curBar1, false, 1, VPOC1[0], 0, VPOC1[0], PlotColor1, PlotStyle1, PlotSize1);
						} else {
							// Если значение МЕНЯЛОСЬ - придется рисовать ДВЕ линии и ЛУЧ
							DrawLine("Plot10"+curBar1, false, 1, VPOC1[1], 0, VPOC1[1], PlotColor1, PlotStyle1, PlotSize1);
							DrawLine("Plot11"+curBar1, false, 0, VPOC1[1], 0, VPOC1[0], PlotColor1, PlotStyle1, PlotSize1);
						}
						// и нарисуем ЛУЧ, уходящий вправо "за горизонт"
						DrawRay("Plot1", false, 1, VPOC1[1], 0, VPOC1[1], PlotColor1, PlotStyle1, PlotSize1);
					}
				} catch {}
			}
			//----------------
			#endregion TO_PLOT_BIG

			#region TO_PLOT_SMALL
			//----------------
			if(FirstTickOfBar && CurrentBars[0] > 20 + initProfileBars2 && BarsInProgress == 0)
			{
				//Print("===============================");
				// На исторической части графика надо вычислять ПОК последних initProfileBars баров вручную - если мы хотим рисовать PLOT конечно!!!
				try {
					P2_vol_price.Clear();
					
					for(int i = CurrentBars[0] - initProfileBars2 + 1; i <= CurrentBars[0]; i++)
					{
						//достанем из "списка списков" нужный нам список-свечку
						if(!bar_vol_price.ContainsKey(i)) {
							// Если по проверяемому бару нет структуры цена-объем, пропускаем его
						} else {
							// Если же нужные данные есть - организуем внутренний цикл и суммируем все объемы ПО ОЧЕРЕДНОМУ БАРУ-СВЕЧКЕ
							foreach(KeyValuePair<Int32, double> kvp in bar_vol_price[i])
							{
								// У нас в R.work_vol_price теперь находятся данные по всем объемно-ценовым уровням ПРОВЕРЯЕМОЙ СВЕЧКИ
								if (!P2_vol_price.ContainsKey(kvp.Key)) {
									// Если в нашем "хранилище" еще нет соответствующей "полочки" или "ящика" (короче, нужной ячейки) - то мы ее добавляем!
									P2_vol_price.Add(kvp.Key,kvp.Value);
								} else {
									// А если такая ячейка уже есть, значит мы ее добавили на другом баре и теперь вновь к ней подошли - добавляем (накапливаем) объемы
									P2_vol_price[kvp.Key] += kvp.Value;
								}
							}
						}
					}
					// Получили "ладдер" для нашего диапазона - общий поценовой список объемов, без разделения на бары
					foreach(KeyValuePair<Int32, double> kvp in P2_vol_price.OrderByDescending(key => key.Value))
					{
						// перебираем этот список от самого мелкого объема до самого крупного - таким образом находим ПОК!
						P2_VpocPrice	= kvp.Key * TickSize;
						break;	//Весь цикл, собственно, затевался только ради сортировки, так что получив первые же данные (самые максимальные) мы цикл прерываем!!!
					}
					// Сохраняем для возможности вывода PLOT'a
					VPOC2[0] = P2_VpocPrice;
					
					if(!useStandartPlot)
					{
						curBar2 = CurrentBars[0];
						if(VPOC2[0] == VPOC2[1])
						{
							// Если значение НЕ менялось - просто рисуем линию
							DrawLine("Plot20"+curBar2, false, 1, VPOC2[0], 0, VPOC2[0], PlotColor2, PlotStyle2, PlotSize2); 
						} else {
							// Если значение МЕНЯЛОСЬ - придется рисовать ДВЕ линии
							DrawLine("Plot20"+curBar2, false, 1, VPOC2[1], 0, VPOC2[1], PlotColor2, PlotStyle2, PlotSize2);
							DrawLine("Plot21"+curBar2, false, 0, VPOC2[1], 0, VPOC2[0], PlotColor2, PlotStyle2, PlotSize2);
						}
						// и нарисуем ЛУЧ, уходящий вправо "за горизонт"
						DrawRay("Plot2", false, 1, VPOC2[1], 0, VPOC2[1], PlotColor2, PlotStyle2, PlotSize2);
					}
				} catch {}
			}
			//----------------
			#endregion TO_PLOT_SMALL
			
        }
		#endregion ON_BAR_UPDATE
		
		#region PLOT
		public override void Plot(Graphics graphics, Rectangle bounds, double min, double max)	
		{	
			// Отрисовываем базовые вещи, только сам индикатор делаем прозрачным в зависимости от настроек
			if(!useStandartPlot)
			{
				Plots[0].Pen.Color = Color.Transparent;
				Plots[1].Pen.Color = Color.Transparent;
			} else {
				// заполним пространство МЕЖДУ двумя ПЛОТами
				//Plots[0].Pen.Color = Color.Transparent;
				//Plots[1].Pen.Color = Color.Transparent;
				//DrawRegion("reg"+CurrentBars[0].ToString(), CurrentBars[0]-1,0,VPOC1,VPOC2,Color.Yellow,Color.Yellow,1);
				DrawRegion("reg", CurrentBars[0]-1, 0, VPOC1, VPOC2, Color.Transparent,PlotColor1,1);

				// нарисуем ЛУЧИ, уходящие вправо "за горизонт"
				DrawRay("Plot1", false, 0, VPOC1[0], -10, VPOC1[0], PlotColor1, PlotStyle1, PlotSize1);
				DrawRay("Plot2", false, 0, VPOC2[0], -10, VPOC2[0], PlotColor2, PlotStyle2, PlotSize2);
			}
			base.Plot(graphics,bounds,min,max);	
			
		} // END of Plot()
		//------------
		#endregion PLOT

		#region OnTermination
		protected  override void OnTermination()
        {
			vol_price.Clear();
			work_vol_price.Clear();
			bar_vol_price.Clear();
			P1_vol_price.Clear();
			//P1_bars_price.Clear();
			P2_vol_price.Clear();
			//P2_bars_price.Clear();
		}
		#endregion		
		
        #region PROPERTIES
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public DataSeries VPOC1
		{
			get { return Values[0]; }
		}
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public DataSeries VPOC2
		{
			get { return Values[1]; }
		}
		//[XmlIgnore()]
		[Description("Использовать ли стандартный вывод PLOT? Если нет - будет рисоваться в ручном режиме")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("00. Standart Plot Method")]
		public bool UseStandartPlot
		{
			get { return useStandartPlot; }
			set { useStandartPlot = value; 
			} 	
		}

		[Description("Глубина (количество баров) диапазона для Профиля и ПОКа большого размера")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("01. Profile Bars - Big")]
		public int MyProfileBars1
		{
			get { return initProfileBars1; }
			set { initProfileBars1 = Math.Max(3, value); 
			} 	
		}
		//[XmlIgnore()]
		[Description("Глубина (количество баров) диапазона для Профиля и ПОКа маленького размера")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("02. Profile Bars - Small")]
		public int MyProfileBars2
		{
			get { return initProfileBars2; }
			set { initProfileBars2 = Math.Max(3, value); 
			} 	
		}
		

// LINES //
		
		//[XmlIgnore()]
        [Description("Цвет линии VPOC большого размера")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("01.1. VPOC Line Color - Big")]
        public Color VpocLineColorP1
        {
            get { return P1_vpocLineColor; }
            set { P1_vpocLineColor = value; }
        }
		[Browsable(false)]
		public string VpocLineColorP1Serialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(VpocLineColorP1); }
			set { VpocLineColorP1 = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
        [Description("Тип линии VPOC большого размера")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("01.2. VPOC Line Style - Big")]
        public DashStyle VpocLineStyleP1
        {
            get { return P1_vpocLineStyle; }
            set { P1_vpocLineStyle = value; }
        }
        [Description("Толщина линии VPOC большого размера")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("01.3. VPOC Line Width - Big")]
        public int VpocLineSizeP1
        {
            get { return P1_vpocLineSize; }
            set { P1_vpocLineSize = value; }
        }
        [Description("Цвет линии VPOC маленького размера")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("02.1. VPOC Line Color - Small")]
        public Color VpocLineColorP2
        {
            get { return P2_vpocLineColor; }
            set { P2_vpocLineColor = value; }
        }
		[Browsable(false)]
		public string VpocLineColorP2Serialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(VpocLineColorP2); }
			set { VpocLineColorP2 = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
        [Description("Тип линии VPOC маленького размера")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("02.2. VPOC Line Style - Small")]
         public DashStyle VpocLineStyleP2
        {
            get { return P2_vpocLineStyle; }
            set { P2_vpocLineStyle = value; }
        }
        [Description("Толщина линии VPOC маленького размера")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("02.3. VPOC Line Width - Small")]
        public int VpocLineSizeP2
        {
            get { return P2_vpocLineSize; }
            set { P2_vpocLineSize = value; }
        }
		

		#endregion PROPERTIES
		
    
	}
 }

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private _Savos_Slide_Profile_v001[] cache_Savos_Slide_Profile_v001 = null;

        private static _Savos_Slide_Profile_v001 check_Savos_Slide_Profile_v001 = new _Savos_Slide_Profile_v001();

        /// <summary>
        /// Slide-Range Profile with VPOC and ValueArea
        /// </summary>
        /// <returns></returns>
        public _Savos_Slide_Profile_v001 _Savos_Slide_Profile_v001(int myProfileBars1, int myProfileBars2, bool useStandartPlot)
        {
            return _Savos_Slide_Profile_v001(Input, myProfileBars1, myProfileBars2, useStandartPlot);
        }

        /// <summary>
        /// Slide-Range Profile with VPOC and ValueArea
        /// </summary>
        /// <returns></returns>
        public _Savos_Slide_Profile_v001 _Savos_Slide_Profile_v001(Data.IDataSeries input, int myProfileBars1, int myProfileBars2, bool useStandartPlot)
        {
            if (cache_Savos_Slide_Profile_v001 != null)
                for (int idx = 0; idx < cache_Savos_Slide_Profile_v001.Length; idx++)
                    if (cache_Savos_Slide_Profile_v001[idx].MyProfileBars1 == myProfileBars1 && cache_Savos_Slide_Profile_v001[idx].MyProfileBars2 == myProfileBars2 && cache_Savos_Slide_Profile_v001[idx].UseStandartPlot == useStandartPlot && cache_Savos_Slide_Profile_v001[idx].EqualsInput(input))
                        return cache_Savos_Slide_Profile_v001[idx];

            lock (check_Savos_Slide_Profile_v001)
            {
                check_Savos_Slide_Profile_v001.MyProfileBars1 = myProfileBars1;
                myProfileBars1 = check_Savos_Slide_Profile_v001.MyProfileBars1;
                check_Savos_Slide_Profile_v001.MyProfileBars2 = myProfileBars2;
                myProfileBars2 = check_Savos_Slide_Profile_v001.MyProfileBars2;
                check_Savos_Slide_Profile_v001.UseStandartPlot = useStandartPlot;
                useStandartPlot = check_Savos_Slide_Profile_v001.UseStandartPlot;

                if (cache_Savos_Slide_Profile_v001 != null)
                    for (int idx = 0; idx < cache_Savos_Slide_Profile_v001.Length; idx++)
                        if (cache_Savos_Slide_Profile_v001[idx].MyProfileBars1 == myProfileBars1 && cache_Savos_Slide_Profile_v001[idx].MyProfileBars2 == myProfileBars2 && cache_Savos_Slide_Profile_v001[idx].UseStandartPlot == useStandartPlot && cache_Savos_Slide_Profile_v001[idx].EqualsInput(input))
                            return cache_Savos_Slide_Profile_v001[idx];

                _Savos_Slide_Profile_v001 indicator = new _Savos_Slide_Profile_v001();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.MyProfileBars1 = myProfileBars1;
                indicator.MyProfileBars2 = myProfileBars2;
                indicator.UseStandartPlot = useStandartPlot;
                Indicators.Add(indicator);
                indicator.SetUp();

                _Savos_Slide_Profile_v001[] tmp = new _Savos_Slide_Profile_v001[cache_Savos_Slide_Profile_v001 == null ? 1 : cache_Savos_Slide_Profile_v001.Length + 1];
                if (cache_Savos_Slide_Profile_v001 != null)
                    cache_Savos_Slide_Profile_v001.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cache_Savos_Slide_Profile_v001 = tmp;
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
        /// Slide-Range Profile with VPOC and ValueArea
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator._Savos_Slide_Profile_v001 _Savos_Slide_Profile_v001(int myProfileBars1, int myProfileBars2, bool useStandartPlot)
        {
            return _indicator._Savos_Slide_Profile_v001(Input, myProfileBars1, myProfileBars2, useStandartPlot);
        }

        /// <summary>
        /// Slide-Range Profile with VPOC and ValueArea
        /// </summary>
        /// <returns></returns>
        public Indicator._Savos_Slide_Profile_v001 _Savos_Slide_Profile_v001(Data.IDataSeries input, int myProfileBars1, int myProfileBars2, bool useStandartPlot)
        {
            return _indicator._Savos_Slide_Profile_v001(input, myProfileBars1, myProfileBars2, useStandartPlot);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Slide-Range Profile with VPOC and ValueArea
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator._Savos_Slide_Profile_v001 _Savos_Slide_Profile_v001(int myProfileBars1, int myProfileBars2, bool useStandartPlot)
        {
            return _indicator._Savos_Slide_Profile_v001(Input, myProfileBars1, myProfileBars2, useStandartPlot);
        }

        /// <summary>
        /// Slide-Range Profile with VPOC and ValueArea
        /// </summary>
        /// <returns></returns>
        public Indicator._Savos_Slide_Profile_v001 _Savos_Slide_Profile_v001(Data.IDataSeries input, int myProfileBars1, int myProfileBars2, bool useStandartPlot)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator._Savos_Slide_Profile_v001(input, myProfileBars1, myProfileBars2, useStandartPlot);
        }
    }
}
#endregion
