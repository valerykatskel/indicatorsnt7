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
	/// 326 - 24 - 17-45
	/// 332 - 18 - 18-15
	/// 338 - 12 - 18-45
	/// 344 - 6 - 19-15
	/// 
	/// 	
	/// 
    /// Натягиваемый и передвигаемый по графику 4-диапазонный индикатор ПОКов и ПРОФИЛЕЙ. Почему всего 4? Мне хватает...
	/// 
	/// Обзорное видео по возможностям 					- https://youtu.be/sQHUWKi71Lc
	/// Видео со стресс-тестом производительности 		- https://youtu.be/wETPwWeU1w0
	/// 
	/// Распространяется БЕСПЛАТНО и на условиях "КАК ЕСТЬ" - но просьба не удалять эту информацию:
	/// Copyright by SavosRU aka "дядя Савос" 2016. All questions - to savos@bk.ru
	/// </summary>
    [Description("Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!")]
    public class _Savos_Range_Profile_v002 : Indicator
    {
		#region VARIABLES
		//******************************************************************************************************************
		
		// === СЕКЦИЯ ДЕБАГА === //
		//---------------------------
		private bool debugPositions 		= true;				// выводить про текущий профиль информацию в отдельное окно или нет?
		private bool debugPerformance 		= true;				// проверять производительность в РеКалк и в ПЛОТ?
		private bool printOnce				= false;			// печатать при первом проходе
		//---------------------------
		// === СЕКЦИЯ ДЕБАГА === //
		
		
		// === СЕКЦИЯ ГОРЯЧИХ КЛАВИШ === //
		//-----------------------------------
		private bool showMouseCursorChanges = true;				// SHIFT+"M" 	-> показывать или прятать изменения курсора при движении мышки на разных частях диапазона (тянуть, расширять, закрывать)
		private bool showProfile			= true;				// SHIFT+"P" 	-> показывать или прятать ПРОФИЛЬ?
		private bool showRange				= true;				// SHIFT+"R" 	-> показывать или прятать сам ДИАПАЗОН?
		private bool showHelpScreen			= false;			// SHIFT+"?" 	-> показывать или прятать HELP-скрин
		private bool showExtProfile			= false;			// SHIFT+">""<" -> включать или отключать расширенный режим  для ПРОФИЛЯ? увеличивать и уменьшать эту ширину
		private bool showRectangles			= true;				// SHIFT+"L"	-> показывать ли прямоугольники ЛИНИЯМИ?
		private int curSelectedRange		= 0;				// "CTRL+TAB" и "BACKSPACE" - выбор текущего активного профиля по кругу в ту и другую сторону
		private bool showDebugScreen		= false;			// SHIFT+"D" 	-> показывать или прятать DEBUG-скрин
		//-----------------------------------
		// === СЕКЦИЯ ГОРЯЧИХ КЛАВИШ === //
		
		
		// === НАСТРОЙКИ, ДОСТУПНЫЕ ПОЛЬЗОВАТЕЛЮ В ПАНЕЛИ СВОЙСТВ ИНДИКАТОРА === //
		//---------------------------------------------------------------------------
		private int initWidth 				= 5;				// сколько баров шириной будет диапазон профиля при создании
		private int extProfile				= 3;				// во сколько раз увеличить ширину профиля (не меняя диапазона) для расширенного режима
		private bool showSummary			= true;				// выводить ли ПОД диапазонами сборную информацию (количество баров, объем всего диапазона, объем ПОКа
		private	Color summaryColor			= Color.White;		// цвет этой надписи со сборной информацией
		private int summaryFontSize 		= 6;				// размер шрифта для этой надписи
		private bool showPrice				= true;				// выводить ли СЛЕВА от диапазонов цену ПОКа
		private	Color priceColor			= Color.Yellow; 	// цвет этой надписи с ценой
		private int priceFontSize			= 8;				// размер шрифта для этой надписи
		private	Color borderColor			= Color.Red;		// цвет рамки вокруг диапазонов
		private bool useTrendColor			= true;				// рисовать ли ПОКи на Лонг и Шорт разными цветами???
		private int histogramLines			= 10000;			// максимальное возможное количество уровней в профиле диапазона
		private int curCreatedRange			= 0;				// текущий создаваемый диапазон - ходит по кругу от 1 до 4
		private bool useNewPatternAlert		= false;			// проигрывать ли звуковой сигнал при начале новой свечки???
		private int nowRanges				= 0;				// количество реально задействованных сейчас выделенных диапазонов
		private string instrument			= "";				// торгуемый на данном графике инструмент
		private int vpocLineSise			= 2;				// толщина ПОК-линии внутри диапазона
		private	Color vpocLineColor			= Color.Yellow; 	// цвет основной линии ПОК
		private DashStyle vpocLineStyle 	= DashStyle.Solid;	// и стиль этой линии
		private int vpocExtLongSise			= 2;				// толщина ПОК-линии снаружи диапазона - расширенной ПОК-линии на ЛОНГ
		private	Color vpocExtLongColor		= Color.Green;		// цвет расширенной ПОК-линии в Лонг
		private DashStyle vpocExtLongStyle	= DashStyle.Solid;	// и стиль этой линии
		private int vpocExtShortSise		= 2;				// толщина ПОК-линии снаружи диапазона - расширенной ПОК-линии на ШОРТ
		private	Color vpocExtShortColor		= Color.Red;		// цвет расширенной ПОК-линии в Лонг
		private DashStyle vpocExtShortStyle	= DashStyle.Solid;	// и стиль этой линии
		private Color rangeColor1			= Color.Red;		// базовый увет фона для диапазона №1
		private Color rangeColor2			= Color.Yellow;		// базовый увет фона для диапазона №2
		private Color rangeColor3			= Color.Green;		// базовый увет фона для диапазона №3
		private Color rangeColor4			= Color.Blue;		// базовый увет фона для диапазона №4
		private Color vpocProfileColor		= Color.Gray;		// цвет гистограммы ПРОФИЛЯ диапазона
		private Color helpFontColor			= Color.White;		// цвет текста ХЕЛП-СКРИНА
		//---------------------------------------------------------------------------
		// === НАСТРОЙКИ, ДОСТУПНЫЕ ПОЛЬЗОВАТЕЛЮ В ПАНЕЛИ СВОЙСТВ ИНДИКАТОРА === //
		
		
		// === СПИСКИ-ХРАНИЛИЩА-КОПИЛКИ === //
		//-------------------------------------
		// Рабочая лошадка №1... поуровневый (по цене) список ОБЪЕМОВ текущей свечки. 
		// На каждой новой свече основного таймфрейма он обнуляется и заполняется заново и заново и заново...
        private Dictionary<double, double> vol_price = new Dictionary<double, double>();
		
		// Рабочая лошадка №2... поуровневый (по цене) список ОБЪЕМОВ перерасчитываемой свечки. 
		// Используется при циклических вычислениях по историческим данным
		private Dictionary<double, double> work_vol_price = new Dictionary<double, double>();

		// для наших ДИАПАЗОННЫХ ПОКов пробуем такой вариант - по каждой свечке основного бара собираем СПИСОК объемов по ценовым уровням (как для ПОКа свечки)
		// НО! не обнуляем этот список на каждой новой свече, а сохраняем список в "списке списков"
		// ВНИМАНИЕТ! не работаем со СТРОКАМИ в списках - ЖУТКИЕ ТОРМОЗА!!! Берем Dictionary of the Dictionary!!!
		private Dictionary<double, Dictionary<double, double>> bar_vol_price = new Dictionary<double, Dictionary<double,double>>();

		// Это список ВСЕХ ОБЪЕМНЫХ МАКСИМУМОВ (то есть V-ПОКов), по одной записи на каждую свечку основного таймфрейма.
       	private Dictionary<Int32, double> vPoc_list = new Dictionary<Int32, double>();
		//--------------------------------------
		// === СПИСКИ-ХРАНИЛИЩА-КОПИЛКИ === //

		
		// === ОСНОВНАЯ СТРУКТУРА === //
		//--------------------------------
		private struct myRangeProfileType{
			public int		id;
			public int		_Xl;
			public int		_Xr;
			public int		_Yt;
			public int		_Yb;
			
			public Color	rangeColor;
			
			public double	Top;
			public double	Bottom;
			
			public int		leftBar;
			public int		leftOffset;
			public DateTime leftTime;
			
			public int		rightBar;
			public int		rightOffset;
			public DateTime rightTime;
			
			public int		rangeWidth;
			public double	rangeVol;
			public int		rangeBars;
			
			public int		profileWidth;
			
			
			public double	VpocPrice;
			public double	VpocVol;
			public int		VpocType;
			
			public bool		extVpocWork;
			public int		extVpocBar;
			public int		extVpocOffset;
			public DateTime extVpocTime;
			
			public bool		isReady;
			public bool		isPlotted;
			public bool		isExtended;
			
			public Dictionary<double, double> vol_price;
			public Dictionary<double, double> work_vol_price;
			public Dictionary<double, double> bars_price;	
		}		
		private myRangeProfileType[] R;
		//--------------------------------
		// === ОСНОВНАЯ СТРУКТУРА === //

		
		// === СОБЫТИЯ МЫШКИ === //
		//--------------------------
		// именно сами события
		private MouseEventHandler mouseDownH;
		private MouseEventHandler mouseUpH;
		private MouseEventHandler mouseMoveH;
		private MouseEventHandler mouseDoubleClickH;

		// варианты возможных СТАТУСОВ для текущих действий с мышкой по тому или иному событию
		enum StatusType{NotMoving,SelectRange1,MovingRange1,ChangeRange1,CloseRange1,SelectRange2,MovingRange2,ChangeRange2,CloseRange2,SelectRange3,MovingRange3,ChangeRange3,CloseRange3,SelectRange4,MovingRange4,ChangeRange4,CloseRange4};
		StatusType status=StatusType.NotMoving;

		// используются при пересчете координат мышки в ценовую шкалу - НЕ УДАЛЯТЬ!!!
		private double min=Double.MaxValue,max=Double.MinValue;	
		delegate int Function(int x);
		
		private int cursorX			= 0;						// текущий бар под курсором мышки
		private double cursorY		= 0;						// текущая цена под курсором мышки
		//--------------------------
		// === СОБЫТИЯ МЫШКИ === //
		
		
		
		// === СОБЫТИЯ КЛАВИАТУРЫ === //
		//-------------------------------
		private KeyEventHandler keyEvtH;
		//-------------------------------
		// === СОБЫТИЯ КЛАВИАТУРЫ === //
		
		
		// === ВСЕ ОСТАЛЬНОЕ, НЕ НАСТОЛЬКО ВАЖНОЕ === //
		//------------------------------------------------
		// переменные для отслеживания сдвижек графика (масштабирование, скролл, изменени размеров окна и т.д.)
		private Rectangle prevBounds;
		private double prevMinPrice		= 0;
		private double prevMaxPrice		= 3000;
		private bool rangesChanged		= false;
		private bool chartChanged		= true;					// двигался ли чарт - сжимался, скроллировался и так далее...
		//------------------------------------------------
		// === ВСЕ ОСТАЛЬНОЕ, НЕ НАСТОЛЬКО ВАЖНОЕ === //

		
		
		//=========================================================               =====================================================
		//========================================================= ВСТАВКА-БОНУС =====================================================
		//=========================================================               =====================================================
		
		// Переменные для записи-чтения данных о профилях - НЕ МЕНЯТЬ!!!
		public string studioFolderName		= "";
		public string projectFolderName		= "";
		public string d						= "_";				// разделитель - delimiter			
		public string myFileName			= "";
		public bool readOnce				= false;			// читаем настройки один раз - при загрузке! не забываем там изменить значение на true!
		
		// Переменные для встроенного внутреннего ПОКа баров
		// === СЕКЦИЯ ГОРЯЧИХ КЛАВИШ === //
		//----------------------------------
		public double vol					= 0;				// рабочая переменная
		public bool showAllVPOCs			= false;			// SHIFT+"A" 	-> показывать или прятать ВСЕ ПОКи баров
		public bool showNakedVPOCs			= false;			// SHIFT+"N" 	-> показывать или прятать этот паттерн
		//----------------------------------
		// === СЕКЦИЯ ГОРЯЧИХ КЛАВИШ === //
		
		// === "КОПИЛКИ" ДЛЯ ПАТТЕРНОВ === //
		//-------------------------------------
		private Dictionary<Int32, double> nakedVPOC_list 		= new Dictionary<Int32, double>();
		// сюда так же добавляется "копилка" для любых других паттернов
		//-------------------------------------
		// === "КОПИЛКИ" ДЛЯ ПАТТЕРНОВ === //
		
		// и остальное...
		private Color myVPOCsColor			= Color.White;		// а сюда будем подставлять тот или иной НУЖНЫЙ цвет
		
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
			
			Add(new Line(new Pen(Color.Yellow, 3), 0, "VPOCline"));
			Add(new Line(new Pen(Color.Green, 3), 0, "ExtLongVPOC"));
			Add(new Line(new Pen(Color.Red, 3), 0, "ExtShortVPOC"));
			Add(new Line(new Pen(Color.Red, 2), 0, "Borders"));			
			
			Add(new Plot(Color.Transparent, "TEST"));
			BarsRequired = 0;
			ClearOutputWindow();
			
			instrument = Instrument.MasterInstrument.Name;
			
		} // END of Initialize()
				
		protected override void OnStartUp()
		{	

						
			// Присваиваем внутренним переменным те значения, которые получены во внешние переменные из настроек индикатора...
			// Такой финт тут просто необходим, в других индикаторах сразу используют внешние переменные, но не в этом...
			vpocLineColor		= Lines[0].Pen.Color;
			vpocExtLongColor	= Lines[1].Pen.Color;
			vpocExtShortColor	= Lines[2].Pen.Color;
			vpocLineSise		= Convert.ToInt16(Lines[0].Pen.Width);
			vpocExtLongSise		= Convert.ToInt16(Lines[1].Pen.Width);
			vpocExtShortSise	= Convert.ToInt16(Lines[2].Pen.Width);
			vpocLineStyle		= Lines[0].Pen.DashStyle;
			vpocExtLongStyle	= Lines[1].Pen.DashStyle;
			vpocExtShortStyle	= Lines[2].Pen.DashStyle;
			
			borderColor			= Lines[3].Pen.Color;

			// Инициализируем основной массив для хранения данных			
			R = new  myRangeProfileType[5];	// чтобы не путаться с нулем - начнем с индекса номер 1 и каждому из четырех диапазонов будет соответствовать ПРАВИЛЬНЫЙ номер
											// но для этого надо маасив создать на ПЯТЬ ячеек
			// Создаем в нужных ячейках этого массива наши будущие внутренние "ладдеры" диапазонов
			R[0].vol_price = new Dictionary<double, double>();
			R[1].vol_price = new Dictionary<double, double>();
			R[2].vol_price = new Dictionary<double, double>();
			R[3].vol_price = new Dictionary<double, double>();
			R[4].vol_price = new Dictionary<double, double>();
			
			// И не забываем про такой же "ладдер" - "рабочую лошадку"
			R[0].work_vol_price = new Dictionary<double, double>();
			R[1].work_vol_price = new Dictionary<double, double>();
			R[2].work_vol_price = new Dictionary<double, double>();
			R[3].work_vol_price = new Dictionary<double, double>();
			R[4].work_vol_price = new Dictionary<double, double>();

			// И также не забываем про такой же "ладдер" - для профиля
			R[0].bars_price = new Dictionary<double, double>();
			R[1].bars_price = new Dictionary<double, double>();
			R[2].bars_price = new Dictionary<double, double>();
			R[3].bars_price = new Dictionary<double, double>();
			R[4].bars_price = new Dictionary<double, double>();

			// и сразу ВКЛЮЧАЕМ для всех работу расширенной за границы диапазона линии ПОКа
			// ПО УМОЛЧАНИЮ ВСЕГДА РАБОТАЕТ!!!
			R[0].extVpocWork = true;
			R[1].extVpocWork = true;
			R[2].extVpocWork = true;
			R[3].extVpocWork = true;
			R[4].extVpocWork = true;
			// Теперь займемся перехватом событий от мышки
			mouseDownH=new MouseEventHandler(this.chart_MouseDown);
			mouseUpH=new MouseEventHandler(this.chart_MouseUp);
			mouseMoveH=new MouseEventHandler(this.chart_MouseMove);
			
			this.ChartControl.ChartPanel.MouseDown += mouseDownH;
			this.ChartControl.ChartPanel.MouseUp += mouseUpH;
			this.ChartControl.ChartPanel.MouseMove += mouseMoveH;
			
			// И соорудим перехватчик для горячих клавиш
			keyEvtH=new System.Windows.Forms.KeyEventHandler(this.chart_KeyDown);	
			this.ChartControl.ChartPanel.KeyDown += keyEvtH;
			
			// а чем мы там, собственно, торгуем-то?
			instrument = Instrument.MasterInstrument.Name;
			
			// и еще от фонаря зададим прямоугльник чарта
			prevBounds.X = 0;
			prevBounds.Y = 0;
			prevBounds.Width = 100;
			prevBounds.Height = 100;
			
			// ну и определимся наконец с полными путями к папка-файлам для записи и чтения - у каждого инструмента и таймфрейма будет отдельная запись
			studioFolderName	= Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\NinjaTrader 7\SavosRuStudio";
			projectFolderName	= studioFolderName + @"\" +this.Name.Replace("_v000", "").Replace("_v001", "").Replace("_v002", "").Replace("_v003", "");
			myFileName			= projectFolderName + @"\" + Instrument.FullName.Replace(" ", d) + d + BarsPeriod.Id.ToString().Substring(0,1) + d + BarsPeriod.Value + d + BarsPeriod.Value2 + ".dat";
			
			
			//Print("Started...");
		} // END of OnStartUp()
		#endregion INIT

		#region UTILITIES
		//------------------------
						
		#region RecalculateVPOCs
		//------------------------
		private void RecalculateVPOCsByNum(int num) {
			// ДЕЛАЕМ ОБЩУЮ "функцию" с параметром - номером диапазона - чтобы она четырежды не дублировала все как сейчас есть...
			
			if(R[num].leftBar != 0 && R[num].rightBar != 0) {
				// имеется ОДИН натянутый диапазон
				//Print("Работаем с диапазоном #" + num);

				if (!R[num].isReady) {
					//Print("Перерасчет параметров диапазона номер " + num);
					// Будем все перерасчеты делать ТОЛЬКО если диапазон был изменен - при этом его свойство R[num].isReady будет сброшено в ЛОЖЬ
					R[num].vol_price.Clear(); // обнуляем "ладдер" и начинаем его заполнять
					R[num].VpocPrice = 0;
					R[num].VpocVol = 0;
					R[num].rangeVol = 0;
					R[num].rangeBars = 0;
					
					// Начинаем в цикле обрабатывать данные обо всех барах в нашем диапазоне
					for(int i = R[num].leftBar - 1; i < R[num].rightBar; i++) {
						//достанем из "списка списков" нужный нам список-свечку
						
						if(!bar_vol_price.ContainsKey(i)) {
							Print("Нет такого внутреннего списка для бара #" + i);
						} else {
							R[num].work_vol_price = bar_vol_price[i];
						}
						// организуем внутренний цикл и суммируем все объемы ПО ОЧЕРЕДНОМУ БАРУ-СВЕЧКЕ
						foreach(KeyValuePair<double, double> kvp in R[num].work_vol_price){
							// У нас в R[num].work_vol_price теперь находятся данные по всем объемно-ценовым уровням ПРОВЕРЯЕМОЙ СВЕЧКИ
							if (!R[num].vol_price.ContainsKey(kvp.Key)) {
								// Если в нашем "хранилище" еще нет соответствующей "полочки" или "ящика" (короче, нужной ячейки) - то мы ее добавляем!
								R[num].vol_price.Add(kvp.Key,kvp.Value);
							} else {
								// А если такая ячейка уже есть, значит мы ее добавили на другом баре и теперь вновь к ней подошли - добавляем (накапливаем) объемы
								R[num].vol_price[kvp.Key] += kvp.Value;
							}
							// Теперь увеличим суммарный обьем ДИАПАЗОНА на объем этого обработанного бара
							R[num].rangeVol += kvp.Value;
						}
						
						
						// Теперь увеличим счетчик баров ДИАПАЗОНА
						R[num].rangeBars ++;
					}
					
					// Получили "ладдер" для нашего диапазона - общий поценовой список объемов, без разделения на бары
					foreach(KeyValuePair<double, double> kvp in R[num].vol_price.OrderByDescending(key => key.Value))
					{
						// перебираем этот список самого мелкого объема до самого крупного - таким образом находим ПОК!
						R[num].VpocPrice	= kvp.Key;
						R[num].VpocVol		= kvp.Value;						
						break;	//Весь цикл, собственно, затевался только ради сортировки, так что получив первые же данные (самые максимальные) мы цикл прерываем!!!
					}
					//if(debugPerformance) if(!Historical) Print("! " + instrument + " | ReCalcVpoc -> " + DateTime.Now);
					// Высчитываем текущее СМЕЩЕНИЕ от правого края вместо конкретных номеров свечей - так удобнее рисовать и писать
					//Print("*** RECALCULATE *** Before: leftOfset = " + R[num].leftOffset + " | rightOffset = " + R[num].rightOffset);
					R[num].leftOffset = Bars.Count - 1 - R[num].leftBar;
					R[num].rightOffset = Bars.Count - 1 - R[num].rightBar;
					//Print("*** RECALCULATE *** After: leftOfset = " + R[num].leftOffset + " | rightOffset = " + R[num].rightOffset);
					
					// Также нам надо определить ВЕРХ и НИЗ этого диапазона по ценовой шкале
					R[num].Top = High[myHighestBar(High, R[num].leftOffset, R[num].rightOffset)];
					R[num].Bottom = Low[myLowestBar(Low, R[num].leftOffset, R[num].rightOffset)];
					
					// И в общем-то надо ширину тоже пересчитать, хоть она и в барах - так как именно ШИРИНУ мы и будем сохранять, а не правый бар!!!
					R[num].rangeWidth = R[num].rightBar - R[num].leftBar;
					
					// Рисуем продолжение линии ПОКа до первого бара
					RecalculateExtVPOCsByNum(num);
				}
				// Уточняем координаты для работы с мышкой
				//Print("L");
				R[num]._Xl = _X_from_BarNum(R[num].leftBar);
				//Print("R");
				R[num]._Xr = _X_from_BarNum(R[num].rightBar);
				//Print("T");
				R[num]._Yt = _Y_from_Price(R[num].Top);
				//Print("B");
				R[num]._Yb = _Y_from_Price(R[num].Bottom);
				//Print("_Xl=" + R[num]._Xl + " | _Xr=" + R[num]._Xr + " | _Yt=" + R[num]._Yt + " | _Yb=" + R[num]._Yb);
				
				R[num].isReady = true; 		// укажем, что мы уже все рассчитали!!!
				R[num].isPlotted = false; 	// укажем, что мы диапазон после пересчета еще не отрисовали!!!
				// ну и стоит сохраниться
				SaveProfilePosition();
			}
		}
		private void RecalculateExtVPOCsByNum(int num) {			
			// Рисуем продолжение линии ПОКа до первого бара, которые ее (линию) пересечет
			// ====================================================================================================
			// ЕСЛИ ВЫКЛЮЧЕНО - ЭТО ЗНАЧИТ БЫЛО СДЕЛАНО СПЕЦИАЛЬНО!!! ВЫХОДИМ!!!
			if(!R[num].extVpocWork) {
				R[num].extVpocBar = R[num].rightBar;
				return;		
			}
			// ====================================================================================================
			if(R[num].extVpocBar > 1) return;	// ПОК уже перестал быть открытым... вот когда сдвинем диапазон - тогда опять начнем проверять!
			
			// Ну, на самом деле рисовать мы будем в функции Plot(), а тут надо только подготовить номер того бара, R[num].extVpocBar
			// на котором рисование этой линии будет завершено... Если такого бара еще нет - этот номер равен нулю
			// Значит сравниваем все бары от правой границы диапазона - и пока они ниже или выше - рисуем!
			if(High[R[num].rightOffset - 1] <= R[num].VpocPrice) {
				// мы ВЫШЕ и будем рисовать линию до тех пор пока выше
				R[num].VpocType = 1;
				R[num].extVpocBar = 0;
				// организуем цикл от правого края диапазона до последнего бара
				// но весь цикл проходить не будем - выскочим при первом же найденном баре
				for(int i = R[num].rightBar+1; i < Bars.Count; i ++ ) {
					int index = (Bars.Count - i - 1);
					if(High[index] < R[num].VpocPrice) {
						// бар все еще ниже ПОКа - условия выполняются
					} else {
						// а вот и пересекли!!! присваиваем нужный номер и выходим!!!
						R[num].extVpocBar = i;
						//Print("ПОК пересечен ценой снизу вверх!");
						R[num].isReady = true; 		// укажем, что мы уже все рассчитали!!!
						R[num].isPlotted = false; 	// укажем, что мы диапазон после пересчета еще не отрисовали!!
						break;	// выход из цикла!
					}
				}
			} else if(Low[R[num].rightOffset - 1] >= R[num].VpocPrice) {
				// мы НИЖЕ и будем рисовать линию до тех пор, пока ниже
				R[num].VpocType = -1;
				R[num].extVpocBar = 0;
				// организуем цикл от правого края диапазона до последнего бара
				// но весь цикл проходить не будем - выскочим при первом же найденном баре
				for(int i = R[num].rightBar+1; i < Bars.Count; i ++ ) {
					int index = (Bars.Count - i - 1);
					if(Low[index] > R[num].VpocPrice) {
						// бар все еще выше ПОКа - условия выполняются
					} else {
						// а вот и пересекли!!! присваиваем нужный номер и выходим!!!
						R[num].extVpocBar = i;
						//Print("ПОК пересечен ценой сверху вниз!");
						R[num].isReady = true; 		// укажем, что мы уже все рассчитали!!!
						R[num].isPlotted = false; 	// укажем, что мы диапазон после пересчета еще не отрисовали!!
						break;	// выход из цикла!
					}
				}
			} else {
				// а вот тут придется еще подумать...
				R[num].extVpocBar = R[num].rightBar;
				R[num].VpocType = 0;
			}
			//Print("!!!@@@###");
		} // END of RecalculateExtVPOCsByNum()
		
		private void RecalculateVPOCs() {
			// ЕДИНОВРЕМЕННЫЙ ПЕРЕРАСЧЕТ ВСЕХ ПАРАМЕТРОВ ВЫДЕЛЕННЫХ ДИАПАЗОНОВ 
			// если они, эти диапазоны, конечно, у нас есть...
			
			if(R[1].leftBar != 0 && R[1].rightBar != 0) {
				// имеется ПЕРВЫЙ натянутый диапазон
				//Print("Работаем с ПЕРВЫМ диапазоном...");
				if (!R[1].isReady) {
					// Если диапазон (и профиль) еще не готов - мы полностью его рассчитываем
					RecalculateVPOCsByNum(1);
				} else {
					// А если уже готов - то пересчитывать надо только линию расширения ПОКа до первого встречного бара
					RecalculateExtVPOCsByNum(1);
					// ну и на случай скролла или масштабирования графика - нужен пересчет X и Y координат
					if(chartChanged){
						R[1]._Xl = _X_from_BarNum(R[1].leftBar);
						R[1]._Xr = _X_from_BarNum(R[1].rightBar);
						R[1]._Yt = _Y_from_Price(R[1].Top);
						R[1]._Yb = _Y_from_Price(R[1].Bottom);
						//Print("_Xl=" + R[1]._Xl + " | _Xr=" + R[1]._Xr + " | _Yt=" + R[1]._Yt + " | _Yb=" + R[1]._Yb);
					}
				}
			}
			if(R[2].leftBar != 0 && R[2].rightBar != 0) {
				// имеется ВТОРОЙ натянутый диапазон
				//Print("Работаем со ВТОРЫМ диапазоном...");
				if (!R[2].isReady) {
					// Если диапазон (и профиль) еще не готов - мы полностью его рассчитываем
					RecalculateVPOCsByNum(2);
				} else {
					// А если уже готов - то пересчитывать надо только линию расширения ПОКа до первого встречного бара
					RecalculateExtVPOCsByNum(2);
					// ну и на случай скролла или масштабирования графика - нужен пересчет X и Y координат
					if(chartChanged){
						R[2]._Xl = _X_from_BarNum(R[2].leftBar);
						R[2]._Xr = _X_from_BarNum(R[2].rightBar);
						R[2]._Yt = _Y_from_Price(R[2].Top);
						R[2]._Yb = _Y_from_Price(R[2].Bottom);
						//Print("_Xl=" + R[2]._Xl + " | _Xr=" + R[2]._Xr + " | _Yt=" + R[2]._Yt + " | _Yb=" + R[2]._Yb);
					}
				}
			}
			if(R[3].leftBar != 0 && R[3].rightBar != 0) {
				// имеется ТРЕТИЙ натянутый диапазон
				//Print("Работаем с ТРЕТЬИМ диапазоном...");
				if (!R[3].isReady) {
					// Если диапазон (и профиль) еще не готов - мы полностью его рассчитываем
					RecalculateVPOCsByNum(3);
				} else {
					// А если уже готов - то пересчитывать надо только линию расширения ПОКа до первого встречного бара
					RecalculateExtVPOCsByNum(3);
					// ну и на случай скролла или масштабирования графика - нужен пересчет X и Y координат
					if(chartChanged){
						R[3]._Xl = _X_from_BarNum(R[3].leftBar);
						R[3]._Xr = _X_from_BarNum(R[3].rightBar);
						R[3]._Yt = _Y_from_Price(R[3].Top);
						R[3]._Yb = _Y_from_Price(R[3].Bottom);
						//Print("_Xl=" + R[3]._Xl + " | _Xr=" + R[3]._Xr + " | _Yt=" + R[3]._Yt + " | _Yb=" + R[3]._Yb);
					}
				}
			}
			if(R[4].leftBar != 0 && R[4].rightBar != 0) {
				// имеется ЧЕТВЕРТЫЙ натянутый диапазон
				//Print("Работаем с ЧЕТВЕРТЫМ диапазоном...");
				if (!R[4].isReady) {
					// Если диапазон (и профиль) еще не готов - мы полностью его рассчитываем
					RecalculateVPOCsByNum(4);
				} else {
					// А если уже готов - то пересчитывать надо только линию расширения ПОКа до первого встречного бара
					RecalculateExtVPOCsByNum(4);
					// ну и на случай скролла или масштабирования графика - нужен пересчет X и Y координат
					if(chartChanged){
						R[4]._Xl = _X_from_BarNum(R[4].leftBar);
						R[4]._Xr = _X_from_BarNum(R[4].rightBar);
						R[4]._Yt = _Y_from_Price(R[4].Top);
						R[4]._Yb = _Y_from_Price(R[4].Bottom);
						//Print("_Xl=" + R[4]._Xl + " | _Xr=" + R[4]._Xr + " | _Yt=" + R[4]._Yt + " | _Yb=" + R[4]._Yb);
					}
				}
			}
			rangesChanged = false;		 // все поменялось!!! но эти изменения уже учтены и с тех пор пока еще ничего не поменялось!!!
		}
		//------------------------
		#endregion RecalculateVPOCs
		
		#region myHighestBar
		private int myHighestBar(IDataSeries series, int startBarsAgo, int endBarsAgo)
		{
			// принимает не НОМЕРА баров, а обратный отсчет (сколько баров от конца)
			// возвращает также ОБРАТНЫЙ отсчет
			double highestHigh = series[endBarsAgo];
			int highestBar = endBarsAgo;
			//Print("start = " + startBarsAgo);
			//Print("end = " + endBarsAgo);
			//Print("high = " + highestHigh);
			
			for (int i = endBarsAgo; i <= startBarsAgo; i++) {
				//Print("series = " + series[i] + " || highest = " + highestHigh);
				if (series[i] > highestHigh) {
					highestHigh = series[i];
					highestBar = i;
				}
			}
			
			return highestBar;
		}
		#endregion myHighestBar
		
		#region myLowestBar
		private int myLowestBar(IDataSeries series, int startBarsAgo, int endBarsAgo)
		{
			// принимает не НОМЕРА баров, а обратный отсчет (сколько баров от конца)
			// возвращает также ОБРАТНЫЙ отсчет
			double lowestLow = series[endBarsAgo];
			int lowestBar = endBarsAgo;
			//Print("start = " + startBarsAgo);
			//Print("end = " + endBarsAgo);
			//Print("low = " + lowestLow);
			
			for (int i = endBarsAgo; i <= startBarsAgo; i++) {
				//Print("series = " + series[i] + " || lowest = " + lowestLow);
				if (series[i] < lowestLow) {
					lowestLow = series[i];
					lowestBar = i;
				}
			}
			
			return lowestBar;
		}
		#endregion myLowestBar
				
		#region StringToWorkDictionaryRange
		private void StringToWorkDictionaryRange(string tmpStr, int num) {
			// переделывает данную строку и складывает результат в предварительно очищенный work_vol_price с нужным номером на конце
			// ЧТОБЫ СЛУЧАЙНО НЕ ЗАЦЕПИТЬ ПОДОБНУЮ ПЕРЕМЕННУЮ В ФУНКЦИИ OnBarUpdate -  я вот таким образом буду это дело решать в функции PLOT
			string[] separator = {"[","]"};
			switch(num) {
				case 1:
					R[num].work_vol_price.Clear();
					// Вот тут без пол-литра не разобраться... но придется просто принимать эту конструкцию как есть!!!
					R[num].work_vol_price = tmpStr.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Split('=')).ToDictionary(split => Convert.ToDouble(split[0]), split => Convert.ToDouble(split[1]));
					break;
				case 2:
					R[num].work_vol_price.Clear();
					// Вот тут без пол-литра не разобраться... но придется просто принимать эту конструкцию как есть!!!
					R[num].work_vol_price = tmpStr.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Split('=')).ToDictionary(split => Convert.ToDouble(split[0]), split => Convert.ToDouble(split[1]));
					break;
				case 3:
					R[num].work_vol_price.Clear();
					// Вот тут без пол-литра не разобраться... но придется просто принимать эту конструкцию как есть!!!
					R[num].work_vol_price = tmpStr.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Split('=')).ToDictionary(split => Convert.ToDouble(split[0]), split => Convert.ToDouble(split[1]));
					break;
				case 4:
					R[num].work_vol_price.Clear();
					// Вот тут без пол-литра не разобраться... но придется просто принимать эту конструкцию как есть!!!
					R[num].work_vol_price = tmpStr.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Split('=')).ToDictionary(split => Convert.ToDouble(split[0]), split => Convert.ToDouble(split[1]));
					break;
				default:
					break;
			}
			//Print("Рабочий ладдер №" + num + " содержит " + R[num].work_vol_price.Count + " записей");
		}
		#endregion StringToWorkDictionaryRange
		
		#region RemoveRangeByNum
		public void RemoveRangeByNum(int num) {
			//Print("REMOVE before:  Now=" + nowRanges + " | Selected=" + curSelectedRange + " | Created=" + curCreatedRange);
			
			switch(num) {
				case 1:
					//Print("Close-1");
					// закрыть надо бы
					// то есть просто остальные три диапазона сдвинуть вниз, а последний еще и обнулить
					//R[1] = R[2];
					R[1].leftBar		= R[2].leftBar;
					R[1].rightBar		= R[2].rightBar;
					R[1].Top			= R[2].Top;
					R[1].Bottom			= R[2].Bottom;
					R[1].VpocPrice		= R[2].VpocPrice;
					R[1].VpocVol		= R[2].VpocVol;
					R[1].extVpocBar		= R[2].extVpocBar;
					R[1].extVpocWork	= R[2].extVpocWork;
					R[1].rangeWidth		= R[2].rangeWidth;
					
					//R[2] = R[3];
					R[2].leftBar		= R[3].leftBar;
					R[2].rightBar		= R[3].rightBar;
					R[2].Top			= R[3].Top;
					R[2].Bottom			= R[3].Bottom;
					R[2].VpocPrice		= R[3].VpocPrice;
					R[2].VpocVol		= R[3].VpocVol;
					R[2].extVpocBar		= R[3].extVpocBar;
					R[2].extVpocWork	= R[3].extVpocWork;
					R[2].rangeWidth		= R[3].rangeWidth;
					
					//R[3] = R[4];
					R[3].leftBar		= R[4].leftBar;
					R[3].rightBar		= R[4].rightBar;
					R[3].Top			= R[4].Top;
					R[3].Bottom			= R[4].Bottom;
					R[3].VpocPrice		= R[4].VpocPrice;
					R[3].VpocVol		= R[4].VpocVol;
					R[3].extVpocBar		= R[4].extVpocBar;
					R[3].extVpocWork	= R[4].extVpocWork;
					R[3].rangeWidth		= R[4].rangeWidth;

					R[4].leftBar	= 0;
					R[4].rightBar	= 0;
					R[4].Top		= 0;
					R[4].Bottom		= 0;
					R[4].VpocPrice	= 0;
					R[4].VpocVol	= 0;
					R[4].extVpocOffset = 0;
					R[4].extVpocBar = 0;
					R[4].extVpocWork	= true;				// ИСТИНА!!!
					R[4].rangeWidth		= 0;
					R[4]._Xl		= 0;
					R[4]._Xr		= 0;
//					R[4]._Yb		= 0;
//					R[4]._Yt		= 0;
					R[4].vol_price.Clear();
					
					//Print("1 closed...");
					break;
				case 2:
					//Print("Close-2");
					// закрыть надо бы
					// то есть просто последние два сдвинуть вниз, а самый последний еще и обнулить
					//R[2] = R[3];
					R[2].leftBar		= R[3].leftBar;
					R[2].rightBar		= R[3].rightBar;
					R[2].Top			= R[3].Top;
					R[2].Bottom			= R[3].Bottom;
					R[2].VpocPrice		= R[3].VpocPrice;
					R[2].VpocVol		= R[3].VpocVol;
					R[2].extVpocBar		= R[3].extVpocBar;
					R[2].extVpocWork	= R[3].extVpocWork;
					R[2].rangeWidth		= R[3].rangeWidth;
					
					//R[3] = R[4];
					R[3].leftBar		= R[4].leftBar;
					R[3].rightBar		= R[4].rightBar;
					R[3].Top			= R[4].Top;
					R[3].Bottom			= R[4].Bottom;
					R[3].VpocPrice		= R[4].VpocPrice;
					R[3].VpocVol		= R[4].VpocVol;
					R[3].extVpocBar		= R[4].extVpocBar;
					R[3].extVpocWork	= R[4].extVpocWork;
					R[3].rangeWidth		= R[4].rangeWidth;

					R[4].leftBar	= 0;
					R[4].rightBar	= 0;
					R[4].Top		= 0;
					R[4].Bottom		= 0;
					R[4].VpocPrice	= 0;
					R[4].VpocVol	= 0;
					R[4].extVpocOffset = 0;
					R[4].extVpocBar = 0;
					R[4].extVpocWork	= true;				// ИСТИНА!!!
					R[4].rangeWidth		= 0;
					R[4]._Xl		= 0;
					R[4]._Xr		= 0;
//					R[4]._Yb		= 0;
//					R[4]._Yt		= 0;
					R[4].vol_price.Clear();
					
					//Print("2 closed...");
					break;
				case 3:
					//Print("Close-3");
					// закрыть надо бы
					// то есть просто предпоследний сдвинуть вниз, а самый последний обнулить
					//R[3] = R[4];
					R[3].leftBar		= R[4].leftBar;
					R[3].rightBar		= R[4].rightBar;
					R[3].Top			= R[4].Top;
					R[3].Bottom			= R[4].Bottom;
					R[3].VpocPrice		= R[4].VpocPrice;
					R[3].VpocVol		= R[4].VpocVol;
					R[3].extVpocBar		= R[4].extVpocBar;
					R[3].extVpocWork	= R[4].extVpocWork;
					R[3].rangeWidth		= R[4].rangeWidth;

					R[4].leftBar	= 0;
					R[4].rightBar	= 0;
					R[4].Top		= 0;
					R[4].Bottom		= 0;
					R[4].VpocPrice	= 0;
					R[4].VpocVol	= 0;
					R[4].extVpocOffset = 0;
					R[4].extVpocBar = 0;
					R[4].extVpocWork	= true;				// ИСТИНА!!!
					R[4].rangeWidth		= 0;
					R[4]._Xl		= 0;
					R[4]._Xr		= 0;
//					R[4]._Yb		= 0;
//					R[4]._Yt		= 0;
					R[4].vol_price.Clear();
						
					//Print("3 closed...");
					break;
				case 4:
					//Print("Close-4");
					// закрыть надо бы
					// то есть просто обнулить диапазон, ведь он последний
					R[4].leftBar	= 0;
					R[4].rightBar	= 0;
					R[4].Top		= 0;
					R[4].Bottom		= 0;
					R[4].VpocPrice	= 0;
					R[4].VpocVol	= 0;
					R[4].extVpocOffset = 0;
					R[4].extVpocBar = 0;
					R[4].extVpocWork	= true;				// ИСТИНА!!!
					R[4].rangeWidth		= 0;
					R[4]._Xl		= 0;
					R[4]._Xr		= 0;
//					R[4]._Yb		= 0;
//					R[4]._Yt		= 0;
					R[4].vol_price.Clear();
										
					//Print("4 closed...");
					break;
				default:
					// это типа статуса "ничего"
					break;
			}
			nowRanges -= 1;
			if(nowRanges < 0) {
				// больше не осталось диапазонов
				nowRanges = 0;
			}
			curCreatedRange -= 1;
			if(curCreatedRange < 0) {
				// больше не осталось диапазонов
				curCreatedRange = 0;
			}

			curSelectedRange -= 1;
			if(curSelectedRange < 1) {
				// больше не осталось диапазонов или остались с другими номерами - надо смотреть на количество
				if (nowRanges > 0) {
					curSelectedRange = nowRanges;
				} else {
					curSelectedRange = 0;
				}
			}
			// Если остался всего ОДИН диапазон - он и должен быть активным!!!
			if(nowRanges == 1) curSelectedRange = 1;
			// Если удалили ПОСЛЕДНИЙ диапазон и больше на графике их нет - удаляем и метку активного диапазона!
			if(nowRanges == 0) RemoveDrawObject("curRange");
			
			R[1].vol_price.Clear();
			R[2].vol_price.Clear();
			R[3].vol_price.Clear();
			R[4].vol_price.Clear();
			
			// Теперь предстоит по-любому обновить ВСЕ оставшиеся диапазоны
			R[1].isReady = false;
			R[1].isPlotted = false;
			R[2].isReady = false;
			R[2].isPlotted = false;
			R[3].isReady = false;
			R[3].isPlotted = false;
			R[4].isReady = false;
			R[4].isPlotted = false;
			
			SaveProfilePosition();
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
			//Print("REMOVE:  Now=" + nowRanges + " | Selected=" + curSelectedRange + " | Created=" + curCreatedRange);
		}
		#endregion RemoveRangeByNum
		
		// ----------- для работы с мышкой и прямой графикой ----------
				
		#region _Y_from_Price()
		private int _Y_from_Price(double price)
        {
            return ChartControl.GetYByValue(this, price);
        }
		#endregion _Y_from_Price()
		
		#region _X_from_BarNum()
		private int _X_from_BarNum(int num)
        {
			//Print("X_from_Bar: bar=" + num);
            return ChartControl.GetXByBarIdx(this.Bars, num);
        }
		#endregion _X_from_BarNum()
		
		#region GetActionFromMouse
		//------------------------
		private StatusType GetActionFromMouse(int mX, int mY)
		{
			StatusType result = StatusType.NotMoving;
			int dX = 5;
			int dY = 5;			
			//Print("mouseX=" + mX + " | mouseY=" + mY);
			
			// КНОПКА ЗАКРЫТИЯ - самая первая, так как накладывается на координаты правой границы и внутреннего пространства
			// и если ее не сделать первой - то ТЕ координаты сработают и мы кнопку не нажмем...
			if(mX >= R[1]._Xr - 2*dX && mX <= R[1]._Xr + dX && mY <= R[1]._Yt + 2*dY && mY >= R[1]._Yt - 2*dY) {
				Cursor.Current = Cursors.Cross;
				//Print("Over CloseBurron-1");
				return StatusType.CloseRange1;
			}
			if(mX >= R[2]._Xr - 2*dX && mX <= R[2]._Xr + dX && mY <= R[2]._Yt + 2*dY && mY >= R[2]._Yt - 2*dY) {
				Cursor.Current = Cursors.Cross;
				//Print("Over CloseBurron-2");
				return StatusType.CloseRange2;
			}
			if(mX >= R[3]._Xr - 2*dX && mX <= R[3]._Xr + dX && mY <= R[3]._Yt + 2*dY && mY >= R[3]._Yt - 2*dY) {
				Cursor.Current = Cursors.Cross;
				//Print("Over CloseBurron-3");
				return StatusType.CloseRange3;
			}
			if(mX >= R[4]._Xr - 2*dX && mX <= R[4]._Xr + dX && mY <= R[4]._Yt + 2*dY && mY >= R[4]._Yt - 2*dY) {
				Cursor.Current = Cursors.Cross;
				//Print("Over CloseBurron-4");
				return StatusType.CloseRange4;				
			} 

			// ЛЕВАЯ ГРАНИЦА
			//if(mX >= R[1]._Xl - dX && mX <= R[1]._Xl + dX && mY <= R[1]._Yb && mY >= R[1]._Yt) {
			if(mX >= R[1]._Xl && mX <= R[1]._Xl + dX && mY <= R[1]._Yb && mY >= R[1]._Yt) {
				Cursor.Current = Cursors.SizeAll;
				//Print("Over Left Side-1");
				return StatusType.MovingRange1;
			}
			if(mX >= R[2]._Xl - dX && mX <= R[2]._Xl + dX && mY <= R[2]._Yb && mY >= R[2]._Yt) {
				Cursor.Current = Cursors.SizeAll;
				//Print("Over Left Side-2");
				return StatusType.MovingRange2;
			}
			if(mX >= R[3]._Xl - dX && mX <= R[3]._Xl + dX && mY <= R[3]._Yb && mY >= R[3]._Yt) {
				Cursor.Current = Cursors.SizeAll;
				//Print("Over Left Side-3");
				return StatusType.MovingRange3;
			}
			if(mX >= R[4]._Xl - dX && mX <= R[4]._Xl + dX && mY <= R[4]._Yb && mY >= R[4]._Yt) {
				Cursor.Current = Cursors.SizeAll;
				//Print("Over Left Side-4");
				return StatusType.MovingRange4;
			}
			// ПРАВАЯ ГРАНИЦА
			if(mX >= R[1]._Xr - dX && mX <= R[1]._Xr + dX && mY <= R[1]._Yb && mY >= R[1]._Yt + 2* dY) {
				Cursor.Current = Cursors.SizeWE;
				//Print("Over Right Side-1");
				return StatusType.ChangeRange1;
			}
			if(mX >= R[2]._Xr - dX && mX <= R[2]._Xr + dX && mY <= R[2]._Yb && mY >= R[2]._Yt + 2* dY) {
				Cursor.Current = Cursors.SizeWE;
				//Print("Over Right Side-2");
				return StatusType.ChangeRange2;
			}
			if(mX >= R[3]._Xr - dX && mX <= R[3]._Xr + dX && mY <= R[3]._Yb && mY >= R[3]._Yt + 2* dY) {
				Cursor.Current = Cursors.SizeWE;
				//Print("Over Right Side-3");
				return StatusType.ChangeRange3;
			}
			if(mX >= R[4]._Xr - dX && mX <= R[4]._Xr + dX && mY <= R[4]._Yb && mY >= R[4]._Yt + 2* dY) {
				Cursor.Current = Cursors.SizeWE;
				//Print("Over Right Side-4");
				return StatusType.ChangeRange4;
			}
			// ВНУТРИ ДИАПАЗОНА
			if(mX >= R[1]._Xl && mX <= R[1]._Xr && mY <= R[1]._Yb && mY >= R[1]._Yt) {
				//Print("Inside Range-1")
				return StatusType.SelectRange1;
			}
			if(mX >= R[2]._Xl && mX <= R[2]._Xr && mY <= R[2]._Yb && mY >= R[2]._Yt) {
				//Print("Inside Range-2")
				return StatusType.SelectRange2;
			}
			if(mX >= R[3]._Xl && mX <= R[3]._Xr && mY <= R[3]._Yb && mY >= R[3]._Yt) {
				//Print("Inside Range-3")
				return StatusType.SelectRange3;
			}
			if(mX >= R[4]._Xl && mX <= R[4]._Xr && mY <= R[4]._Yb && mY >= R[4]._Yt) {
				//Print("Inside Range-4")
				return StatusType.SelectRange4;
			}
			
			return result;
		}
		//------------------------
		#endregion GetActionFromMouse
	
		#region GetBarFromX
		private int GetBarFromX(int x)
        {
			//Print("GetBarFromX: x = " + x);
            if (ChartControl == null)
                return 0;
			
	    	int idxSelectedBar		= 0;
            int idxFirstBar 		= ChartControl.FirstBarPainted;
            int idxLastBar 		= ChartControl.LastBarPainted;
            int totalNoOfBars	 	= Bars.Count - 1;
            int firstBarX 			= ChartControl.GetXByBarIdx(Bars,idxFirstBar);
		    int lastBarX 			= ChartControl.GetXByBarIdx(Bars,idxLastBar);
		    int pixelRangeOfBars 	= lastBarX - firstBarX + 1;
	    	int selectedBarNoScreen= (int)(Math.Round(((double)(x - firstBarX)) / (ChartControl.BarSpace), 0));
	    	int SelectedBarNumber 	= idxFirstBar + selectedBarNoScreen;
			
			if (x <= firstBarX)
				idxSelectedBar = totalNoOfBars - idxFirstBar;
			else if (x >= lastBarX)
				idxSelectedBar = totalNoOfBars - idxLastBar;
			else
				idxSelectedBar = totalNoOfBars - SelectedBarNumber;
			
			if (false) // Display info in Output window if enabled 
			{
				Print("------------------------------");
				Print("Instrument: " + Instrument.FullName + ",    Chart: " +  BarsPeriod.ToString()); // To identify which chart the data was coming from									
				Print("Mouse Coordinate X: " + x.ToString());
				Print("Total Bars On Chart: " + totalNoOfBars.ToString());
				Print("BarWidth: " + ChartControl.BarWidth.ToString());
				Print("BarSpace: " + ChartControl.BarSpace.ToString());
				Print("Firs Bar Idx On Screen: " + idxFirstBar.ToString());
				Print("Last Bar Idx On Screen: " + idxLastBar.ToString());
				Print("No Of Bars On Screen: " + (idxLastBar - idxFirstBar).ToString());
				Print("First (Left) On Screen Bar X Coordinate: " + firstBarX.ToString());
				Print("Last (Right) On Screen Bar X Coordinate: " + lastBarX.ToString());
				Print("Pixel Range Of Visible Bars: " + pixelRangeOfBars.ToString());
				Print("Bar Position On Screen From Left: " + selectedBarNoScreen.ToString());
				Print("Selected Bar Number: " + SelectedBarNumber.ToString());
				Print("Selected Bar Index: " + idxSelectedBar.ToString());
			}
				
			return idxSelectedBar;
        }
		#endregion GetBarFromX
		
		// ----------- для внутреннего ПОКа баров -----------
			
		#region CHECK_NAKED_VPOC		
		public void Check_Naked_VPOCs() {
			// копилка nakedVPOC_list и nakedStrongVPOC_list
			// Проверяем VPOC два бара назад (ближе нельзя!) на его соответствие требований к "голышам" - NAKED VPOCs - ГОЛЫЕ ПОКи
			// Но сначала фильтр по величину объема - может он нам вообще не подходит и не важно "голый" он или одетый???
			int BarNum = CurrentBar - 2;
			
			if(true
				&& ((VPOC[2] > High[3]) && (VPOC[2] > High[1]))
				|| ((VPOC[2] < Low[3]) && (VPOC[2] < Low[1]))
			){
				// Это и есть наш "голыш" - кладем в копилочку
				if(!nakedVPOC_list.ContainsKey(BarNum)) {
					nakedVPOC_list.Add(BarNum, VPOC[2]);
				} else {
					nakedVPOC_list[BarNum] = VPOC[2];
				}
				// Но в реал-тайме его показываем ТОЛЬКО если включен режим обычных голышей
				if(!Historical && showNakedVPOCs) {
					DrawDot(BarNum.ToString() + "vpocDot", false, 2, VPOC[2], myVPOCsColor);
				}
			} else {
				// А вот это уже НЕ "голыш"	- значит его НЕ надо показывать!!!
				// То есть ничего тут не делаем...
			}
		}
		
		public void Show_NAKED_VPOCs(bool show) {
			// копилка nakedVPOC_list и nakedStrongVPOC_list
			//Print("ShowNAKED: " + show);
			Int32 BarNum = 0;
			double Price = 0;
			int offset = 0;
			if(show) {
				// в реалтайме оно все будет дальше само рисоваться - тут надо вывести в цикле все имевшееся ранеее
				if(showNakedVPOCs) {
					// обычные голыши
					foreach(KeyValuePair<Int32, double> kvp in nakedVPOC_list)
					{	
						BarNum=kvp.Key;
						Price=kvp.Value;						
						offset = Bars.Count - 1 - BarNum;
						DrawDot(BarNum.ToString() + "vpocDot", false, offset, Price, myVPOCsColor);
					}
				}
			} else {
				// в реалтайме оно все просто перестанет дальше само рисоваться - тут надо спрятать в цикле все имевшееся ранеее
				if(!showNakedVPOCs) {
					// обычные голыши
					foreach(KeyValuePair<Int32, double> kvp in nakedVPOC_list)
					{	
						BarNum=kvp.Key;
						Price=kvp.Value;						
						offset = Bars.Count - 1 - BarNum;
						RemoveDrawObject(BarNum.ToString() + "vpocDot");
					}
				}
				// а если при этом были включены ДРУГИЕ ПАТТЕРНЫ (которых тут нет, но сами можете сделать), то часть их сейчас удалилась - надо их перерисовать!!!
				// например так -> if(showHiddenVPOCs) Show_HIDDEN_VPOCs(true);				
			}
			// после всего  надо дать знать графику - мол, пора перерисовать картинку
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
			// впрочем, это можно сделать и там, откуда данная функция будет вызвана
		}

		#endregion CHECK_NAKED_VPOC
		
		#region SHOW_HIDE_ALL_VPOCS
		
		public void Show_ALL_VPOCs(bool show) {
			//Print("ShowALL: " + show);
			//Print("VPOCs Count: " + VPOC.Count);
			if(show) {
				// в реалтайме оно все будет дальше само рисоваться - тут надо вывести в цикле все имевшееся ранеее
				for(int i = 10; i <= Bars.Count - 1; i++) {
					int offset = Bars.Count - 1 - i;
					DrawDot(i.ToString() + "vpocDot", false, offset, VPOC[offset], myVPOCsColor);
				}
			} else {
				// в реалтайме оно все просто перестанет дальше само рисоваться - тут надо спрятать в цикле все имевшееся ранеее
				for(int i = 10; i <= Bars.Count - 1; i++) {
					int offset = Bars.Count - 1 - i;
					RemoveDrawObject(i.ToString() + "vpocDot");
				}
			}
			// после всего  надо дать знать графику - мол, пора перерисовать картинку
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
			// впрочем, это можно сделать и там, откуда данная функция будет вызвана
		}
				
		#endregion SHOW_HIDE_ALL_VPOCS
		
		// ------------ для сохранения текущих положений профилей --------
		
		#region SAVE_READ_PROFILE_POSITION
		//----------------------------
		public void SaveProfilePosition() {
			DirectoryInfo drMyInfoDir = new DirectoryInfo(projectFolderName);
			// сначала проверяем наличие папок и создаем их если надо
			if (!drMyInfoDir.Exists) {
				drMyInfoDir.Create(); // создается за один проход папка любой вложенности с родителями если надо
				//Print("*** SaveProfilePosition *** Создали папку " + drMyInfoDir.FullName);
			}	else { 	
				//Print("*** SaveProfilePosition *** Папка " + drMyInfoDir.FullName + " уже есть!"); 
			}
			try{
				StreamWriter file = new StreamWriter(myFileName);
				for(int i=1; i<=4; i++) {
					// если у нас нет какого-то диапазона - пишем вместо  него нули все-равно!!!
					// то есть у нас потом при чтении ВСЕГДА должно получаться 4 строки!!!
					// и формат этих строчек такой: левый бар, ширина диапазона и ширина профиля - все остальное можно из этого вычислить...
					// только левый бар задан не НОМЕРОМ... не СМЕЩЕНИЕМ... а конкретным ВРЕМЕНЕМ!!! 
					
					file.WriteLine(Times[0][R[i].leftOffset] + " | " + R[i].rangeWidth + " | " + R[i].profileWidth);
				}
				file.Close();
				//Print("*** SaveProfilePosition *** Записано в файл " + myFileName);
			} catch {
				try{
				//Print(" тут ошибка?");
				// ничего страшного! значит сохранимся позже!
				StreamWriter file = new StreamWriter(myFileName);
				for(int i=1; i<=4; i++) {
					file.WriteLine(Times[0][R[i].leftOffset] + " | " + R[i].rangeWidth + " | " + R[i].profileWidth);
				}
				file.Close();
				} catch {
					// ничего страшного! значит сохранимся позже!
				}
			}
		}
		
		public int ReadProfilePosition() {
			int result = 0;
			DirectoryInfo drMyInfoDir = new DirectoryInfo(projectFolderName);
			FileInfo myInfoFile = new FileInfo(myFileName);
			// сначала проверяем наличие папки  - если нет, значит восстанавливать не из чего и просто выходим
			if (!drMyInfoDir.Exists) {
				//Print("*** ReadProfilePosition *** Нет такой папки: " + drMyInfoDir.FullName);
				return 0;
			}
			// ОК! теперь папки точно есть, проверяем наличие файла - если нет, значит восстанавливать не из чего и просто выходим
			if (!myInfoFile.Exists) {
				//Print("*** ReadProfilePosition *** Нет такого файла: " + myInfoFile.FullName);
				return 0;
			}
			// OK! и файл в наличии - можно читать!!!
			StreamReader file = new StreamReader(myFileName);
			// мы четко знаем: нам нужно всего 4 первые строки!
			for(int i=1; i<=4; i++)
			{
				// то есть у нас потом при чтении ВСЕГДА должно получаться 4 строки!!!
				// и формат этих строчек такой: левый бар, ширина диапазона и ширина профиля - все остальное можно из этого вычислить...
				// только левый бар задан не НОМЕРОМ... не СМЕЩЕНИЕМ... а конкретным ВРЕМЕНЕМ!!! 				
				char c = '|';
				string[] lines = file.ReadLine().Split(c);
				if(lines.Length != 3)
				{
					// что-то не так - просто выходим, тут не страшно... Значит просто профили не восстановятся и всё...
					Print("*** ReadProfilePosition *** В прочитанной строке НЕ ТРИ переменные! Их тут " + lines.Length);
					return 0;
				}
				
				// еще одна проверка: НИ ОДНО из прочитанных значений не должно быть нулем!!!
				// если НОЛЬ есть - выходим из этой итерации цикла и начинаем искать дальше! тут толку уже не будет!!!
				if((Convert.ToInt32(lines[1])  < 1) || (Convert.ToInt32(lines[2])  < 1)) continue;
				
				
				// Раз мы тут - значит все ОК! Но как же теперь прочитанное время перевести в номер бара-то???
				// видимо просто в лоб: перебором от последнего бара назад в историю
				// Так что присвоим очередной запсиси НУЛИ и дальше если найдем нужный бар - перепишем,
				// а если не найдем - значит этот профиль останется пустым - заодно решится и проблема с ОШИБКОЙ при МаркетРеплейном восстановлении "будущих" баров
				R[i].leftBar		= 0;
				R[i].rangeWidth		= 0;
				R[i].profileWidth	= 0;
				
				for(int num = 0 ; num < Bars.Count - 1; num ++)
				{
					//Print("num: " + num + " | Time: " + Times[0][num]);
					if(Times[0][num] == DateTime.Parse(lines[0]))
					{
						// НАШЛИ!!!
						R[i].leftOffset = num;
						// то есть уже по-любому будем из функции чтения возвращать КАКОЕ-ТО число
						result ++;
						// но не только можно, а и нужно пересчитать другие параметры диапазона и профиля
						R[result].rangeWidth = Convert.ToInt32(lines[1]);
						R[result].profileWidth = Convert.ToInt32(lines[2]);
						R[result].leftBar = Bars.Count - 1 - num;
						R[result].rightBar = R[result].leftBar + R[result].rangeWidth;
						if(R[result].profileWidth != R[result].rangeWidth) {
							R[result].isExtended = true;
						} else {
							R[result].isExtended = false;
						}
						switch(result) {
							case 1:
								R[1].rangeColor = rangeColor1;
								break;
							case 2:
								R[2].rangeColor = rangeColor2;
								break;
							case 3:
								R[3].rangeColor = rangeColor3;
								break;
							case 4:
								R[4].rangeColor = rangeColor4;
								break;								
						}
						// и выставить флаги незавершенности и неотрисованности профиля
						R[result].isReady = false;
						R[result].isPlotted = false;
						// ТЕПЕРЬ можно выходить! разумеется ТОЛЬКО из внутреннего цикла!
						break;
					}
				}
				// а внешний цикл пройдет свои 4 круга по-любому!
				//Print("Left: " + R[i].leftBar + " | Width: " + R[i].rangeWidth + " | Profile: " + R[i].profileWidth);
			}
			// вот он - конец внешнего цикла!						
			return result;
		}
		//----------------------------
		#endregion SAVE_READ_PROFILE_POSITION
		
		//------------------------
		#endregion Utilities

		#region ON_BAR_UPDATE
		protected override void OnBarUpdate()
        {
			if(!Historical && !readOnce) {
				// при первых же тиках реал-тайма - то есть уже после обработки исторической части
				// читаем сохраненные данные о профилях (если есть что читать)
				nowRanges = ReadProfilePosition();
				if( nowRanges > 0) {
					// ОЙ! мы тут чего-то нашли!!!
					curSelectedRange = 1;
					curCreatedRange = nowRanges;
					// по идее достаточно вызвать перерисовку - остальное перерасчитается само что надо
					R[1].isReady = false;
					R[2].isReady = false;
					R[3].isReady = false;
					R[4].isReady = false;
					this.ChartControl.ChartPanel.Invalidate();
					this.ChartControl.Refresh();
					//Print("LOAD:  Now=" + nowRanges + " | Selected=" + curSelectedRange + " | Created=" + curCreatedRange);
				}
				// читаем один раз - больше не надо!
				readOnce = true;
				
				// А еще выведем надпись-предупреждалочку... до первого нажатия на клавиши - потом она сама уйдет
				DrawTextFixed("HotKeyStat", "Press SHIFT+\"?\" for vew HELP...\r\n\r\n", TextPosition.BottomLeft, helpFontColor, new Font("Arial", 7, FontStyle.Regular), Color.Transparent, Color.Transparent, 0);
			}
			
			//**********************************************************************************************************************
			if (Close[0]>max) max=Close[0];	// используется в GetTicksFromMouseClick() - не удалять эту строку ни в коем случае!!!
			if (Close[0]<min) min=Close[0];	// используется в GetTicksFromMouseClick() - не удалять эту строку ни в коем случае!!!
			//**********************************************************************************************************************
			
			// Предварительная подготовка - постоянный безостановочный сбор тиковых данных и распределение их по структурам для хранения
			#region MAKE_ALL_LADDERS
			/**/
			// ЕДИНИЦА обозначает, что это событие OnBarUpdate было вызвано не для свечки основного таймфрейма нашего графика
			// а для добавленного 1-тикового таймфрейма. Именно тут можно обрабатывать поступающие данные ПОТИКОВО!!!
			if (BarsInProgress==1) {
				if (!vol_price.ContainsKey(Closes[1][0])) {
					// Если в нашем "хранилище" еще нет соответствующей "полочки" или "ящика" (короче, нужной ячейки) - то мы ее добавляем!
					// Это значит, что на основном таймфрейме в нашей текущей свечке на этом ценовом уровне пока еще не было сделок и данный тик принес
					// первые данные, например, нужную нам информацию по объемам.
                	vol_price.Add(Closes[1][0],Volumes[1][0]);
				} else {
					// А если такая ячейка уже есть, значит мы к имеющимся там данным добавляем очередную порцию!
					// То есть накапливаем обьем на этом ценовом уровне в нашей свечке на основном таймфрейме.
                    vol_price[Closes[1][0]]+=Volumes[1][0];
				}
			}
			
			
			// НОЛЬ обозначает, что событие OnBarUpdate было вызвано как раз для основного таймфрейма - то есть именно свечка нашего графика
			// получила порцию изменений и тут можно с этим что-то делать. Ну что там мы обычно в этом событии и собираемся...
			if (BarsInProgress==0 && CurrentBars[0] > 10) {
				if(FirstTickOfBar) {
					// Ну тут все понятно - это ПЕРВЫЙ тик свечки на ОСНОВНОМ таймфрейме, то есть самое начало нашей свечи
					int prevBar = CurrentBars[0] - 1; // Это номер только что закрывшейся свечки, данные по которой мы сейчас как раз и обрабатываем

					// Сначала из накопленных тиковых данных соберем информацию про ПОК текущей свечки
					//------------
					#region VOL-Ladder
					double vpoc_price=0;
					double vpoc_vol=0;
					double vol = 0;
					//------------
					foreach(KeyValuePair<double, double> kvp in vol_price.OrderByDescending(key => key.Value))
					{	// У нас в vol_price накопились данные по всем объемно-ценовым уровням ПРОШЛОЙ СВЕЧКИ
						// и мы их перебираем от самого мелкого до самого крупного - таким обращом находим ПОК!
						// Причем и сам ценовой уровень, и значение объема наэтом уровне - а значит можем при желании ФИЛЬТРОВАТЬ
						// эти значения объемов. Но пока не будем...
						vpoc_price=kvp.Key;
						vpoc_vol=kvp.Value;						
						break;	//Весь цикл, собственно, затевался только ради сортировки, так что получив первые же данные (самые максимальные) мы цикл прерываем!!!
					}
					//------------
					// Теперь, когда ПОК прошлой свечки найден - записываем его в общий список ВСЕХ ПОКОВ нашего графика
					// При этом, поскольку в ЗНАЧЕНИЯХ ИНДИКАТОРА мы просто обязаны хранить ЦЕНУ (иначе как показать его на графике???
					// то в значениях СПИСКА ПОКов мы сохраним ОБЪЕМ
					VPOC[0] = vpoc_price;
					
					if (!vPoc_list.ContainsKey(CurrentBar-1)) {
						vPoc_list.Add(CurrentBar-1,vpoc_vol);	// окончательно рассчитанный объем ПОКа предыдущей свечки!!!
					} else {
						vPoc_list[CurrentBar-1] = vpoc_vol;
					}
					//------------
					// Ну а теперь надо освободить vol_price для того, чтобы в нем начала накапливаться новая история - уже по текущей, 
					// только что открывшейся свечке основного таймфрейма на нашем графике
					//vol_price.Clear();
					#endregion VOL-Ladder
						
					//------------
					#region BAR_VOL-Ladder
//					// По какой-то непонятной мне причине при сохранении Dictionary внутри другой Dictionary и обратном ее извлечении - получаем пустоту...
//					// Пробуем обойти это через преобразование в строку
//					string tmpStr = "";
//					foreach(KeyValuePair<double, double> kvp in vol_price){
//						tmpStr += "[" + kvp.Key + "=" + kvp.Value + "]";	// формат подобран под функцию обратной конвертации строки в список - не надо тут ничего менять!!!
//					}
//					//Print("!!! !!! " + tmpStr);
//					if (!volLadderStr.ContainsKey(prevBar)) {
//						// Если в нашем "хранилище" еще нет соответствующей "полочки" или "ящика" (короче, нужной ячейки) - то мы ее добавляем!
//						volLadderStr.Add(prevBar,tmpStr);
//					} else {
//						// А если такая ячейка уже есть, значит мы заменим имеющиеся там данные
//						volLadderStr[prevBar] = tmpStr;
//					}
					
					// Создаем СПИСОК БАРОВ, в каждой записи которого будут СПИСКИ ОБЪЕМОВ на всех ценовых уровнях бара
					// То есть - пробуем вложить список в список
					if(!bar_vol_price.ContainsKey(prevBar)) {
						// Если в нашем списке еще нет записи по предыдущей свечке - создаем ее
						bar_vol_price.Add(prevBar,new Dictionary<double, double>());
					} else {
						// Если же эта запись есть (чего вообще-то быть не должно!!!) - очищаем ее
						bar_vol_price[prevBar].Clear();
					}
					// Теперь у нас нужная запись есть и она чистая - можно ее заполнять
					foreach(KeyValuePair<double, double> kvp in vol_price.OrderByDescending(key => key.Value))
					{
						// В цикле проходим по всем записям обьемов и цен, накопленных при обработке тиковых данных
						if(!bar_vol_price[prevBar].ContainsKey(kvp.Key)) {
							// На всякий случай не просто добавляем новую "запись в записи", а предвариетльно убеждаемся, что ее нет и добавление не вызовет ошибку
							bar_vol_price[prevBar].Add(kvp.Key, kvp.Value);
						} else {
							// Если же эта запись есть - чего опять-таки не должно быть - перезаписываем ее содержимое
							bar_vol_price[prevBar][kvp.Key] = kvp.Value;
						}
					}
					#endregion BAR_VOL-Ladder
					
					//------------
					// И вот ТОЛЬКО ТЕПЕРЬ после сохранения списка по ПРЕДУДУЩЕЙ свечке в нашем "списке списков" - теперь можно этот рабочий список обнулить
					// для того, чтобы в нем начала накапливаться новая история - уже по текущей, только что открывшейся свечке основного таймфрейма на нашем графике
					vol_price.Clear();
				}
				
				if (!Historical) {
					double poc_price2=0;
                    double poc_vol2=0;
                    foreach(KeyValuePair<double, double> kvp in vol_price.OrderByDescending(key => key.Value))
                    {	
						poc_price2=kvp.Key;
                        poc_vol2=kvp.Value;
                        break;
                    }
					// заглушка, чтобы убрать ошибку, когда ПОК на секундочку падает ниже плинтуса... потом возвращается, но нам этого падения НЕ НАДО-ть!!!
					if (poc_vol2 < Closes[0][1] - 50*TickSize) poc_vol2 = Closes[0][0];
					vPoc_list[CurrentBar]=poc_price2;
					//------------
					VPOC[0] = poc_price2;
					//------------										
				}
				// выводим надписи над свечками - потом закомментируем!!!
				//DrawText("volByTick"+CurrentBars[0], volBarByTicks.ToString(), 0, High[0] + 5*TickSize, Color.White);
				//DrawText("volByTick"+CurrentBars[0], CurrentBars[0].ToString(), 0, High[0] + 5*TickSize, Color.White);
				
				//-----------------
				#region INTERNAL_VPOC_of_BAR
				//===========================================
				// наш встроенный "индикатор" ПОКов для баров
				//===========================================
				// Это не индикатор - мы просто проверяем нужные паттерны и складываем их в соответствующие "копилки"
				// А поскольку они включаться и отключаться могут потом только в реалтайме,
				// то и рисовать их имеет смысл ТОЛЬКО там и ТОЛЬКО если включены
				// Соответсвенно, чтобы потом вывести те сигналы, которые были на истории - надо будет в цикле их все показать
				
				
				//------------
				// если надо что-то конкретное - то уже именно ЭТО ищем и складываем в собственные "копилки"
				Check_Naked_VPOCs();
				// выводить же все это на график будут соответствующие функции - для истории Show_Naked_VPOCS(true/false) и тому подобные
				// а для реалтайма предусмотрен вывод и внутри функций ЧЕК
				//------------	
				
				// Если включены ВСЕ - то нам тоже отдельной копилки не надо - просто выводим все имеющиеся, но уже ОСНОВНЫМ цветом
				// НО! эту основную копилку же надо заполнять?! Да это наш VPOC[0] - заполняется уже выше!!!
				// Это на истории... в реалтайме же будем делать так:
				if(showAllVPOCs && !Historical) DrawDot(CurrentBar.ToString() + "vpocDot", false, 0, VPOC[0], myVPOCsColor);
				// и эта часть ВСЕГДА ПОСЛЕДНЯЯ!!!
				
				#endregion INTERNAL_VPOC_OF_BAR
			}
			/**/
			#endregion MAKE_ALL_LADDERS
				
			// Рассчет параметров выделенных диапазонов - производится ТОЛЬКО при создании и/или изменении диапазонов
			// НО! ОДИН РАЗ В БАР НАДО ПЕРЕСЧИТЫВАТЬ ВСЕ ОФФСЕТЫ!!!
			// Так как левые и правые границы заданы в БАРАХ - то они стоят на месте, все ок. Но те величины, которые зависят от смещения
			// маркеры, запись, чтение - начинают давать неверные результаты
			if(!Historical && BarsInProgress==0 && FirstTickOfBar && CurrentBar > 10)
			{
				for(int num = 1; num<=4; num++)
				{
					R[num].leftOffset = Bars.Count - 1 - R[num].leftBar;
					R[num].rightOffset = Bars.Count - 1 - R[num].rightBar;
					R[num].extVpocOffset = Bars.Count - 1 - R[num].extVpocBar;
				}
			}
			
        }
		#endregion ON_BAR_UPDATE
		
		#region Mouse_Events
				
		#region MouseDOWN
		private void chart_MouseDown (object sender, MouseEventArgs e)
		{
			try {
				if (e.Button == MouseButtons.Left)
				{
					// Нажата левая кнопка мышки, но без горячих клавиш. Значит новый диапазон мы точно не создаем.
					// Но, возможно, что-то делаем с уже имеющимися. Надо проверить!
					
					if ((Control.ModifierKeys & Keys.Control) == 0) {
						// Работаем с имеющимся диапазоном или выходим ни с чем, так как кликнули без Контрол'а
						
						#region NoCtrlClick
						//Print("not Ctrl");
						// кликнули на зоне закрытия и можем удалить ЭТО
						status = GetActionFromMouse(e.X, e.Y);
						
						switch(status) {
							case StatusType.CloseRange1:
								RemoveRangeByNum(1);
								break;
							case StatusType.CloseRange2:
								RemoveRangeByNum(2);
								break;
							case StatusType.CloseRange3:
								RemoveRangeByNum(3);
								break;
							case StatusType.CloseRange4:
								RemoveRangeByNum(4);
								break;
							default:
								// это типа статуса "ничего"
								break;
						}
						rangesChanged = true;	// ну просто я хочу, чтобы по клику все перерисовывалось!!!
						#endregion NoCtrlClick
						
					} else {
						// Создаем новый диапазон, так как кликнули с Контрол'ом

						#region CtrlClick
						cursorX = Bars.Count - 1 - GetBarFromX(e.X);
						if(cursorX > Bars.Count - initWidth - 1) {	// следим, чтобы не зайти ЗА ПРАВЫЙ КРАЙ графика
							cursorX = Bars.Count - initWidth - 1;
						}							
						
						// пока тупо предусмотрено ВСЕГО ЧЕТЫРЕ диапазона (возможные два ПОКа сверху от текущей цены и два снизу - но можно и по-другому как угодно)
						nowRanges += 1;
						if(nowRanges > 4) {
							nowRanges = 4;
						}
						curCreatedRange += 1;
						if(curCreatedRange > 4) curCreatedRange = 1;

						if(curCreatedRange == 1) {
							curSelectedRange = 1;
							R[1].rangeColor		= rangeColor1;
						}
						if(curCreatedRange == 2) {
							curSelectedRange = 2;
							R[2].rangeColor		= rangeColor2;
						}
						if(curCreatedRange == 3) {
							curSelectedRange = 3;
							R[3].rangeColor		= rangeColor3;
						}
						if(curCreatedRange == 4) {
							curSelectedRange = 4;
							R[4].rangeColor		= rangeColor4;
						}
						status=StatusType.NotMoving;
						
						R[curCreatedRange].id				= curCreatedRange;
						R[curCreatedRange].leftBar			= cursorX;
						R[curCreatedRange].leftOffset		= Bars.Count - 1 - R[curCreatedRange].leftBar;
						R[curCreatedRange].leftTime			= Time[R[curCreatedRange].leftOffset];
						R[curCreatedRange].rightBar			= R[curCreatedRange].leftBar + initWidth - 1;
						R[curCreatedRange].rightOffset		= Bars.Count - 1 - R[curCreatedRange].rightBar;
						R[curCreatedRange].rightTime		= Time[R[curCreatedRange].rightOffset];
						R[curCreatedRange].extVpocBar		= 0;
						R[curCreatedRange].extVpocOffset	= 0;
						R[curCreatedRange].extVpocTime		= Time[R[curCreatedRange].extVpocOffset];
						R[curCreatedRange].rangeVol			= 0;
						R[curCreatedRange].VpocPrice		= 0;
						R[curCreatedRange].VpocType			= 0;
						R[curCreatedRange].VpocVol			= 0;
						R[curCreatedRange].rangeWidth		= R[curCreatedRange].rightBar - R[curCreatedRange].leftBar;
						R[curCreatedRange].profileWidth	= initWidth;			// профиль нового диапазона равен по ширине самому диапазону
						R[curCreatedRange].extVpocWork		= true;				// ИСТИНА!!!
						R[curCreatedRange].isExtended		= false;				// и расширенный режим изначально НЕ включен, если не сказано иное
						R[curCreatedRange].isPlotted		= false;				// ну конечно же новый профиль еще на нарисован, он ведь не готов...
						R[curCreatedRange].isReady			= false;				// только создали диапазон - рано считатеь его готовым, надо еще вычислять объемы, ПОКи, Профиль и так далее
						rangesChanged = true;										// все поменялось!!!
						DrawDiamond("curRange", false,R[curCreatedRange].leftOffset, R[curCreatedRange].Top, Color.Yellow);
						#endregion CtrlClick

					}
					this.ChartControl.ChartPanel.Invalidate();
					this.ChartControl.Refresh();
				}
			} catch (Exception exc) { /* можно что-то вставить */}
		}
		#endregion MouseDOWN
		
		#region MouseUP
		private void chart_MouseUp (object sender, MouseEventArgs e)
		{	
			try {
				if (e.Button == MouseButtons.Left)
				{
					// Если кнопку отпустили внутри какого-либо диапазона - считаем что по нему кликнули для активизаци
					if(status == StatusType.SelectRange1) curSelectedRange = 1;
					if(status == StatusType.SelectRange2) curSelectedRange = 2;
					if(status == StatusType.SelectRange3) curSelectedRange = 3;
					if(status == StatusType.SelectRange4) curSelectedRange = 4;
					DrawDiamond("curRange", false,R[curSelectedRange].leftOffset, R[curSelectedRange].Top, Color.Yellow);
					status=StatusType.NotMoving;
					
					this.ChartControl.ChartPanel.Invalidate();
					this.ChartControl.Refresh();
				}
			} catch (Exception exc) { /* можно что-то вставить */}
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
			// не удаляем - просто комментируем этот ПРИНТ... иногда полезно!!!
			//Print("mouseX=" + e.X + " | barFromX=" + (Bars.Count - 1 - GetBarFromX(e.X) + " | offsetFromX=" + GetBarFromX(e.X)  + " | mouseY= " + e.Y));

		}
		#endregion MouseUP
		
		#region MouseMOVE
		private void chart_MouseMove (object sender, MouseEventArgs e)
		{
			try	{
				if (e.Button == MouseButtons.None)
				{
					// Если просто двигаем мышкой БЕЗ НАЖАТИЯ НА КНОПКУ, то можем только в нужных местах менять внешний вид курсора					
					if(showMouseCursorChanges) GetActionFromMouse(e.X, e.Y); // ничего не делаем (кнопка ведь не нажата), а просто меняем курсор мышки внутри той функции
					
				} else if(e.Button == MouseButtons.Left)
				{
					// Если двигаем мышку с нажатой ЛЕВОЙ кнопкой - можем в зависимости от текущего статуса ДВИГАТЬ диапазоны или МЕНЯТЬ их размеры				
					cursorX = Bars.Count - 1 - GetBarFromX(e.X);
					
					int curWidth = 0;	// размер выбранного диапазона в барах
					
					// проверяем на ДВИЖЕНИЕ диапазона
					if(status == StatusType.MovingRange1 || status == StatusType.MovingRange2 || status == StatusType.MovingRange3 || status == StatusType.MovingRange4) {
						switch(status) {
							case StatusType.MovingRange1:
								// двигаем первый диапазон и делаем его активным
								curSelectedRange = 1;
								break;
							case StatusType.MovingRange2:
								// двигаем второй диапазон и делаем его активным
								curSelectedRange = 2;
								break;
							case StatusType.MovingRange3:
								// двигаем третий диапазон и делаем его активным
								curSelectedRange = 3;
								break;
							case StatusType.MovingRange4:
								// двигаем четвертый диапазон и делаем его активным
								curSelectedRange = 4;
								break;
							default:
								// тут все остальные статусы, которые мне пока пофиг...
								break;
						}
						if(cursorX > Bars.Count - R[curSelectedRange].rangeWidth - 1) {	// следим, чтобы не зайти ЗА ПРАВЫЙ КРАЙ графика
							cursorX = Bars.Count - R[curSelectedRange].rangeWidth - 1;
						}
						//Print("Cursor=" + cursorX);
						if(cursorX < R[curSelectedRange].leftBar - 50) cursorX = R[curSelectedRange].leftBar - 3 ; // следим за тем, чтобы левый край не мог сдвинуться сразу на гигантское расстоние!!!
						R[curSelectedRange].leftBar	= cursorX;
						R[curSelectedRange].leftOffset		= Bars.Count - 1 - R[curSelectedRange].leftBar;
						R[curSelectedRange].leftTime		= Time[R[curSelectedRange].leftOffset];
						R[curSelectedRange].rightBar	    = R[curSelectedRange].leftBar + R[curSelectedRange].rangeWidth;
						R[curSelectedRange].rightOffset		= Bars.Count - 1 - R[curSelectedRange].rightBar;
						R[curSelectedRange].rightTime		= Time[R[curSelectedRange].rightOffset];
						//Print("leftBar=" + R[1].leftBar + " | rightBar=" + R[1].rightBar);
						
						// не забываем при каждом движении сбрасывать extVPOC в ноль - иначе он не будет пересчитан!!!
						R[curSelectedRange].extVpocBar = 0;
						R[curSelectedRange].isReady	= false; // только сдвинули диапазон - надо заново вычислять
					}
					
					// проверяем на РАСШИРЕНИЕ диапазона
					if(status == StatusType.ChangeRange1 || status == StatusType.ChangeRange2 || status == StatusType.ChangeRange3 || status == StatusType.ChangeRange4) {
						switch(status) {
							case StatusType.ChangeRange1:
								// меняем размер первого диапазона и делаем его активным
								curSelectedRange = 1;
								break;
							case StatusType.ChangeRange2:
								// меняем размер второго диапазона и делаем его активным
								curSelectedRange = 2;
								break;
							case StatusType.ChangeRange3:
								// меняем размер третьего диапазона и делаем его активным
								curSelectedRange = 3;
								break;							
							case StatusType.ChangeRange4:
								// меняем размер четвертого диапазона и делаем его активным
								curSelectedRange = 4;
								break;							
							default:
								// тут все остальные статусы, которые мне пока пофиг...
								break;
						}
						if(cursorX > Bars.Count - 1) {	// следим, чтобы не зайти ЗА ПРАВЫЙ КРАЙ графика
							cursorX = Bars.Count - 1;
						}
						R[curSelectedRange].rightBar	= cursorX;
						R[curSelectedRange].rightOffset	= Bars.Count - 1 - R[curSelectedRange].rightBar;
						R[curSelectedRange].rightTime	= Time[R[curSelectedRange].rightOffset];
						R[curSelectedRange].rangeWidth	= R[curSelectedRange].rightBar - R[curSelectedRange].leftBar;
						// только надо проследить, чтобы диапазон не стал меньше миниммального размера
						if(R[curSelectedRange].rangeWidth < initWidth - 1) {
							R[curSelectedRange].rightBar = R[curSelectedRange].leftBar + initWidth - 1;
							// и заново рассчитать ширину диапазона
							R[curSelectedRange].rangeWidth = R[curSelectedRange].rightBar - R[curSelectedRange].leftBar;
						}
						//Print("leftBar=" + R[1].leftBar + " | rightBar=" + R[1].rightBar);
						
						// не забываем при каждом движении сбрасывать extVPOC в ноль - иначе он не будет пересчитан!!!
						R[curSelectedRange].extVpocBar = 0;
						R[curSelectedRange].isReady	= false; // только сдвинули диапазон - надо заново вычислять
					}
					rangesChanged = !(R[1].isReady && R[2].isReady && R[3].isReady && R[4].isReady);		 // если хоть один не готов - значит все поменялось!!!
					//if(rangesChanged) Print("CHANGED: " + rangesChanged);
					DrawDiamond("curRange", false,R[curSelectedRange].leftOffset, R[curSelectedRange].Top, Color.Yellow);
					this.ChartControl.ChartPanel.Invalidate();
					this.ChartControl.Refresh();

				}
			} catch (Exception exc) { /* можно что-то вставить */}
		}
		#endregion MouseMOVE
		
		#endregion Mouse_Events

		#region HotKeys
		private void chart_KeyDown (object sender, KeyEventArgs e)// горячие клавиши
		{	
			// если не знаем как правильно записать код клавиши или вообще не знаем можно ли ее использовать (может она не отзывается)
			// просто раскомментируем следующую строчку и в окне вывода все сами увидим ;-)
			//Print("KeyCode: " + e.KeyCode.ToString());

			if(e.Control && e.KeyCode==Keys.F4)
			{
				// ничего не будем делать showMouseCursorChanges
			} else if(e.Shift && e.KeyCode==Keys.OemQuestion) {
				// Включаем или прячем ХЕЛП-СКРИН
				showHelpScreen = !showHelpScreen;
				Show_Help_Screen(showHelpScreen);
			} else if(e.Shift && e.KeyCode==Keys.D){
				// Включаем или прячем ДЕБАГ-СКРИН
				showDebugScreen = !showDebugScreen;
				Show_Debug_Screen(showDebugScreen);
			} else if(e.Shift && e.KeyCode==Keys.M){
				// Включаем или отключаем изменения мышиного курсора
				showMouseCursorChanges = !showMouseCursorChanges;
			} else if(e.Shift && e.KeyCode==Keys.P) {
				// Включаем или прячем ПРОФИЛЬ
				showProfile = !showProfile;
				R[1].isReady = false;
				R[2].isReady = false;
				R[3].isReady = false;
				R[4].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Shift && e.KeyCode==Keys.R) {
				// Включаем или прячем РЕЙНДЖ (диапазон)
				showRange = !showRange;
				R[1].isReady = false;
				R[2].isReady = false;
				R[3].isReady = false;
				R[4].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Shift && e.KeyCode==Keys.L){
				// Переключаем отображение ПРОФИЛЯ из режима прямоугольников в режим линий и обратно
				showRectangles = !showRectangles;
				R[1].isReady = false;
				R[2].isReady = false;
				R[3].isReady = false;
				R[4].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Shift && e.KeyCode==Keys.V) {
				// Включаем или отключаем расширение линии ПОКа для конкретного выбранного сейчас профиля!!!
				R[curSelectedRange].extVpocWork = !R[curSelectedRange].extVpocWork;
				R[1].isReady = false;
				R[2].isReady = false;
				R[3].isReady = false;
				R[4].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(!e.Alt && !e.Control && !e.Shift && e.KeyCode==Keys.Delete){
				if(nowRanges > 0) {
					//Print("DELETE");
					//Print("DEL before:  Now=" + nowRanges + " | Selected=" + curSelectedRange + " | Created=" + curCreatedRange);
					//Удаляем выделенный диапазон
					RemoveRangeByNum(curSelectedRange);
					//curSelectedRange --; // НЕ УМЕНЬШАЕМ ТУТ!!! все что надо уменьшилось в процессе удаления!!!
					if(curSelectedRange <= 0) curSelectedRange = nowRanges;
					//Print("DEL:  Now=" + nowRanges + " | Selected=" + curSelectedRange + " | Created=" + curCreatedRange);
				}
				
			} else if(!e.Alt && !e.Control && e.Shift && e.KeyCode==Keys.A){
				// Show ALL VPOCs of Bars - Показываем ВСЕ ПОКи баров!!! - Ну или прячем их!
				showAllVPOCs = !showAllVPOCs;
				if(showAllVPOCs) {
					showNakedVPOCs = false;
				}
				Show_ALL_VPOCs(showAllVPOCs);
			} else if(!e.Alt && !e.Control && e.Shift && e.KeyCode==Keys.N){
				// Show NAKED VPOCs of Bars - Показываем ГОЛЫЕ ПОКи баров!!! - Ну или прячем их!
				showNakedVPOCs = !showNakedVPOCs;
				if(showNakedVPOCs) {
					showAllVPOCs = false;
				}
				Show_NAKED_VPOCs(showNakedVPOCs);
			} else if(!e.Alt && !e.Control && !e.Shift && e.KeyCode==Keys.Oemcomma){
				// Двигаем ВЕСЬ ТЕКУЩИЙ ДИАПАЗОН ВЛЕВО
				//Print("LEFT");
				R[curSelectedRange].leftBar -= 1;
				if(R[curSelectedRange].leftBar <= 2) {
					// если вышли за левую часть графика - возвращаемся обратно и правый край диапазона даже не будем трогать
					R[curSelectedRange].leftBar = 2;
				} else {
					// если не вышли за левую часть графика - подтянем правый край диапазона вслед за левым
					R[curSelectedRange].rightBar -= 1;
				}
				// не забываем сбросить расширение VPOCа - иначе оно не будет перерасчитано!!!
				R[curSelectedRange].extVpocBar = 0;
				R[curSelectedRange].extVpocOffset = 0;
				
				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(!e.Alt && !e.Control && !e.Shift  && e.KeyCode==Keys.OemPeriod){
				// Двигаем ВЕСЬ ТЕКУЩИЙ ДИАПАЗОН ВПРАВО
				//showRange = !showRange;
				//Print("RIGHT");
				R[curSelectedRange].rightBar += 1;
				if(R[curSelectedRange].rightBar >= Bars.Count - 1) {
					// если вышли за правый край графика - вернем обратно и левую часть диапазона даже не будем трогать
					R[curSelectedRange].rightBar = Bars.Count - 1;
				} else {
					// если не вышли еще за правую часть графика - подтянем левый край диапазона вслед за правым краем
					R[curSelectedRange].leftBar += 1;
				}
				// не забываем сбросить расширение VPOCа - иначе оно не будет перерасчитано!!!
				R[curSelectedRange].extVpocBar = 0;
				R[curSelectedRange].extVpocOffset = 0;

				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Control && e.KeyCode==Keys.Oemcomma){
				// Двигаем ЛЕВУЮ границу текущего диапазона ВЛЕВО
				//Print("CTRL+LEFT");
				R[curSelectedRange].leftBar -= 1;
				if(R[curSelectedRange].leftBar <= 2) R[curSelectedRange].leftBar = 2;
				// не забываем сбросить расширение VPOCа - иначе оно не будет перерасчитано!!!
				R[curSelectedRange].extVpocBar = 0;
				R[curSelectedRange].extVpocOffset = 0;
				
				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Control && e.KeyCode==Keys.OemPeriod){
				// Двигаем ЛЕВУЮ границу текущего диапазона ВПРАВО
				//showRange = !showRange;
				//Print("CTRL+RIGHT");
				R[curSelectedRange].leftBar += 1;
				if(R[curSelectedRange].leftBar >= R[curSelectedRange].rightBar - 2) R[curSelectedRange].leftBar = R[curSelectedRange].rightBar - 2;
				// не забываем сбросить расширение VPOCа - иначе оно не будет перерасчитано!!!
				R[curSelectedRange].extVpocBar = 0;
				R[curSelectedRange].extVpocOffset = 0;

				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Shift && e.KeyCode==Keys.Oemcomma){
				// Двигаем ПРАВУЮ границу текущего диапазона ВЛЕВО
				//Print("SHIFT+LEFT");
				R[curSelectedRange].rightBar -= 1;
				if(R[curSelectedRange].rightBar <= R[curSelectedRange].leftBar + 2) R[curSelectedRange].rightBar = R[curSelectedRange].leftBar + 2;
				// а сам профиль НЕ ТРОГАЕМ только для ЭКСТЕНДЕД диапазонов, для остальных тянем его принудительно!!!
				if(R[curSelectedRange].isExtended) {
					// ничего не делаем!
				} else {
					// а тут очень даже делаем!!!
					R[curSelectedRange].profileWidth = R[curSelectedRange].rightBar - R[curSelectedRange].leftBar;
				}
				// не забываем сбросить расширение VPOCа - иначе оно не будет перерасчитано!!!
				R[curSelectedRange].extVpocBar = 0;
				R[curSelectedRange].extVpocOffset = 0;

				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Shift && e.KeyCode==Keys.OemPeriod){
				// Двигаем ПРАВУЮ границу текущего диапазона ВПРАВО
				//showRange = !showRange;
				//Print("SHIFT+RIGHT");
				R[curSelectedRange].rightBar += 1;
				//if(R[curSelectedRange].rightBar >= Bars.Count - 1) R[curSelectedRange].rightBar = Bars.Count - 1;
				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Alt && e.KeyCode==Keys.Oemcomma){
				// Меняем размер текущего профиля в сторону уменьшения
				//Print("ALT+МЕНЬШЕ");	// Alt+"<"
				R[curSelectedRange].profileWidth = Convert.ToInt32(R[curSelectedRange].profileWidth/extProfile);
				if(R[curSelectedRange].profileWidth <= R[curSelectedRange].rangeWidth) {
					R[curSelectedRange].profileWidth = R[curSelectedRange].rangeWidth;
					R[curSelectedRange].isExtended = false;
				}
				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();				
			} else if(e.Alt && e.KeyCode==Keys.OemPeriod){
				// Меняем размер текущего профиля в сторону увеличения
				//Print("ALT+БОЛЬШЕ");	// Alt+">"
				R[curSelectedRange].isExtended = true;
				R[curSelectedRange].profileWidth *= extProfile;
				//R[curSelectedRange].profileWidth += extProfile * R[curSelectedRange].rangeWidth;
				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();				
			} else if(!e.Alt && !e.Control && !e.Shift && e.KeyCode==Keys.Back){
				// Меняем номер ТЕКУЩЕГО диапазона для дальнейшего управления клавишами в сторону "справа-налево"
				//Print("BACKSPACE");
				curSelectedRange --;
				if(curSelectedRange <= 0) curSelectedRange = nowRanges;
				Show_Debug_Screen(showDebugScreen);
			//} else if(!e.Alt && !e.Control && !e.Shift && e.KeyCode==Keys.Tab){
			} else if((e.Control && e.KeyCode==Keys.Tab) || (e.KeyCode==Keys.Tab)){
				// Меняем номер ТЕКУЩЕГО диапазона для дальнейшего управления клавишами в сторону "слева-направо"
				//Print("TAB");
				curSelectedRange ++;
				if(curSelectedRange > Math.Min(nowRanges,4)) curSelectedRange = 1;  // ДА! Вот так! Нам же надо еще учесть РЕАЛЬНОЕ количество имеющихся профилей!!!
				Show_Debug_Screen(showDebugScreen);
			}


			// Выведем статус наших основных переключателей-хоткеев, чтобы было наглядно
			string statHotKeys = "";
			if(showAllVPOCs) {
				statHotKeys += "|A=on";
			} else {
				statHotKeys += "|A=off";
			}			
			if(showMouseCursorChanges) {
				statHotKeys += "|M=on";
			} else {
				statHotKeys += "|M=off";
			}
			if(showProfile) {
				statHotKeys += "|P=on";
			} else {
				statHotKeys += "|P=off";
			}
			if(showRange) {
				statHotKeys += "|R=on";
			} else {
				statHotKeys += "|P=off";
			}
			if(R[curSelectedRange].isExtended) {
				statHotKeys += "| -> EXTENDED <- |";
			} else {
				// ничего
			}			
			if(R[curSelectedRange].extVpocWork) {
				// ничего
			} else {
				statHotKeys += "| -> NO_EXT_VPOC <- |";
			}			

			DrawTextFixed("HotKeyStat", statHotKeys + "|\r\n\r\n", TextPosition.BottomLeft, Color.White, new Font("Arial", 7, FontStyle.Regular), Color.Transparent, Color.Transparent, 0);
			if(nowRanges > 0) DrawDiamond("curRange", false,R[curSelectedRange].leftOffset, R[curSelectedRange].Top, Color.Yellow);
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
	
		}
		#endregion HotKeys
				
		#region Plots/RangeVPOC
		//------------
		#region PLOT
		public override void Plot(Graphics graphics, Rectangle bounds, double min, double max)	
		{	
			//Print("@@@");
			// будем пересчитывать нужные нам вещи ТОЛЬКО если чарт двигался или масштабировался
			// или когда были изменены диапазоны - то есьт стала ИСТИНОЙ переменная rangesChanged
			
//			if((prevMinPrice == min) && (prevMaxPrice == max) && (prevBounds == bounds) && !rangesChanged) {
//				// ничего не изменилось - значит ВСЕ КООРДИНАТЫ остались прежними!
//				//Print("PLOT: OK!");
//				chartChanged = false;
//			} else {
//				// ну а тут все ясно - будем пересчитывать...
//				Print("PLOT: Rectangle=(x:" + bounds.X + " | y:" + bounds.Y + " | w:" + bounds.Width + " | h:" + bounds.Height+") - Price=(min:"+min+" | max:" + max + ")");
//				// и сохраним новые значения - для дальнейших проверок
//				prevBounds = bounds;
//				prevMinPrice = min;
//				prevMaxPrice = max;
//				chartChanged = true;
//			}
			//========================================
			// а дальше уже добавляем то, что нам надо
			//========================================
			// Перед тем, как рисовать диапазоны, ПОКи, профили и так далее - убедимся, что все данные рассчитаны и не требуют обновления
			//Print("PLOT -> RANGESCHANGED: " + rangesChanged);
			RecalculateVPOCs();
			PlotVPOCs();	// пока я вижу что он нужен только при удалении
			
			// Отрисовываем базовые вещи, только сам индикатор делаем прозрачным
			Plots[0].Pen.Color = Color.Transparent;
			base.Plot(graphics,bounds,min,max);	
			//Print("Plot:  Now=" + nowRanges + " | Selected=" + curSelectedRange + " | Created=" + curCreatedRange);
		} // END of Plot()
		//------------
		#endregion PLOT
		
		//------------
		#region PLOT_VPOCs
		public void PlotVPOCs() {
			if(!R[1].isPlotted) {ClearProfileByNum(1); ClearRangeByNum(1);}
			if(R[1].isReady && !R[1].isPlotted ) PlotsByNum(1); 
			if(!R[2].isPlotted ) {ClearProfileByNum(2); ClearRangeByNum(2);}
			if(R[2].isReady && !R[2].isPlotted ) PlotsByNum(2);
			if(!R[3].isPlotted ) {ClearProfileByNum(3); ClearRangeByNum(3);}
			if(R[3].isReady && !R[3].isPlotted ) PlotsByNum(3);
			if(!R[4].isPlotted ) {ClearProfileByNum(4); ClearRangeByNum(4);}
			if(R[4].isReady && !R[4].isPlotted ) PlotsByNum(4);
		}
		public void PlotsByNum(int num) {
			if(R[num].leftBar != 0 && R[num].rightBar != 0) {
				// Если у нас уже есть УКАЗАННЫЙ по номеру диапазон...
				// ОЧИЩАЕМ профиль и РИСУЕМ заново (если надо)
				ClearProfileByNum(num);
				if(showProfile) {
					PlotProfileByNum(num);
				}
				
				// ОЧИЩАЕМ диапазон и РИСУЕМ заново (если надо)
				ClearRangeByNum(num);
				if(showRange) {
					PlotRangeByNum(num);
				}
			} else {
				// Если диапазон ЕЩЁ не задали - или УЖЕ удалили...
				// ОЧИЩАЕМ диапазон
				if(showRange) ClearRangeByNum(num);
			}

		}
		//------------
		#endregion PLOT_VPOCs
		
		//------------
		#region CLEAR_RANGE_by_NUM
		public void ClearRangeByNum(int num) {				
			// Удаляем весь рейндж (диапазон)
			RemoveDrawObject("MyRangeVpoc" + num);
			RemoveDrawObject("volRange" + num);
			RemoveDrawObject("VpocPrice" + num);
			RemoveDrawObject("VpocRangeLine" + num);
			RemoveDrawObject("VpocRangeLineExt" + num);
			RemoveDrawObject("MyRangeVpocBtn" + num);
		}  // END of ClearRangeByNum()
		//------------
		#endregion CLEAR_RANGE_by_NUM
					
		//------------
		#region PLOT_RANGE_by_NUM
		public void PlotRangeByNum(int num) {				
			// Сначала создадим нужные нам текстовые строки-названия
			string	myRangeSummaryStr = "volRange"+num;
			string	myRangePriceStr = "VpocPrice"+num;
			string	myRangeLineStr = "VpocRangeLine"+num;
			string	myRangeLineExtStr = "VpocRangeLineExt"+num;
			string	myRangeStr = "MyRangeVpoc"+num;
			string	myRangeBtnStr = "MyRangeVpocBtn"+num;
			
			// ПЕРЕСЧИТЫВАЕМ каждый раз СМЕЩЕНИЕ от правого края вместо конкретных номеров свечей - так удобнее рисовать и писать
			R[num].leftOffset = Bars.Count - 1 - R[num].leftBar;
			R[num].rightOffset = Bars.Count - 1 - R[num].rightBar;				
			R[num].extVpocOffset = (R[num].extVpocBar == 0) ? 0 : Bars.Count - 1 - R[num].extVpocBar;
			
			// Если надо - выводим информацию
			if(showSummary) DrawText(myRangeSummaryStr, false, "Bars: " + R[num].rangeBars + "\nVpocVol: " + R[num].VpocVol.ToString() + "\nRangeVol: " + R[num].rangeVol.ToString(), R[num].leftOffset, R[num].Bottom - 2*TickSize, 0, summaryColor, new Font("Arial", summaryFontSize, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
			
			// И теперь (пока грубо) нарисуем "профиль"
			if(showPrice) DrawText(myRangePriceStr, false, R[num].VpocPrice.ToString() + " VPOC =>", R[num].leftOffset, R[num].VpocPrice, 0, priceColor, new Font("Arial", priceFontSize, FontStyle.Bold), StringAlignment.Far, Color.Transparent, Color.Transparent, 0);
			DrawLine(myRangeLineStr, false, R[num].leftOffset, R[num].VpocPrice, R[num].rightOffset, R[num].VpocPrice,  vpocLineColor, vpocLineStyle, vpocLineSise);
			
			if(useTrendColor) {
				if(R[num].VpocType == 1) {
					if(R[num].extVpocBar == 0) {
						DrawRay(myRangeLineExtStr, false, R[num].rightOffset, R[num].VpocPrice, 0, R[num].VpocPrice, vpocExtShortColor, vpocExtShortStyle, vpocExtShortSise);
					} else {
						DrawLine(myRangeLineExtStr, false, R[num].rightOffset, R[num].VpocPrice, R[num].extVpocOffset, R[num].VpocPrice, vpocExtShortColor, vpocExtShortStyle, vpocExtShortSise);
					}
				} else if(R[num].VpocType == -1) {
					if(R[num].extVpocBar == 0) {
						DrawRay(myRangeLineExtStr, false, R[num].rightOffset, R[num].VpocPrice, 0, R[num].VpocPrice, vpocExtLongColor, vpocExtLongStyle, vpocExtLongSise);
					} else {
						DrawLine(myRangeLineExtStr, false, R[num].rightOffset, R[num].VpocPrice, R[num].extVpocOffset, R[num].VpocPrice, vpocExtLongColor, vpocExtLongStyle, vpocExtLongSise);
					}
				} else {
					RemoveDrawObject(myRangeLineExtStr);
				}
			} else {
				if(R[num].VpocType != 0) {
					if(R[num].extVpocBar == 0) {
						DrawRay(myRangeLineExtStr, false, R[num].rightOffset, R[num].VpocPrice, 0, R[num].VpocPrice, vpocLineColor, vpocLineStyle, vpocLineSise);
					} else {
						DrawLine(myRangeLineExtStr, false, R[num].rightOffset, R[num].VpocPrice, R[num].extVpocOffset, R[num].VpocPrice, vpocLineColor, vpocLineStyle, vpocLineSise);
					}
				} else {
					RemoveDrawObject(myRangeLineExtStr);
				}
			}
			
			// Ну и в самом конце можно нарисовать рамку с фоном
			DrawRectangle(myRangeStr, true, R[num].leftOffset, R[num].Bottom, R[num].rightOffset, R[num].Top, borderColor, R[num].rangeColor, 1);
			//DrawRectangle(myRangeBtnStr, true, R[num].rightOffset + 2, R[num].Top - 2*TickSize, R[num].rightOffset, R[num].Top, borderColor, borderColor, 200);
			DrawSquare(myRangeBtnStr, false, R[num].rightOffset, R[num].Top, borderColor);
			//if(debugPerformance) Print("Plot-" + num);
			R[num].isPlotted = true; 	// укажем, что мы уже все отрисовали!!!
			DrawDiamond("curRange", false,R[curSelectedRange].leftOffset, R[curSelectedRange].Top, Color.Yellow);
		}  // END of PlotRangeByNum()
		//------------
		#endregion PLOT_RANGE_by_NUM

		//------------
		#region CLEAR_PROFILE_by_NUM
		public void ClearProfileByNum(int num) {				
			// очищаем хранилище
			R[num].bars_price.Clear();
			// удаляем возможные куски старого профиля (надо подобрать количество или не надо???)
			for(int i = 0; i<histogramLines; i++)
			{
				RemoveDrawObject("Profile_"+ num+"_"+i);
			}
		}  // END of ClearProfileByNum()
		//------------
		#endregion CLEAR_PROFILE_by_NUM					
		
		//------------
		#region PLOT_PROFILE_by_NUM
		public void PlotProfileByNum(int num) {
			// Сначала создадим нужные нам текстовые строки-названия
			string	myRangeSummaryStr = "volRange"+num;
			string	myRangePriceStr = "VpocPrice"+num;
			string	myRangeLineStr = "VpocRangeLine"+num;
			string	myRangeLineExtStr = "VpocRangeLineExt"+num;
			string	myRangeStr = "MyRangeVpoc"+num;
			string	myRangeBtnStr = "MyRangeVpocBtn"+num;
			
			// ПЕРЕСЧИТЫВАЕМ каждый раз СМЕЩЕНИЕ от правого края вместо конкретных номеров свечей - так удобнее рисовать и писать
			R[num].leftOffset = Bars.Count - 1 - R[num].leftBar;
			R[num].rightOffset = Bars.Count - 1 - R[num].rightBar;				
			R[num].extVpocOffset = (R[num].extVpocBar == 0) ? 0 : Bars.Count - 1 - R[num].extVpocBar;
		
			// гм... профиль... у нас есть ладдер с объемами - надо его как-то преобразовать в ладдер с процентами
			// нет, поскольку рисовать будем с шагом в один бар - то нужен ладдер с количеством баров в качестве "размера"
			// При этом еще понадобится новая переменная кроме минимального размера диапазона - минимальный размер профиля
			// (чтобы на мелких диапазонах профиль мог выходить за рамки!)
			// Для этого была создана переменная R[num].profileWidth в каждом диапазоне, изначально равная стартовой ширине
			// значит теперь надо или приравнивать ее ТЕКУЩЕЙ ширине диапазона, или же увеличивать по требованию пользователя
			
			if(R[num].isExtended) {
				// если включен режим расширенного профиля именно для данного диапазона
				// то применяем текущие размеры профиля - меняются через клавиши АЛЬТ+"<" и АЛЬТ+">" с заданным в настройках коэффициентом увеличения
				R[num].profileWidth = R[num].profileWidth; // попросту говоря - ничего не делаем				
			} else {
				// если такой режим НЕ ВКЛЮЧЕН  - каким бы ни был задан профиль, приравниваем его к ширине диапазона
				R[num].profileWidth = R[num].rangeWidth;
			}
			
			// Так... у нас объем на ПОКе - это будет 100% нашего профиля. И количество баров в профиле - тоже 100% профиля...
			// То есть можем узнать, сколько ОБЬЕМА приходится на "один БАР по ширине" а не по фактическому обьему этого конкретного бара
			// Просто делим объем на количество баров...
			double inOneBar = R[num].VpocVol / R[num].profileWidth;
			int counter = 0;
			foreach(KeyValuePair<double, double> kvp in R[num].vol_price.OrderByDescending(key => key.Value))
			{
				// этот счетчик поможет потом удалить куски старого профиля при изменении размеров или местоположения
				counter ++;
				// так... у нас объем на ПОКе - это будет 100% нашего профиля
				double profileLineWidth = kvp.Value / inOneBar;
				int profileLineBars = Convert.ToInt16(profileLineWidth);
				int profileLineOffset = R[num].leftOffset - profileLineBars;
				//R[num].bars_price.Add(kvp.Key, profileLineWidth);
				R[num].bars_price.Add(kvp.Key, profileLineOffset); // именно это СМЕЩЕНИЕ и будем хранить, чтобы не пересчитывать постоянно!!!
				// и можем теперь это отобразить
				if(kvp.Key == R[num].VpocPrice) {
					// вот так мы рисуем линию ПОКа в профиле
					if(showRectangles) {
						DrawRectangle("Profile_"+ num+"_"+counter, false, R[num].leftOffset, kvp.Key, profileLineOffset, kvp.Key - TickSize, Color.Transparent, vpocLineColor, 4);
					} else {
						DrawLine("Profile_"+ num+"_"+counter, false, R[num].leftOffset, kvp.Key, profileLineOffset, kvp.Key, vpocLineColor, DashStyle.Solid, 4);
					}
				} else {
					// а вот так рисуем остальные линии гистограммы профиля
					if(showRectangles) {
						DrawRectangle("Profile_"+ num+"_"+counter, false, R[num].leftOffset, kvp.Key, profileLineOffset, kvp.Key - TickSize, vpocProfileColor, vpocProfileColor, 3);
					} else {
						DrawLine("Profile_"+ num+"_"+counter, false, R[num].leftOffset, kvp.Key, profileLineOffset, kvp.Key, vpocProfileColor, DashStyle.Solid, 2);
					}
				}
				//Print("P="+kvp.Key + " V="+kvp.Value + "B="+profileLineBars + " O="+profileLineOffset);
			}
		}
		#endregion PLOT_PROFILE_by_NUM
		
		
		//------------		
		#endregion Plots/RangeVPOC

		#region OnTermination
		protected  override void OnTermination()
        {
			//SaveProfilePosition();
			
			if (this.ChartControl != null)
			{	
				if (mouseDownH != null)
				{	
					this.ChartControl.ChartPanel.MouseDown -= mouseDownH;
			   		mouseDownH = null;
				}
				if (mouseUpH != null)
				{	
					this.ChartControl.ChartPanel.MouseUp -= mouseUpH;
			   		mouseUpH = null;
				}
				if (mouseMoveH != null)
				{	
					this.ChartControl.ChartPanel.MouseMove -= mouseMoveH;
			   		mouseMoveH = null;
				}
				if (keyEvtH != null)
				{	
					this.ChartControl.ChartPanel.KeyDown -= keyEvtH;
				   	keyEvtH=null;
				}
			}
			vol_price.Clear();
			work_vol_price.Clear();
			R[0].vol_price.Clear();
			R[1].vol_price.Clear();
			R[2].vol_price.Clear();
			R[3].vol_price.Clear();
			R[4].vol_price.Clear();
			R[0].work_vol_price.Clear();
			R[1].work_vol_price.Clear();
			R[2].work_vol_price.Clear();
			R[3].work_vol_price.Clear();
			R[4].work_vol_price.Clear();
			R[0].bars_price.Clear();
			R[1].bars_price.Clear();
			R[2].bars_price.Clear();
			R[3].bars_price.Clear();
			R[4].bars_price.Clear();
		}
		#endregion		

		#region HELP_SCREEN
		//-----------------------
		public void Show_Help_Screen(bool show) {
			
			Color ColorNeutral=Color.FromArgb(255,ChartControl.BackColor.R,ChartControl.BackColor.G,ChartControl.BackColor.B);			
			Font MonoSpasedFont = new Font("Courier", 10, FontStyle.Bold);
			
			string msg="";
				msg += "\r\n  CTRL+CLICK(mouse)         Create new RANGE with PROFILE";
				msg += "\r\n                            (total available 4 ranges)\r\n";
				msg += "\r\n  CLICK or CLICK+DRAG       All operations: Select, Move, Resize, Delete\r\n";
				msg += "\r\n  **********************";
				msg += "\r\n  List of HOT-KEY Codes";
				msg += "\r\n  ********************** \r\n";			
				msg += "\r\n  TAB or BACKSPACE          Circulate Selected Range Mark";
				msg += "\r\n  DELETE                    Remove selected (active) range\r\n";			
				msg += "\r\n  \"<\" or \">\"                MOVE Selected Range";
				msg += "\r\n  SHIFT+\"<\" or SHIFT+\">\"    CHANGE Right Border of Selected Range";
				msg += "\r\n  CTRL+\"<\" or CTRL+\">\"      CHANGE Left Border of Selected Range";
				msg += "\r\n  ALT+\"<\" or ALT+\">\"        EXTEND PROFILE Size or return to back\r\n";
				msg += "\r\n  Shift+\"P\"                 Toggle Show|Hide PROFILE";
				msg += "\r\n  Shift+\"R\"                 Toggle Show|Hide RANGE";
				msg += "\r\n  Shift+\"L\"                 Toggle Lines|Rectangles Profile mode";
				msg += "\r\n\r\n  ***   ! BONUS !   ***";
			    msg += "\r\n  Shift+\"A\"                 Show|Hide All VPOCs on each bar";
				msg += "\r\n  Video Review	              http://youtu.be/sQHUWKi71Lc";
				msg += "\r\n  Video Stress-Test         https://youtu.be/wETPwWeU1w0";
				msg += "\r\n\r\n";
			
			if(show) {
				DrawTextFixed("helpScreen", msg, TextPosition.Center, helpFontColor, MonoSpasedFont, Color.Transparent, ColorNeutral, 7);
			} else {
				RemoveDrawObject("helpScreen");
			}
		}
		public void Show_Debug_Screen(bool show) {
			
			Color ColorNeutral=Color.FromArgb(255,ChartControl.BackColor.R,ChartControl.BackColor.G,ChartControl.BackColor.B);			
			Font MonoSpasedFont = new Font("Courier", 10, FontStyle.Bold);
			
			string msg="";
				msg += "\r\n  ***********";
				msg += "\r\n  Debug Info";
				msg += "\r\n  ************ \r\n";			
				msg += "\r\n  LeftBar          " + R[curSelectedRange].leftBar;
				msg += "\r\n  LeftOffset       " + R[curSelectedRange].leftOffset;
				msg += "\r\n  LeftTime         " + R[curSelectedRange].leftTime;
				msg += "\r\n  RightBar         " + R[curSelectedRange].rightBar;
				msg += "\r\n  RightOffset      " + R[curSelectedRange].rightOffset;
				msg += "\r\n  RightTime        " + R[curSelectedRange].rightTime;
				msg += "\r\n  VPOC Price       " + R[curSelectedRange].VpocPrice;
				msg += "\r\n  extVpocBar       " + R[curSelectedRange].extVpocBar;
				msg += "\r\n  extVpocOffset    " + R[curSelectedRange].extVpocOffset;
				msg += "\r\n  extVpocTime      " + R[curSelectedRange].extVpocTime;
				msg += "\r\n  Range Width      " + R[curSelectedRange].rangeWidth;
				msg += "\r\n  Profile Width    " + R[curSelectedRange].profileWidth;
			
			if(show) {
				DrawTextFixed("debugScreen", msg, TextPosition.TopLeft, helpFontColor, MonoSpasedFont, Color.Transparent, ColorNeutral, 7);
			} else {
				RemoveDrawObject("debugScreen");
			}
		}
		//-----------------------
		#endregion HELP_SCREEN
		
        #region PROPERTIES
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public DataSeries VPOC
		{
			get { return Values[0]; }
		}
		//[XmlIgnore()]
		[Description("Изначальная ширина диапазона для ПОКа")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("01. Init Width")]
		public int InitWidth
		{
			get { return initWidth; }
			set { initWidth = Math.Max(2, value); 
			} 	
		}
		[Description("Коэффициент увеличения ширины профиля для расширенного режима (включается и отключается через <ЗВЕЗДОЧКУ> на дополнительной цифровой клавиатуре)")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("02. Init Width")]
		public int ExtProfile
		{
			get { return extProfile; }
			set { extProfile = Math.Max(2, value); 
			} 	
		}
		//[XmlIgnore()]
		[Description("Выводить ли ПОД диапазонами сборную информацию (количество баров, объем всего диапазона, объем ПОКа")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("03. Show Summary")]
		public bool ShowSummary
		{
			get { return showSummary; }
			set { showSummary = value; } 	
		}
		//[XmlIgnore()]
		[Description("Размер шрифта для ИНФО-текста")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("04. Summary Font Size")]
		public int SummaryFontSize
		{
			get { return summaryFontSize; }
			set { summaryFontSize = Math.Max(2, value); 
			} 	
		}
		//[XmlIgnore()]
		[Description("Выводить ли СЛЕВА от диапазонов цену ПОКа")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("05. Show Price")]
		public bool ShowPrice
		{
			get { return showPrice; }
			set { showPrice = value; } 	
		}
		//[XmlIgnore()]
		[Description("Размер шрифта для цены ПОКа")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("06. Price Font Size")]
		public int PriceFontSize
		{
			get { return priceFontSize; }
			set { priceFontSize = Math.Max(2, value); 
			} 	
		}
		//[XmlIgnore()]
		[Description("рисовать ли ПОКи на Лонг и Шорт разными цветами")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("07. Use Trend Color")]
		public bool UseTrendColor
		{
			get { return useTrendColor; }
			set { useTrendColor = value; } 	
		}
		//[XmlIgnore()]
        [Description("Цвет первого диапазона")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("01. Range-1 Color")]
        public Color RangeColor1
        {
            get { return rangeColor1; }
            set { rangeColor1 = value; }
        }
		[Browsable(false)]
		public string RangeColor1Serialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(RangeColor1); }
			set { RangeColor1 = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет второго диапазона")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("02. Range-2 Color")]
        public Color RangeColor2
        {
            get { return rangeColor2; }
            set { rangeColor2 = value; }
        }
		[Browsable(false)]
		public string RangeColor2Serialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(RangeColor2); }
			set { RangeColor2 = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет третьего диапазона")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("03. Range-3 Color")]
        public Color RangeColor3
        {
            get { return rangeColor3; }
            set { rangeColor3 = value; }
        }
		[Browsable(false)]
		public string RangeColor3Serialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(RangeColor3); }
			set { RangeColor3 = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет четвертого диапазона")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("04. Range-4 Color")]
        public Color RangeColor4
        {
            get { return rangeColor4; }
            set { rangeColor4 = value; }
        }
		[Browsable(false)]
		public string RangeColor4Serialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(RangeColor4); }
			set { RangeColor4 = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет линий гистограммы ПРОФИЛЯ (кроме линии ПОКа)")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("05. PROFILE Color")]
        public Color VpocProfileColor
        {
            get { return vpocProfileColor; }
            set { vpocProfileColor = value; }
        }
		[Browsable(false)]
		public string VpocProfileColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(VpocProfileColor); }
			set { VpocProfileColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет текста ПОД диапазонами (ИНФО, если его вообще показывать)")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("06. Summary Color")]
        public Color SummaryColor
        {
            get { return summaryColor; }
            set { summaryColor = value; }
        }
		[Browsable(false)]
		public string SummaryColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(summaryColor); }
			set { summaryColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет текста СЛЕВА от диапазонов (ЦЕНА ПОКа, если её вообще показывать)")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("07. Price Color")]
        public Color PriceColor
        {
            get { return priceColor; }
            set { priceColor = value; }
        }
		[Browsable(false)]
		public string PriceColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(priceColor); }
			set { priceColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет текста ХЕЛП-СКРИНа")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("08. HelpScreen Font Color")]
        public Color HelpFontColor
        {
            get { return helpFontColor; }
            set { helpFontColor = value; }
        }
		[Browsable(false)]
		public string HelpFontColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(HelpFontColor); }
			set { HelpFontColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
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
        private _Savos_Range_Profile_v002[] cache_Savos_Range_Profile_v002 = null;

        private static _Savos_Range_Profile_v002 check_Savos_Range_Profile_v002 = new _Savos_Range_Profile_v002();

        /// <summary>
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        public _Savos_Range_Profile_v002 _Savos_Range_Profile_v002(int extProfile, int initWidth, int priceFontSize, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            return _Savos_Range_Profile_v002(Input, extProfile, initWidth, priceFontSize, showPrice, showSummary, summaryFontSize, useTrendColor);
        }

        /// <summary>
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        public _Savos_Range_Profile_v002 _Savos_Range_Profile_v002(Data.IDataSeries input, int extProfile, int initWidth, int priceFontSize, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            if (cache_Savos_Range_Profile_v002 != null)
                for (int idx = 0; idx < cache_Savos_Range_Profile_v002.Length; idx++)
                    if (cache_Savos_Range_Profile_v002[idx].ExtProfile == extProfile && cache_Savos_Range_Profile_v002[idx].InitWidth == initWidth && cache_Savos_Range_Profile_v002[idx].PriceFontSize == priceFontSize && cache_Savos_Range_Profile_v002[idx].ShowPrice == showPrice && cache_Savos_Range_Profile_v002[idx].ShowSummary == showSummary && cache_Savos_Range_Profile_v002[idx].SummaryFontSize == summaryFontSize && cache_Savos_Range_Profile_v002[idx].UseTrendColor == useTrendColor && cache_Savos_Range_Profile_v002[idx].EqualsInput(input))
                        return cache_Savos_Range_Profile_v002[idx];

            lock (check_Savos_Range_Profile_v002)
            {
                check_Savos_Range_Profile_v002.ExtProfile = extProfile;
                extProfile = check_Savos_Range_Profile_v002.ExtProfile;
                check_Savos_Range_Profile_v002.InitWidth = initWidth;
                initWidth = check_Savos_Range_Profile_v002.InitWidth;
                check_Savos_Range_Profile_v002.PriceFontSize = priceFontSize;
                priceFontSize = check_Savos_Range_Profile_v002.PriceFontSize;
                check_Savos_Range_Profile_v002.ShowPrice = showPrice;
                showPrice = check_Savos_Range_Profile_v002.ShowPrice;
                check_Savos_Range_Profile_v002.ShowSummary = showSummary;
                showSummary = check_Savos_Range_Profile_v002.ShowSummary;
                check_Savos_Range_Profile_v002.SummaryFontSize = summaryFontSize;
                summaryFontSize = check_Savos_Range_Profile_v002.SummaryFontSize;
                check_Savos_Range_Profile_v002.UseTrendColor = useTrendColor;
                useTrendColor = check_Savos_Range_Profile_v002.UseTrendColor;

                if (cache_Savos_Range_Profile_v002 != null)
                    for (int idx = 0; idx < cache_Savos_Range_Profile_v002.Length; idx++)
                        if (cache_Savos_Range_Profile_v002[idx].ExtProfile == extProfile && cache_Savos_Range_Profile_v002[idx].InitWidth == initWidth && cache_Savos_Range_Profile_v002[idx].PriceFontSize == priceFontSize && cache_Savos_Range_Profile_v002[idx].ShowPrice == showPrice && cache_Savos_Range_Profile_v002[idx].ShowSummary == showSummary && cache_Savos_Range_Profile_v002[idx].SummaryFontSize == summaryFontSize && cache_Savos_Range_Profile_v002[idx].UseTrendColor == useTrendColor && cache_Savos_Range_Profile_v002[idx].EqualsInput(input))
                            return cache_Savos_Range_Profile_v002[idx];

                _Savos_Range_Profile_v002 indicator = new _Savos_Range_Profile_v002();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.ExtProfile = extProfile;
                indicator.InitWidth = initWidth;
                indicator.PriceFontSize = priceFontSize;
                indicator.ShowPrice = showPrice;
                indicator.ShowSummary = showSummary;
                indicator.SummaryFontSize = summaryFontSize;
                indicator.UseTrendColor = useTrendColor;
                Indicators.Add(indicator);
                indicator.SetUp();

                _Savos_Range_Profile_v002[] tmp = new _Savos_Range_Profile_v002[cache_Savos_Range_Profile_v002 == null ? 1 : cache_Savos_Range_Profile_v002.Length + 1];
                if (cache_Savos_Range_Profile_v002 != null)
                    cache_Savos_Range_Profile_v002.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cache_Savos_Range_Profile_v002 = tmp;
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
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator._Savos_Range_Profile_v002 _Savos_Range_Profile_v002(int extProfile, int initWidth, int priceFontSize, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            return _indicator._Savos_Range_Profile_v002(Input, extProfile, initWidth, priceFontSize, showPrice, showSummary, summaryFontSize, useTrendColor);
        }

        /// <summary>
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        public Indicator._Savos_Range_Profile_v002 _Savos_Range_Profile_v002(Data.IDataSeries input, int extProfile, int initWidth, int priceFontSize, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            return _indicator._Savos_Range_Profile_v002(input, extProfile, initWidth, priceFontSize, showPrice, showSummary, summaryFontSize, useTrendColor);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator._Savos_Range_Profile_v002 _Savos_Range_Profile_v002(int extProfile, int initWidth, int priceFontSize, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            return _indicator._Savos_Range_Profile_v002(Input, extProfile, initWidth, priceFontSize, showPrice, showSummary, summaryFontSize, useTrendColor);
        }

        /// <summary>
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        public Indicator._Savos_Range_Profile_v002 _Savos_Range_Profile_v002(Data.IDataSeries input, int extProfile, int initWidth, int priceFontSize, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator._Savos_Range_Profile_v002(input, extProfile, initWidth, priceFontSize, showPrice, showSummary, summaryFontSize, useTrendColor);
        }
    }
}
#endregion
