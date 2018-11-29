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
     /// Натягиваемый и передвигаемый по графику 4-диапазонный индикатор ПОКов и ПРОФИЛЕЙ. Почему всего 4? Мне хватает...
     /// </summary>
    [Description("Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!")]
    public class _Savos_Range_Profile_v003 : Indicator
    {

		#region VARIABLES
		//******************************************************************************************************************
		
		// === СЕКЦИЯ "для Валерки" ;-) === //
		private	Color raysColor			= Color.Cyan;		// цвет лучей на ПОКах (когда лучи включены через |Ctrl-Shift-G|
		private int raysLineSize		= 3;				// толщина линии лучей
		private DashStyle raysLineStyle	= DashStyle.Dot;	// и стиль этой линии
		// === СЕКЦИЯ "для Валерки" ;-) === //
		
		
		// === СЕКЦИЯ ДЕБАГА === //
		//---------------------------
		private bool debugPerformance 		= true;				// проверять производительность в РеКалк и в ПЛОТ?
		private bool printOnce				= false;			// печатать при первом проходе
		private bool saveread				= false;			// сохранять ли профили при перерисовке???
		//---------------------------
		// === СЕКЦИЯ ДЕБАГА === //
		
		
		// === СЕКЦИЯ ГОРЯЧИХ КЛАВИШ === //
		//-----------------------------------
		private bool showMouseCursorChanges = true;				// SHIFT+"M" 	-> показывать или прятать изменения курсора при движении мышки на разных частях диапазона (тянуть, расширять, закрывать)
		private bool showStrongProfile		= false;			// SHIFT+"M" 	-> показывать или прятать в гистограмме профиля МЕЛКИЕ (потому и буква М) бары, которые меньше 10% от ширины профиля
		private bool showProfile			= true;				// SHIFT+"P" 	-> показывать или прятать ПРОФИЛЬ?
		private bool showRange				= true;				// SHIFT+"R" 	-> показывать или прятать сам ДИАПАЗОН?
		private bool showHelpScreen			= false;			// SHIFT+"?" 	-> показывать или прятать HELP-скрин
		private bool showExtProfile			= false;			// SHIFT+">""<" -> включать или отключать расширенный режим  для ПРОФИЛЯ? увеличивать и уменьшать эту ширину
		private bool showRectangles			= true;				// SHIFT+"L"	-> показывать ли прямоугольники ЛИНИЯМИ?
		private int curSelectedRange		= 0;				// "CTRL+TAB" и "BACKSPACE" - выбор текущего активного профиля по кругу в ту и другую сторону
		private bool showDebugScreen		= false;			// SHIFT+"D" 	-> показывать или прятать DEBUG-скрин
		private bool showRays				= false;			// по умолчанию рисуем ПОКи графическими  инструментами, но если надо кому-то - можем продублировать ЛУЧАМИ
		//-----------------------------------
		// === СЕКЦИЯ ГОРЯЧИХ КЛАВИШ === //
		
		
		// === НАСТРОЙКИ, ДОСТУПНЫЕ ПОЛЬЗОВАТЕЛЮ В ПАНЕЛИ СВОЙСТВ ИНДИКАТОРА === //
		//---------------------------------------------------------------------------
		private int initWidth 				= 9;				// сколько баров шириной будет диапазон профиля при создании
		private int extProfile				= 3;				// во сколько раз увеличить ширину профиля (не меняя диапазона) для расширенного режима
		private bool showSummary			= true;				// выводить ли ПОД диапазонами сборную информацию (количество баров, объем всего диапазона, объем ПОКа
		private	Color summaryColor			= Color.White;		// цвет этой надписи со сборной информацией
		private int summaryFontSize 		= 6;				// размер шрифта для этой надписи
		private bool showPrice				= true;				// выводить ли СЛЕВА от диапазонов цену ПОКа
		private int regTransparency			= 40;				// степень прозрачности цветного заполнения диапазонов
		private int profTransparency		= 200;				// степень прозрачности цветного заполнения гистограммы профиля
		private	Color priceColor			= Color.Yellow; 	// цвет этой надписи с ценой
		private int priceFontSize			= 8;				// размер шрифта для этой надписи
		private bool useTrendColor			= true;				// рисовать ли ПОКи на Лонг и Шорт разными цветами???
		private int histogramLines			= 10000;			// максимальное возможное количество уровней в профиле диапазона
		private int curCreatedRange			= 0;				// текущий создаваемый диапазон - ходит по кругу от 1 до 4
		private bool useNewPatternAlert		= false;			// проигрывать ли звуковой сигнал при начале новой свечки???
		private bool extVahVal				= true;			// расширять ли линии VAH и VAL за пределы выделенного диапазона (до самого правого края графика)???
		private int nowRanges				= 0;				// количество реально задействованных сейчас выделенных диапазонов
		private string instrument			= "";				// торгуемый на данном графике инструмент
		private int vpocLineSize			= 2;				// толщина ПОК-линии внутри диапазона
		private	Color vpocLineColor			= Color.Yellow; 	// цвет основной линии ПОК
		private DashStyle vpocLineStyle 	= DashStyle.Solid;	// и стиль этой линии
		private int vpocExtLongSize			= 2;				// толщина ПОК-линии снаружи диапазона - расширенной ПОК-линии на ЛОНГ
		private	Color vpocExtLongColor		= Color.Green;		// цвет расширенной ПОК-линии в Лонг
		private DashStyle vpocExtLongStyle	= DashStyle.Solid;	// и стиль этой линии
		private int vpocExtShortSize		= 2;				// толщина ПОК-линии снаружи диапазона - расширенной ПОК-линии на ШОРТ
		private	Color vpocExtShortColor		= Color.Red;		// цвет расширенной ПОК-линии в Шорт
		private DashStyle vpocExtShortStyle	= DashStyle.Solid;	// и стиль этой линии
		private Color vahLineColor			= Color.Cyan;		// цвет линии VAH
		private int vahLineSize				= 1;				// толщина линии VAH
		private DashStyle vahLineStyle 		= DashStyle.DashDot;// и стиль этой линии
		private Color valLineColor			= Color.Magenta;	// цвет линии VAL
		private int valLineSize				= 1;				// толщина линии VAL
		private DashStyle valLineStyle 		= DashStyle.DashDot;// и стиль этой линии
		private	Color borderColor			= Color.Yellow;		// цвет рамки вокруг диапазонов
		private int borderLineSize			= 4;				// толщина линии рамки
		private DashStyle borderLineStyle	= DashStyle.Solid;	// и стиль этой линии
		private Color rangeColor1			= Color.Tan;		// базовый увет фона для диапазона №1
		private Color rangeColor2			= Color.Yellow;		// базовый увет фона для диапазона №2
		private Color rangeColor3			= Color.Green;		// базовый увет фона для диапазона №3
		private Color rangeColor4			= Color.Blue;		// базовый увет фона для диапазона №4
		private Color vpocProfileColor		= Color.Gray;		// цвет гистограммы ПРОФИЛЯ диапазона вне ValueArea
		private Color vpocValueAreaColor	= Color.Tan;		// цвет гистограммы ПРОФИЛЯ диапазона внутри ValueArea
		private Color helpFontColor			= Color.White;		// цвет текста ХЕЛП-СКРИНА
		private Color btnActiveColor		= Color.White;		// цвет АКТИВИРОВАННОЙ "кнопки"
		private Color btnEnableColor		= Color.Black;		// цвет НЕ АКТИВНОЙ, НО ДОСТУПНОЙ для нажатия "кнопки"
		private Color btnDisableColor		= Color.Tan;		// цвет ВЫКЛЮЧЕННОЙ "кнопки", то есть и НЕ активной и НЕ доступной!
		//---------------------------------------------------------------------------
		// === НАСТРОЙКИ, ДОСТУПНЫЕ ПОЛЬЗОВАТЕЛЮ В ПАНЕЛИ СВОЙСТВ ИНДИКАТОРА === //
		
		// === СПИСКИ-ХРАНИЛИЩА-КОПИЛКИ === //
		//-------------------------------------
		// Рабочая лошадка №1... поуровневый (по цене) список ОБЪЕМОВ текущей свечки. 
		// На каждой новой свече основного таймфрейма он обнуляется и заполняется заново и заново и заново...
        //private Dictionary<Int32, double> vol_price = new Dictionary<Int32, double>();
		private Dictionary<Int32, double> vol_price = new Dictionary<Int32, double>();	// в списке ЦЕНА будет не в баксах а в ТИКАХ!!!
		
		// Рабочая лошадка №2... поуровневый (по цене) список ОБЪЕМОВ перерасчитываемой свечки. 
		// Используется при циклических вычислениях по историческим данным
		//private Dictionary<Int32, double> work_vol_price = new Dictionary<Int32, double>();
		private Dictionary<Int32, double> work_vol_price = new Dictionary<Int32, double>();	// в списке ЦЕНА будет не в баксах а в ТИКАХ!!!

		// для наших ДИАПАЗОННЫХ ПОКов пробуем такой вариант - по каждой свечке основного бара собираем СПИСОК объемов по ценовым уровням (как для ПОКа свечки)
		// НО! не обнуляем этот список на каждой новой свече, а сохраняем список в "списке списков"
		// ВНИМАНИЕТ! не работаем со СТРОКАМИ в списках - ЖУТКИЕ ТОРМОЗА!!! Берем Dictionary of the Dictionary!!!
		//private Dictionary<double, Dictionary<Int32, double>> bar_vol_price = new Dictionary<double, Dictionary<double,double>>();
		private Dictionary<Int32, Dictionary<Int32, double>> bar_vol_price = new Dictionary<Int32, Dictionary<Int32,double>>();	// в списке ЦЕНА будет не в баксах а в ТИКАХ!!!

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
			
			public int		rightBarBackup;
			public bool		leftOrientedBackup;
			public bool		vahvalExtendedBackup;
			
			public int		rangeWidth;
			public double	rangeVol;
			public int		rangeBars;
			
			public int		profileWidth;	// ширина профиля в конкретных единицах (барах или пикселях)
			public int		profileSize;	// размер профиля: Dime - Quoter - Half - Normal - Double - Triple (то есть от нуля до пяти)
			
			public double	VpocPrice;
			public double	VpocVol;
			public int		VpocType;
			public double	VAH;
			public double	VAL;
			public double	volumeVA;
			
			public int		extVpocBar;
			public int		extVpocOffset;
			public DateTime extVpocTime;
			
			public bool		isReady;
			public bool		isPlotted;
			public bool		isExtended;
			
			public bool		isLeftOriented;
			public bool		isLeftSlided;
			public bool		isRightSlided;
			public bool		isRangeFilled;
			public bool		isProfileVisible;
			public bool		isInfoVisible;
			public bool		isValVahExtended;
			public bool		isHollow;
			
			public Dictionary<Int32, double> vol_price;
			public Dictionary<Int32, double> work_vol_price;
			public Dictionary<Int32, double> bars_price;	
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
		enum StatusType{NotMoving,
			SelectRange1,MovingRange1,ChangeRange1,CloseRange1,ReadyToMove1,ReadyToClose1,ReadyToChange1,MovingInside1,
			SelectRange2,MovingRange2,ChangeRange2,CloseRange2,ReadyToMove2,ReadyToClose2,ReadyToChange2,MovingInside2,
			SelectRange3,MovingRange3,ChangeRange3,CloseRange3,ReadyToMove3,ReadyToClose3,ReadyToChange3,MovingInside3,
			SelectRange4,MovingRange4,ChangeRange4,CloseRange4,ReadyToMove4,ReadyToClose4,ReadyToChange4,MovingInside4
		};
		private StatusType status=StatusType.NotMoving;
		private StatusType lastStatus=StatusType.NotMoving;
		private int lastStatusNum = 0;	// номер последнего обновления статуса (номер диапазона, в котором это обновление сработало)
		
		// варианты СТАТУСОВ для действий с кнопками "панели управления" текущего выделенногодиапазона
		enum BtnStatusType{None, Btn1, Btn2, Btn3, Btn4, BtnR, BtnP, BtnI, BtnE, BtnV, BtnL, BtnH, BtnSizeL, BtnSizeR};
		private BtnStatusType btnStat = BtnStatusType.None;
		
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
		private Pen myPen;
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
		private string studioFolderName	= "";
		private string projectFolderName	= "";
		private string d	= "_";	// разделитель - delimiter
		private string myFileName	= "";
		private bool ReadyToRead	= false;	// проверяем загрузку всех свечей при старте - чтобы читать сохраненные профили ПОСЛЕ окончания этой загрузки
		private bool readOnce		= false;	// читаем настройки один раз - при загрузке! не забываем там изменить значение на true!

		// Переменные для встроенного внутреннего ПОКа баров
		// === СЕКЦИЯ ГОРЯЧИХ КЛАВИШ === //
		//----------------------------------
		private double vol	= 0;	// рабочая переменная
		private bool showAllVPOCs	= false;	// SHIFT+"A" -> показывать или прятать ВСЕ ПОКи баров
		private bool showNakedVPOCs	= false;	// SHIFT+"N" -> показывать или прятать этот паттерн
		private bool showStrongNakedVPOCs	= false;	// SHIFT+"S" -> показывать или прятать этот паттерн
		private bool showTriPodryad	= false;	// SHIFT+"T" -> показывать или прятать этот паттерн
		private bool showHiddenVPOCs = false;	// SHIFT+"H" -> показывать или прятать ДРУГИЕ (вторым цветом) при включенном одном из вышеуказанных режимов
		private bool showVPOCsVolume	= false;	// показывать ли снизу под барами ОБЪЕМ ПОКА
		//----------------------------------
		// === СЕКЦИЯ ГОРЯЧИХ КЛАВИШ === //

		// === "КОПИЛКИ" ДЛЯ ПАТТЕРНОВ === //
		//-------------------------------------
		private Dictionary<Int32, double> nakedVPOC_list = new Dictionary<Int32, double>();
		private Dictionary<Int32, double> nakedStrongVPOC_list	= new Dictionary<Int32, double>();
		private Dictionary<Int32, double> triVPOC_list	= new Dictionary<Int32, double>();
		// сюда так же добавляется "копилка" для любых других паттернов
		//-------------------------------------
		// === "КОПИЛКИ" ДЛЯ ПАТТЕРНОВ === //

		// и остальное...
		private Color myColor1	= Color.White;	// это будет рабочий цвет 1
		private Color myColor2	= Color.Yellow;	// это будет рабочий цвет 2
		private Color myHiddenColor	= Color.DarkGray;	// это будет цвет СКРЫТОГО участка ЛИНИИ - можно не прозразным сделать, а иным, но я предпочитаю так
		private Color myVPOCsColor	= Color.White;	// а сюда будем подставлять тот или иной НУЖНЫЙ цвет
		private int useVpocSize	= 0;	// фильтрация по объему: если НОЛЬ - не фильтруем, если число - показываем ТОЛЬКО те, что больше числа
		private bool useStrongRules	= true;	// Использовать ли более строгие правила для отбора "Голых ПОКов"???
											// Если ИСТИНА - то вообще показаны будут только те, которые "с полосочкой"
											// То есть те, перед которыми на предыдущих ТРЕХ барах было минимум ДВА ПОКа на одном и том же уровне!!!

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
			
//			Add(new Line(new Pen(vpocLineColor,		3), 0, "VPOCline"));
//			Add(new Line(new Pen(vpocExtLongColor,	3), 0, "ExtLongVPOC"));
//			Add(new Line(new Pen(vpocExtShortColor,	3), 0, "ExtShortVPOC"));
//			Add(new Line(new Pen(borderColor,		4), 0, "Borders"));			
//			Add(new Line(new Pen(vahLineColor,		1), 0, "VAH"));	
//			Add(new Line(new Pen(valLineColor,		1), 0, "VAL"));
			
			BarsRequired = 0;
			ClearOutputWindow();
			
			instrument = Instrument.MasterInstrument.Name;
			
		} // END of Initialize()
				
		protected override void OnStartUp()
		{	

			// Инициализируем основной массив для хранения данных			
			R = new  myRangeProfileType[5];	// чтобы не путаться с нулем - начнем с индекса номер 1 и каждому из четырех диапазонов будет соответствовать ПРАВИЛЬНЫЙ номер
											// но для этого надо маасив создать на ПЯТЬ ячеек
			// Создаем в нужных ячейках этого массива наши будущие внутренние "ладдеры" диапазонов
			R[0].vol_price = new Dictionary<Int32, double>();
			R[1].vol_price = new Dictionary<Int32, double>();
			R[2].vol_price = new Dictionary<Int32, double>();
			R[3].vol_price = new Dictionary<Int32, double>();
			R[4].vol_price = new Dictionary<Int32, double>();
			
			// И не забываем про такой же "ладдер" - "рабочую лошадку"
			R[0].work_vol_price = new Dictionary<Int32, double>();
			R[1].work_vol_price = new Dictionary<Int32, double>();
			R[2].work_vol_price = new Dictionary<Int32, double>();
			R[3].work_vol_price = new Dictionary<Int32, double>();
			R[4].work_vol_price = new Dictionary<Int32, double>();

			// И также не забываем про такой же "ладдер" - для профиля
			R[0].bars_price = new Dictionary<Int32, double>();
			R[1].bars_price = new Dictionary<Int32, double>();
			R[2].bars_price = new Dictionary<Int32, double>();
			R[3].bars_price = new Dictionary<Int32, double>();
			R[4].bars_price = new Dictionary<Int32, double>();

			// Еще сразу задаем значения "кнопок" (вернее самих параметров, которые переключаются кнопками) ПО УМОЛЧАНИЮ
			R[0].isLeftOriented		= false;
			R[0].isLeftSlided		= false;
			R[0].isRightSlided		= false;
			R[0].isRangeFilled		= true;
			R[0].isProfileVisible	= true;
			R[0].isInfoVisible		= showSummary;
			R[0].isValVahExtended	= extVahVal;
			R[0].profileSize		= 3; // NORMAL
			R[0].isHollow			= false;
			
			R[1].isLeftOriented		= false;
			R[1].isLeftSlided		= false;
			R[1].isRightSlided		= false;
			R[1].isRangeFilled		= true;
			R[1].isProfileVisible	= true;
			R[1].isInfoVisible		= showSummary;
			R[1].isValVahExtended	= extVahVal;
			R[1].profileSize		= 3; // NORMAL
			R[1].isHollow			= false;
			
			R[2].isLeftOriented		= false;
			R[2].isLeftSlided		= false;
			R[2].isRightSlided		= false;
			R[2].isRangeFilled		= true;
			R[2].isProfileVisible	= true;
			R[2].isInfoVisible		= showSummary;
			R[2].isValVahExtended	= extVahVal;
			R[2].profileSize		= 3; // NORMAL
			R[2].isHollow			= false;

			R[3].isLeftOriented		= false;
			R[3].isLeftSlided		= false;
			R[3].isRightSlided		= false;
			R[3].isRangeFilled		= true;
			R[3].isProfileVisible	= true;
			R[3].isInfoVisible		= showSummary;
			R[3].isValVahExtended	= extVahVal;
			R[3].profileSize		= 3; // NORMAL
			R[3].isHollow			= false;

			R[4].isLeftOriented		= false;
			R[4].isLeftSlided		= false;
			R[4].isRightSlided		= false;
			R[4].isRangeFilled		= true;
			R[4].isProfileVisible	= true;
			R[4].isInfoVisible		= showSummary;
			R[4].isValVahExtended	= extVahVal;
			R[4].profileSize		= 3; // NORMAL
			R[4].isHollow			= false;

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
			
			if(R[num].leftBar != 0 && R[num].rightBar != 0)
			{
				// имеется ОДИН натянутый диапазон
				//Print("Работаем с диапазоном #" + num);

				if (!R[num].isReady)
				{
					//Print("Перерасчет параметров диапазона номер " + num);
					// Будем все перерасчеты делать ТОЛЬКО если диапазон был изменен - при этом его свойство R[num].isReady будет сброшено в ЛОЖЬ
					R[num].vol_price.Clear(); // обнуляем "ладдер" и начинаем его заполнять
					R[num].VpocPrice = 0;
					R[num].VpocVol = 0;
					R[num].rangeVol = 0;
					R[num].rangeBars = 0;
					
					// Если у нас включен режим СЛАЙДИНГА ВЛЕВО
					if(R[num].isLeftSlided)
					{
						// то наш правый край - это всегда самая правая видимая свечка!!!
						R[num].rightBar = Math.Min(ChartControl.Bars[0].Count - 1, ChartControl.LastBarPainted);
						R[num].rightOffset = ChartControl.Bars[0].Count - 1 - R[num].rangeWidth;
						R[num].rightTime = Time[R[num].rightOffset];
						// а левый край теперь будет "скользить" по графику, то есть мы ФИКСИРУЕМ ширину на момент включения этого режима
						R[num].leftBar = R[num].rightBar - R[num].rangeWidth;
						R[num].leftOffset = R[num].rightOffset + R[num].rangeWidth;
						R[num].leftTime = Time[R[num].leftOffset];
						// также мы должны зафиксировать ширину профиля!!!
						R[num].profileWidth = R[num].rangeWidth;
					}
					// Начинаем в цикле обрабатывать данные обо всех барах в нашем диапазоне
					for(int i = R[num].leftBar - 1; i < R[num].rightBar; i++) {
					//for(int i = R[num].leftBar; i < R[num].rightBar; i++) {
						//достанем из "списка списков" нужный нам список-свечку
						
						if(!bar_vol_price.ContainsKey(i)) {
							Print("Нет такого внутреннего списка для бара #" + i);
						} else {
							R[num].work_vol_price = bar_vol_price[i];
						}
						// организуем внутренний цикл и суммируем все объемы ПО ОЧЕРЕДНОМУ БАРУ-СВЕЧКЕ
						foreach(KeyValuePair<Int32, double> kvp in R[num].work_vol_price){
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
					foreach(KeyValuePair<Int32, double> kvp in R[num].vol_price.OrderByDescending(key => key.Value))
					{
						// перебираем этот список от самого мелкого объема до самого крупного - таким образом находим ПОК!
						R[num].VpocPrice	= kvp.Key * TickSize;
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
					

//					// ПРОВЕРКА - все ли УРОВНИ есть в профиле???
//					Print("* RECALC-TEST * =========== RANGE = " + num + " ===================");
//					foreach(KeyValuePair<Int32, double> kvp in R[num].vol_price.OrderByDescending(key => key.Key))
//					{
//						// перебираем этот список от самой маленькой цены до самой большой и выводим в окно для проверки
//						Print("price = " + kvp.Key * TickSize + " \t vol = " + kvp.Value);
//					}
//					// ВРОДЕ ВСЁ ЕСТЬ!!!
					
					
					// Попытка сделать вычисление VALUE AREA!!!
					// Пока упрощенно - исходя из того, что у нас есть ОДИН ПОК (случай, когда их ДВА одинаковых или даже больше - рассмотрим потом в применении именно к поиску правильного ПОКа)
					// Согласно теории, нам надо взять сверху и снизу от пока сумму ДВУХ уровней и сравнить. После чего двигаемся "в сторону большего".
					// Потом повторяем: опять берем сверху и снизу суммы ДВУХ уровней, двигаемся, берем, двигаемся - пока не получим 70% или другую заданную величину
					// Значит цикл по условию достижения объема. И значит, если высота профиля ПЯТЬ или меньше тиков - то по упрощенной схеме обозначаем VA просто на тик выше и ниже ПОКа
					R[num].VAH = Math.Min(R[num].VpocPrice + TickSize, R[num].Top);
					R[num].VAL = Math.Max(R[num].VpocPrice - TickSize, R[num].Bottom);
					R[num].volumeVA = R[num].rangeVol * 68 / 100; // Это РАССЧЕТНАЯ величина объема

					if(Convert.ToInt32((R[num].Top - R[num].Bottom) / TickSize) > 5)
					{
						// РАБОТАЕМ!!! То есть ВЫЧИСЛЯЕМ!
						try
						{
							double curVolVA = 0;
							try {
							R[num].VAH = R[num].VpocPrice;
							R[num].VAL = R[num].VpocPrice;
							curVolVA = R[num].VpocVol;
							} catch {Print("Catch 1");}
								
							double curTopVol = 0;
							double curBottomVol = 0;
							double curPrice1 = R[num].VpocPrice;
							double curPrice2 = 0;
							while(curVolVA < R[num].volumeVA)
							{
								// получаем суммы двух объемов НАД ПОКом
								// только тут важно не выйти ЗА ВЕРХНИЙ край диапазона
								curPrice1 = R[num].VAH + TickSize;
								if(curPrice1 > R[num].Top)
								{
									// вышли за край - дальше ВВЕРХ мы точно не будем!!!
									curTopVol = 0;
								} else if(curPrice1 == R[num].Top)
								{
									// еще не вышли, но совсем рядом и вторую сточку уже брать негде
									curTopVol = R[num].vol_price[Convert.ToInt32(curPrice1/TickSize)];
									//Print("1) CurPrice1 = " + curPrice1 + "\tCurPrice2 = " + curPrice2 + "\t Top = " + R[num].Top + "\t Bottom = " + R[num].Bottom);
								} else if((curPrice1 < R[num].Top) && (curPrice1 >= R[num].Bottom)) 
								{
									// все еще в пределах нормы - работаем как обычно
									curPrice2 = curPrice1 + TickSize;
									curPrice2 = Convert.ToInt32(curPrice1/TickSize + 1) * TickSize;
//									Print("2) CurPrice1 = " + curPrice1 + "\tCurPrice2 = " + curPrice2 + "\t Top = " + R[num].Top + "\t Bottom = " + R[num].Bottom);
//									Print("2-a) vol = " + R[num].vol_price[1.0695]);
//									Print("2-b) vol = " + R[num].vol_price[curPrice1]);
//									Print("2-c) vol = " + R[num].vol_price[1.0697]);
//									Print("2-d) vol = " + R[num].vol_price[curPrice2]);
									curTopVol = R[num].vol_price[Convert.ToInt32(curPrice1/TickSize)] + R[num].vol_price[Convert.ToInt32(curPrice2/TickSize)];
								} else {
									// а вот ТУТ что-то не так!!!
									Print("0) CurPrice1 = " + curPrice1 + "\tCurPrice2 = " + curPrice2 + "\t Top = " + R[num].Top + "\t Bottom = " + R[num].Bottom);
								}
								// получаем суммы двух объемов ПОД ПОКом
								// только тут важно не выйти ЗА НИЖНИЙ край диапазона
								curPrice1 = R[num].VAL - TickSize;
								if(curPrice1 < R[num].Bottom)
								{
									// вышли за край - дальше ВВЕРХ мы точно не будем!!!
									curBottomVol = 0;
								} else if(curPrice1 == R[num].Bottom) 
								{
									// еще не вышли, но совсем рядом и вторую сточку уже брать негде
									curBottomVol = R[num].vol_price[Convert.ToInt32(curPrice1/TickSize)];	
								} else if((curPrice1 > R[num].Bottom) && (curPrice1 <= R[num].Top))
								{
									// все еще в пределах нормы - работаем как обычно
									curPrice2 = curPrice1 - TickSize;
									curBottomVol = R[num].vol_price[Convert.ToInt32(curPrice1/TickSize)] + R[num].vol_price[Convert.ToInt32(curPrice2/TickSize)];
								} else {
									// а вот ТУТ что-то не так!!!
									Print("CurPrice1 = " + curPrice1 + "\t Bottom = " + R[num].Bottom + "\t Top = " + R[num].Top);
								}
								if(curTopVol > curBottomVol)
								{
									// расширяем ValueArea вверх
									R[num].VAH += 2*TickSize;
									// и соответственно увеличиваем накопленный объем VA
									curVolVA += curTopVol;
								} else if(curTopVol < curBottomVol)
								{
									// расширяем ValueArea вниз
									R[num].VAL -= 2*TickSize;
									// и соответственно увеличиваем накопленный объем VA
									curVolVA += curBottomVol;
								} else if(curTopVol > 0 && curBottomVol > 0){
									// если равны между собой, но при этом не равны нулю - расширяем ValueArea в обе стороны
									R[num].VAH += 2*TickSize;
									R[num].VAL -= 2*TickSize;
									// и соответственно увеличиваем накопленный объем VA
									curVolVA += curTopVol + curBottomVol;
								}				
							}
							//Print("volumeVA = " + volumeVA + "\tcurVolVA = " + curVolVA + "\tVAH = " + R[num].VAH + "\tVAL = " + R[num].VAL);
							R[num].volumeVA = curVolVA; // А вот это уже РЕАЛЬНАЯ величина объема!!!
						} catch(Exception e)
						{ 
							//Print(Time[0] + " " + e.ToString());
						}
					} else {
						// Просто НАЗНАЧАЕМ!!!
						//Print("НАЗНАЧЕННАЯ Value Area!!!");
						//Print("Top = " + R[num].Top + " Bottom = " + R[num].Bottom);
					}
			
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
				
				if(((R[num].VAH - R[num].VAH) > TickSize * 2) || ((R[num].Top - R[num].Bottom) < TickSize * 5)) 
				{
					R[num].isReady = true; 		// укажем, что мы уже все рассчитали!!!
				} else {
					R[num].isReady = false;		// укажем, что не удалось нормально рассчитать VALUE AREA и надо будет повторить вычисления еще раз
				}
				R[num].isPlotted = false; 	// укажем, что мы диапазон после пересчета еще не отрисовали!!!
				// ну и стоит сохраниться
				SaveProfilePosition();
				
//				Print("* RECALC * \tnum = " + num + " \tisReady = " + R[num].isReady);
			}
		}
		private void RecalculateExtVPOCsByNum(int num) {			
			// Рисуем продолжение линии ПОКа до первого бара, которые ее (линию) пересечет
			// =============================================================================
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
		
		#region RemoveRangeByNum
		public void RemoveRangeByNum(int num) {
			//Print("REMOVE before:  Now=" + nowRanges + " | Selected=" + curSelectedRange + " | Created=" + curCreatedRange);
			switch(num) {
				case 1:
					//Print("Close-1");
					// закрыть надо бы
					// то есть просто остальные три диапазона сдвинуть вниз, а последний еще и обнулить
					//R[1] = R[2];
					R[1].leftBar	= R[2].leftBar;
					R[1].rightBar	= R[2].rightBar;
					R[1].Top		= R[2].Top;
					R[1].Bottom		= R[2].Bottom;
					R[1].VpocPrice	= R[2].VpocPrice;
					R[1].VpocVol	= R[2].VpocVol;
					R[1].extVpocBar	= R[2].extVpocBar;
					R[1].rangeWidth	= R[2].rangeWidth;
					R[1].volumeVA	= R[2].volumeVA;
					R[1].VAH		= R[2].VAH;
					R[1].VAL		= R[2].VAL;
					R[1].profileSize = R[2].profileSize;
					R[1].isLeftOriented		= R[2].isLeftOriented;
					R[1].isLeftSlided		= R[2].isLeftSlided;
					R[1].isRightSlided		= R[2].isRightSlided;
					R[1].isRangeFilled		= R[2].isRangeFilled;
					R[1].isProfileVisible	= R[2].isProfileVisible;
					R[1].isInfoVisible		= R[2].isInfoVisible;
					R[1].isValVahExtended	= R[2].isValVahExtended;
					R[1].isHollow			= R[2].isHollow;
					
					//R[2] = R[3];
					R[2].leftBar	= R[3].leftBar;
					R[2].rightBar	= R[3].rightBar;
					R[2].Top		= R[3].Top;
					R[2].Bottom		= R[3].Bottom;
					R[2].VpocPrice	= R[3].VpocPrice;
					R[2].VpocVol	= R[3].VpocVol;
					R[2].extVpocBar	= R[3].extVpocBar;
					R[2].rangeWidth	= R[3].rangeWidth;
					R[2].volumeVA	= R[3].volumeVA;
					R[2].VAH		= R[3].VAH;
					R[2].VAL		= R[3].VAL;
					R[2].profileSize = R[3].profileSize;
					R[2].isLeftOriented		= R[3].isLeftOriented;
					R[2].isLeftSlided		= R[3].isLeftSlided;
					R[2].isRightSlided		= R[3].isRightSlided;
					R[2].isRangeFilled		= R[3].isRangeFilled;
					R[2].isProfileVisible	= R[3].isProfileVisible;
					R[2].isInfoVisible		= R[3].isInfoVisible;
					R[2].isValVahExtended	= R[3].isValVahExtended;
					R[2].isHollow			= R[3].isHollow;
					
					//R[3] = R[4];
					R[3].leftBar	= R[4].leftBar;
					R[3].rightBar	= R[4].rightBar;
					R[3].Top		= R[4].Top;
					R[3].Bottom		= R[4].Bottom;
					R[3].VpocPrice	= R[4].VpocPrice;
					R[3].VpocVol	= R[4].VpocVol;
					R[3].extVpocBar	= R[4].extVpocBar;
					R[3].rangeWidth	= R[4].rangeWidth;
					R[3].volumeVA	= R[4].volumeVA;
					R[3].VAH		= R[4].VAH;
					R[3].VAL		= R[4].VAL;
					R[3].profileSize = R[4].profileSize;
					R[3].isLeftOriented		= R[4].isLeftOriented;
					R[3].isLeftSlided		= R[4].isLeftSlided;
					R[3].isRightSlided		= R[4].isRightSlided;
					R[3].isRangeFilled		= R[4].isRangeFilled;
					R[3].isProfileVisible	= R[4].isProfileVisible;
					R[3].isInfoVisible		= R[4].isInfoVisible;
					R[3].isValVahExtended	= R[4].isValVahExtended;
					R[3].isHollow			= R[4].isHollow;

					R[4].leftBar	= 0;
					R[4].rightBar	= 0;
					R[4].Top		= 0;
					R[4].Bottom		= 0;
					R[4].VpocPrice	= 0;
					R[4].VpocVol	= 0;
					R[4].extVpocOffset = 0;
					R[4].extVpocBar = 0;
					R[4].rangeWidth	= 0;
					R[4]._Xl		= 0;
					R[4]._Xr		= 0;
//					R[4]._Yb		= 0;
//					R[4]._Yt		= 0;
					R[4].volumeVA	= 0;
					R[4].VAH		= 0;
					R[4].VAL		= 0;
					R[4].profileSize = 3; // NORMAL
					R[4].isLeftOriented		= false;
					R[4].isLeftSlided		= false;
					R[4].isRightSlided		= false;
					R[4].isRangeFilled		= true;
					R[4].isProfileVisible	= true;
					R[4].isInfoVisible		= showSummary;
					R[4].isValVahExtended	= extVahVal;
					R[4].isHollow			= false;
					R[4].vol_price.Clear();
					
					//Print("1 closed...");
					break;
				case 2:
					//Print("Close-2");
					// закрыть надо бы
					// то есть просто последние два сдвинуть вниз, а самый последний еще и обнулить
					//R[2] = R[3];
					R[2].leftBar	= R[3].leftBar;
					R[2].rightBar	= R[3].rightBar;
					R[2].Top		= R[3].Top;
					R[2].Bottom		= R[3].Bottom;
					R[2].VpocPrice	= R[3].VpocPrice;
					R[2].VpocVol	= R[3].VpocVol;
					R[2].extVpocBar	= R[3].extVpocBar;
					R[2].rangeWidth	= R[3].rangeWidth;
					R[2].volumeVA	= R[3].volumeVA;
					R[2].VAH		= R[3].VAH;
					R[2].VAL		= R[3].VAL;
					R[2].profileSize = R[3].profileSize;
					R[2].isLeftOriented		= R[3].isLeftOriented;
					R[2].isLeftSlided		= R[3].isLeftSlided;
					R[2].isRightSlided		= R[3].isRightSlided;
					R[2].isRangeFilled		= R[3].isRangeFilled;
					R[2].isProfileVisible	= R[3].isProfileVisible;
					R[2].isInfoVisible		= R[3].isInfoVisible;
					R[2].isValVahExtended	= R[3].isValVahExtended;
					R[2].isHollow			= R[3].isHollow;

					//R[3] = R[4];
					R[3].leftBar	= R[4].leftBar;
					R[3].rightBar	= R[4].rightBar;
					R[3].Top		= R[4].Top;
					R[3].Bottom		= R[4].Bottom;
					R[3].VpocPrice	= R[4].VpocPrice;
					R[3].VpocVol	= R[4].VpocVol;
					R[3].extVpocBar	= R[4].extVpocBar;
					R[3].rangeWidth	= R[4].rangeWidth;
					R[3].volumeVA	= R[4].volumeVA;
					R[3].VAH		= R[4].VAH;
					R[3].VAL		= R[4].VAL;
					R[3].profileSize = R[4].profileSize;
					R[3].isLeftOriented		= R[4].isLeftOriented;
					R[3].isLeftSlided		= R[4].isLeftSlided;
					R[3].isRightSlided		= R[4].isRightSlided;
					R[3].isRangeFilled		= R[4].isRangeFilled;
					R[3].isProfileVisible	= R[4].isProfileVisible;
					R[3].isInfoVisible		= R[4].isInfoVisible;
					R[3].isValVahExtended	= R[4].isValVahExtended;
					R[3].isHollow			= R[4].isHollow;

					R[4].leftBar	= 0;
					R[4].rightBar	= 0;
					R[4].Top		= 0;
					R[4].Bottom		= 0;
					R[4].VpocPrice	= 0;
					R[4].VpocVol	= 0;
					R[4].extVpocOffset = 0;
					R[4].extVpocBar = 0;
					R[4].rangeWidth	= 0;
					R[4]._Xl		= 0;
					R[4]._Xr		= 0;
//					R[4]._Yb		= 0;
//					R[4]._Yt		= 0;
					R[4].volumeVA	= 0;
					R[4].VAH		= 0;
					R[4].VAL		= 0;
					R[4].profileSize = 3; // NORMAL
					R[4].isLeftOriented		= false;
					R[4].isLeftSlided		= false;
					R[4].isRightSlided		= false;
					R[4].isRangeFilled		= true;
					R[4].isProfileVisible	= true;
					R[4].isInfoVisible		= showSummary;
					R[4].isValVahExtended	= extVahVal;
					R[4].isHollow			= false;
					R[4].vol_price.Clear();
					
					//Print("2 closed...");
					break;
				case 3:
					//Print("Close-3");
					// закрыть надо бы
					// то есть просто предпоследний сдвинуть вниз, а самый последний обнулить
					//R[3] = R[4];
					R[3].leftBar	= R[4].leftBar;
					R[3].rightBar	= R[4].rightBar;
					R[3].Top		= R[4].Top;
					R[3].Bottom		= R[4].Bottom;
					R[3].VpocPrice	= R[4].VpocPrice;
					R[3].VpocVol	= R[4].VpocVol;
					R[3].extVpocBar	= R[4].extVpocBar;
					R[3].rangeWidth	= R[4].rangeWidth;
					R[3].volumeVA	= R[4].volumeVA;
					R[3].VAH		= R[4].VAH;
					R[3].VAL		= R[4].VAL;
					R[3].profileSize = R[4].profileSize;
					R[3].isLeftOriented		= R[4].isLeftOriented;
					R[3].isLeftSlided		= R[4].isLeftSlided;
					R[3].isRightSlided		= R[4].isRightSlided;
					R[3].isRangeFilled		= R[4].isRangeFilled;
					R[3].isProfileVisible	= R[4].isProfileVisible;
					R[3].isInfoVisible		= R[4].isInfoVisible;
					R[3].isValVahExtended	= R[4].isValVahExtended;
					R[3].isHollow			= R[4].isHollow;

					R[4].leftBar	= 0;
					R[4].rightBar	= 0;
					R[4].Top		= 0;
					R[4].Bottom		= 0;
					R[4].VpocPrice	= 0;
					R[4].VpocVol	= 0;
					R[4].extVpocOffset = 0;
					R[4].extVpocBar = 0;
					R[4].rangeWidth	= 0;
					R[4]._Xl		= 0;
					R[4]._Xr		= 0;
//					R[4]._Yb		= 0;
//					R[4]._Yt		= 0;
					R[4].volumeVA	= 0;
					R[4].VAH		= 0;
					R[4].VAL		= 0;
					R[4].profileSize = 3; // NORMAL
					R[4].isLeftOriented		= false;
					R[4].isLeftSlided		= false;
					R[4].isRightSlided		= false;
					R[4].isRangeFilled		= true;
					R[4].isProfileVisible	= true;
					R[4].isInfoVisible		= showSummary;
					R[4].isValVahExtended	= extVahVal;
					R[4].isHollow			= false;
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
					R[4].rangeWidth	= 0;
					R[4]._Xl		= 0;
					R[4]._Xr		= 0;
//					R[4]._Yb		= 0;
//					R[4]._Yt		= 0;
					R[4].volumeVA	= 0;
					R[4].VAH		= 0;
					R[4].VAL		= 0;
					R[4].profileSize = 3; // NORMAL
					R[4].isLeftOriented		= false;
					R[4].isLeftSlided		= false;
					R[4].isRightSlided		= false;
					R[4].isRangeFilled		= true;
					R[4].isProfileVisible	= true;
					R[4].isInfoVisible		= showSummary;
					R[4].isValVahExtended	= extVahVal;
					R[4].isHollow			= false;
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
			if(nowRanges == 0) {
				RemoveDrawObject("curRange");
				RemoveDrawObject("curFrameL");
				RemoveDrawObject("curFrameR");
			}
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
			lastStatus = StatusType.NotMoving;
			int dX = 5;
			int dY = 5;			
			//Print("mouseX=" + mX + " | mouseY=" + mY);
			
			// КНОПКА ЗАКРЫТИЯ - самая первая, так как накладывается на координаты правой границы и внутреннего пространства
			// и если ее не сделать первой - то ТЕ координаты сработают и мы кнопку не нажмем...
			if(mX >= R[1]._Xr - 2*dX && mX <= R[1]._Xr + dX && mY <= R[1]._Yt + 2*dY && mY >= R[1]._Yt - 2*dY) {
				Cursor.Current = Cursors.Cross;
				//Print("Over CloseBurron-1");
				lastStatus = StatusType.ReadyToClose1;
				lastStatusNum = 1;
				return StatusType.CloseRange1;
			}
			if(mX >= R[2]._Xr - 2*dX && mX <= R[2]._Xr + dX && mY <= R[2]._Yt + 2*dY && mY >= R[2]._Yt - 2*dY) {
				Cursor.Current = Cursors.Cross;
				//Print("Over CloseBurron-2");
				lastStatus = StatusType.ReadyToClose2;
				lastStatusNum = 2;
				return StatusType.CloseRange2;
			}
			if(mX >= R[3]._Xr - 2*dX && mX <= R[3]._Xr + dX && mY <= R[3]._Yt + 2*dY && mY >= R[3]._Yt - 2*dY) {
				Cursor.Current = Cursors.Cross;
				//Print("Over CloseBurron-3");
				lastStatus = StatusType.ReadyToClose3;
				lastStatusNum = 3;
				return StatusType.CloseRange3;
			}
			if(mX >= R[4]._Xr - 2*dX && mX <= R[4]._Xr + dX && mY <= R[4]._Yt + 2*dY && mY >= R[4]._Yt - 2*dY) {
				Cursor.Current = Cursors.Cross;
				//Print("Over CloseBurron-4");
				lastStatus = StatusType.ReadyToClose4;
				lastStatusNum = 4;
				return StatusType.CloseRange4;				
			} 

			// ПАНЕЛЬ УПРАВЛЕНИЯ ИНДИКАТОРОМ
			// по задумке будем уходить от хоткеев и нам понадобится панель для всего индикатора вместо горячих клавиш
			
			// ПАНЕЛЬ УПРАВЛЕНИЯ ПРОФИЛЕМ
			// Еще нам надо ловить нажатия на кнопки "панели управления", причем ТОЛЬКО на активном диапазоне!!!
			// И их "перехватчик" тоже должен быть (как и кнопка удаления) ДО ТОГО, как пойдут все остальные: веделение, перемещение, изменение размера...
			if(mY >= R[curSelectedRange]._Yt - 20F && mY <= R[curSelectedRange]._Yt - 5F)
			{
				//Print("Inside Control Panel TOP");
				if(mX >= R[curSelectedRange]._Xl + 5 && mX <= R[curSelectedRange]._Xl + 20) {
					//Print("Inside Control Panel - BTN-R");
					btnStat = BtnStatusType.BtnR;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 22 && mX <= R[curSelectedRange]._Xl + 37) {
					//Print("Inside Control Panel - BTN-P");
					btnStat = BtnStatusType.BtnP;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 39 && mX <= R[curSelectedRange]._Xl + 54) {
					//Print("Inside Control Panel - BTN-I");
					btnStat = BtnStatusType.BtnI;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 56 && mX <= R[curSelectedRange]._Xl + 71) {
					//Print("Inside Control Panel - BTN-E");
					btnStat = BtnStatusType.BtnE;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 73 && mX <= R[curSelectedRange]._Xl + 88) {
					//Print("Inside Control Panel - BTN-H");
					btnStat = BtnStatusType.BtnH;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 90 && mX <= R[curSelectedRange]._Xl + 105) {
					//Print("Inside Control Panel - BTN->>");
					//btnStat = BtnStatusType.Btn4;
					btnStat = BtnStatusType.None;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 107 && mX <= R[curSelectedRange]._Xl + 122) {
					//Print("Inside Control Panel - BTN-04");
					//btnStat = BtnStatusType.Btn1;
					btnStat = BtnStatusType.None;
					return result;
				} else {
					btnStat = BtnStatusType.None;
					return result;
				}
			} else if(mY >= R[curSelectedRange]._Yb + 5F && mY <= R[curSelectedRange]._Yb + 20F)
			{
				//Print("Inside Control Panel BOTTOM");
				if(mX >= R[curSelectedRange]._Xl + 5 && mX <= R[curSelectedRange]._Xl + 20) {
					//Print("Inside Control Panel - BTN-<=);
					btnStat = BtnStatusType.BtnSizeL;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 22 && mX <= R[curSelectedRange]._Xl + 37) {
					//Print("Inside Control Panel - BTN-H");
					// тут ничего не надо
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 39 && mX <= R[curSelectedRange]._Xl + 54) {
					//Print("Inside Control Panel - BTN-=>");
					btnStat = BtnStatusType.BtnSizeR;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 56 && mX <= R[curSelectedRange]._Xl + 71) {
					//Print("Inside Control Panel - BTN-L");
					btnStat = BtnStatusType.BtnL;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 73 && mX <= R[curSelectedRange]._Xl + 88) {
					//Print("Inside Control Panel - BTN->>");
					btnStat = BtnStatusType.Btn4;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 90 && mX <= R[curSelectedRange]._Xl + 105) {
					//Print("Inside Control Panel - BTN->>");
					//btnStat = BtnStatusType.Btn4;
					btnStat = BtnStatusType.None;
					return result;
				} else if(mX >= R[curSelectedRange]._Xl + 107 && mX <= R[curSelectedRange]._Xl + 122) {
					//Print("Inside Control Panel - BTN-04");
					//btnStat = BtnStatusType.Btn1;
					btnStat = BtnStatusType.None;
					return result;
				} else {
					btnStat = BtnStatusType.None;
					return result;
				}
			} else {
				btnStat = BtnStatusType.None;
			}
			
			// ЛЕВАЯ ГРАНИЦА
			//if(mX >= R[1]._Xl - dX && mX <= R[1]._Xl + dX && mY <= R[1]._Yb && mY >= R[1]._Yt) {
			if(mX >= R[1]._Xl && mX <= R[1]._Xl + dX && mY <= R[1]._Yb && mY >= R[1]._Yt) {
				Cursor.Current = Cursors.SizeAll;
				//Print("Over Left Side-1");
				lastStatus = StatusType.ReadyToMove1;
				lastStatusNum = 1;
				return StatusType.MovingRange1;
			}
			if(mX >= R[2]._Xl - dX && mX <= R[2]._Xl + dX && mY <= R[2]._Yb && mY >= R[2]._Yt) {
				Cursor.Current = Cursors.SizeAll;
				//Print("Over Left Side-2");
				lastStatus = StatusType.ReadyToMove2;
				lastStatusNum = 2;
				return StatusType.MovingRange2;
			}
			if(mX >= R[3]._Xl - dX && mX <= R[3]._Xl + dX && mY <= R[3]._Yb && mY >= R[3]._Yt) {
				Cursor.Current = Cursors.SizeAll;
				//Print("Over Left Side-3");
				lastStatus = StatusType.ReadyToMove3;
				lastStatusNum = 3;
				return StatusType.MovingRange3;
			}
			if(mX >= R[4]._Xl - dX && mX <= R[4]._Xl + dX && mY <= R[4]._Yb && mY >= R[4]._Yt) {
				Cursor.Current = Cursors.SizeAll;
				//Print("Over Left Side-4");
				lastStatus = StatusType.ReadyToMove4;
				lastStatusNum = 4;
				return StatusType.MovingRange4;
			}
			// ПРАВАЯ ГРАНИЦА
			if(mX >= R[1]._Xr - dX && mX <= R[1]._Xr + dX && mY <= R[1]._Yb && mY >= R[1]._Yt + 2* dY) {
				Cursor.Current = Cursors.SizeWE;
				//Print("Over Right Side-1");
				lastStatus = StatusType.ReadyToChange1;
				lastStatusNum = 1;
				return StatusType.ChangeRange1;
			}
			if(mX >= R[2]._Xr - dX && mX <= R[2]._Xr + dX && mY <= R[2]._Yb && mY >= R[2]._Yt + 2* dY) {
				Cursor.Current = Cursors.SizeWE;
				//Print("Over Right Side-2");
				lastStatus = StatusType.ReadyToChange2;
				lastStatusNum = 2;
				return StatusType.ChangeRange2;
			}
			if(mX >= R[3]._Xr - dX && mX <= R[3]._Xr + dX && mY <= R[3]._Yb && mY >= R[3]._Yt + 2* dY) {
				Cursor.Current = Cursors.SizeWE;
				//Print("Over Right Side-3");
				lastStatus = StatusType.ReadyToChange3;
				lastStatusNum = 3;
				return StatusType.ChangeRange3;
			}
			if(mX >= R[4]._Xr - dX && mX <= R[4]._Xr + dX && mY <= R[4]._Yb && mY >= R[4]._Yt + 2* dY) {
				Cursor.Current = Cursors.SizeWE;
				//Print("Over Right Side-4");
				lastStatus = StatusType.ReadyToChange4;
				lastStatusNum = 4;
				return StatusType.ChangeRange4;
			}
			// ВНУТРИ ДИАПАЗОНА
			if(mX >= R[1]._Xl && mX <= R[1]._Xr && mY <= R[1]._Yb && mY >= R[1]._Yt) {
				//Print("Inside Range-1")
				lastStatus = StatusType.MovingInside1;
				lastStatusNum = 1;
				return StatusType.SelectRange1;
			}
			if(mX >= R[2]._Xl && mX <= R[2]._Xr && mY <= R[2]._Yb && mY >= R[2]._Yt) {
				//Print("Inside Range-2")
				lastStatus = StatusType.MovingInside2;
				lastStatusNum = 2;
				return StatusType.SelectRange2;
			}
			if(mX >= R[3]._Xl && mX <= R[3]._Xr && mY <= R[3]._Yb && mY >= R[3]._Yt) {
				//Print("Inside Range-3")
				lastStatus = StatusType.MovingInside3;
				lastStatusNum = 3;
				return StatusType.SelectRange3;
			}
			if(mX >= R[4]._Xl && mX <= R[4]._Xr && mY <= R[4]._Yb && mY >= R[4]._Yt) {
				//Print("Inside Range-4")
				lastStatus = StatusType.MovingInside4;
				lastStatusNum = 4;
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
		//-------------------
		public void Check_Naked_VPOCs() {
			// копилка nakedVPOC_list и nakedStrongVPOC_list
			// Проверяем VPOC два бара назад (ближе нельзя!) на его соответствие требований к "голышам" - NAKED VPOCs - ГОЛЫЕ ПОКи
			// Но сначала фильтр по величину объема - может он нам вообще не подходит и не важно "голый" он или одетый???
			int BarNum = CurrentBar - 2;

			#region FILTERS
			/**/
			double vol = 0;
			if(useVpocSize != 0) {
				// Если не НОЛЬ - будем фильтровать!!!
				vol = (!vPoc_list.ContainsKey(CurrentBar-2)) ? 0: vPoc_list[CurrentBar - 2];
				if( vol < useVpocSize) {
					// Если у нас ПОК меньше заданного фильтра - то скрываем ВСЕ ТРИ ЗНАЧЕНИЯ!!!
					PlotColors[0][2] = Color.Transparent;
					// и выходим из данной функции вообще
					return;
				}
			}
			/**/
			#endregion FILTERS

			if(true
				&& (useStrongRules ? ((VPOC[2] > High[3]) && (VPOC[2] > High[1])) : ((VPOC[2] >= High[3]) && (VPOC[2] >= High[1])))
				|| (useStrongRules ? ((VPOC[2] < Low[3]) && (VPOC[2] < Low[1])) : ((VPOC[2] <= Low[3]) && (VPOC[2] <= Low[1])))
			){
				// Это и есть наш "голыш"
				// Можно проверить еще некие паттерны, например СТРОГИЙ "голыш" - когда имеется еще два одинаковых ПОКа перед этим "голышом"!!!
				if(	VPOC[3] == VPOC[4]
					|| VPOC[4] == VPOC[5]
					|| VPOC[3] == VPOC[5]
				){
					// ДА! Это СТРОГИЙ "голыш". Его добавляем в список "голышей" в любом случае!!!
					if(!nakedVPOC_list.ContainsKey(BarNum)) {
						nakedVPOC_list.Add(BarNum, VPOC[2]);
					} else {
						nakedVPOC_list[BarNum] = VPOC[2];
					}
					// Ну и не забываем в собственную копилочку тоже положить!!!
					if(!nakedStrongVPOC_list.ContainsKey(BarNum)) {
						nakedStrongVPOC_list.Add(BarNum, VPOC[2]);
					} else {
						nakedStrongVPOC_list[BarNum] = VPOC[2];
					}
					// А если мы в реал-тайме работаем, то не грех и показать это чудо!
					if(!Historical && (showNakedVPOCs || showStrongNakedVPOCs)) {
						DrawDot(BarNum.ToString() + "vpocDot", false, 2, VPOC[2], myVPOCsColor);
						if(showStrongNakedVPOCs) DrawLine(BarNum.ToString() + "vpocLine", 2, VPOC[2], 0, VPOC[2], myVPOCsColor); 
					}
				} else {
					// А это ОБЫЧНЫЙ "голыш"... Сохраняем!
					if(!nakedVPOC_list.ContainsKey(BarNum)) {
						nakedVPOC_list.Add(BarNum, VPOC[2]);
					} else {
						nakedVPOC_list[BarNum] = VPOC[2];
					}
					// Но в реал-тайме его показываем ТОЛЬКО если включен режим обычных голышей
					if(!Historical && showNakedVPOCs) {
						DrawDot(BarNum.ToString() + "vpocDot", false, 2, VPOC[2], myVPOCsColor);
					}
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
			if(showNakedVPOCs)
			{
				// обычные голыши
				foreach(KeyValuePair<Int32, double> kvp in nakedVPOC_list)
				{
					BarNum=kvp.Key;
					Price=kvp.Value;
					offset = Bars.Count - 1 - BarNum;
					DrawDot(BarNum.ToString() + "vpocDot", false, offset, Price, myVPOCsColor);
				}
			}
			if(showStrongNakedVPOCs)
			{
				// сильные голыши
				foreach(KeyValuePair<Int32, double> kvp in nakedStrongVPOC_list)
				{
					BarNum=kvp.Key;
					Price=kvp.Value;
					offset = Bars.Count - 1 - BarNum;
					DrawDot(BarNum.ToString() + "vpocDot", false, offset, Price, myVPOCsColor);
					DrawLine(BarNum.ToString() + "vpocLine", offset, Price, offset - 2, Price, myVPOCsColor);
				}
			} 
			} else {
				// в реалтайме оно все просто перестанет дальше само рисоваться - тут надо спрятать в цикле все имевшееся ранеее
				if(!showNakedVPOCs)
				{
					// обычные голыши
					foreach(KeyValuePair<Int32, double> kvp in nakedVPOC_list)
					{
						BarNum=kvp.Key;
						Price=kvp.Value;
						offset = Bars.Count - 1 - BarNum;
						RemoveDrawObject(BarNum.ToString() + "vpocDot");
					}
					// а если при этом были включены СИЛЬНЫЕ, то часть их сейчас удалилась - надо их перерисовать!!!
					if(showStrongNakedVPOCs) Show_NAKED_VPOCs(true);
				}
				if(!showStrongNakedVPOCs)
				{
					// сильные голыши
					foreach(KeyValuePair<Int32, double> kvp in nakedStrongVPOC_list)
					{
						BarNum=kvp.Key;
						Price=kvp.Value;
						offset = Bars.Count - 1 - BarNum;
						RemoveDrawObject(BarNum.ToString() + "vpocDot");
						RemoveDrawObject(BarNum.ToString() + "vpocLine");
					}
					// а если при этом были включены ОБЫЧНЫЕ, то часть их сейчас удалилась - надо их перерисовать!!!
					if(showNakedVPOCs) Show_NAKED_VPOCs(true);
				} 
				// а если при этом были включены СКРЫТЫЕ, то часть их сейчас удалилась - надо их перерисовать!!!
				if(showHiddenVPOCs) Show_HIDDEN_VPOCs(true);
			}
			// после всего  надо дать знать графику - мол, пора перерисовать картинку
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
			// впрочем, это можно сделать и там, откуда данная функция будет вызвана
		}
		//-------------------
		#endregion CHECK_NAKED_VPOC

		#region CHECK_TRI_VPOCs_PODRYAD
		//-------------------
		public void Check_Tri_VPOCs_Podryad() {
			// копилка triVPOC_list
			int BarNum1 = CurrentBar - 1;
			int BarNum2 = CurrentBar - 2;
			int BarNum3 = CurrentBar - 3;

			// Ну а если показываем ТРИ ПОДРЯД - тогда красим все ПОКи, которые соответствуют такому правилу:
			// ПОК совпадает с предыдущим или пред-предыдущим с точностью до заданного количества тиков. Начнем с нуля - то есть ТОЧНОЕ СОВПАДЕНИЕ
			int tochnost = (useStrongRules)? 0 : 1; // при обычных правилах ЕДИНИЦА, при более строгих - НОЛЬ
			
			if( ((VPOC[1] == VPOC[2]) || (Math.Abs(VPOC[1] - VPOC[2]) <= tochnost*TickSize))
			&& ((VPOC[1] == VPOC[3]) || (Math.Abs(VPOC[1] - VPOC[3]) <= tochnost*TickSize))
			){
				// у нас что-то есть! то есть ЭТО ПАТТЕРН!!!
				if(useNewPatternAlert) PlaySound("Alert4.wav");
				
				// это наша троица... складываем в копилку!
				if(!triVPOC_list.ContainsKey(BarNum1)) {
					triVPOC_list.Add(BarNum1, VPOC[1]);
				} else {
					triVPOC_list[BarNum1] = VPOC[1];
				}
				if(!triVPOC_list.ContainsKey(BarNum2)) {
					triVPOC_list.Add(BarNum2, VPOC[2]);
				} else {
					triVPOC_list[BarNum2] = VPOC[2];
				}
				if(!triVPOC_list.ContainsKey(BarNum3)) {
					triVPOC_list.Add(BarNum3, VPOC[3]);
				} else {
					triVPOC_list[BarNum3] = VPOC[3];
				}
			}
		}

		public void Show_TRI_VPOCs(bool show) {
			// копилка triVPOC_list
			//Print("ShowTRI: " + show);
			Int32 BarNum = 0;
			double Price = 0;
			int offset = 0;
			
			if(show)
			{
				// в реалтайме оно все будет дальше само рисоваться - тут надо вывести в цикле все имевшееся ранеее
				foreach(KeyValuePair<Int32, double> kvp in triVPOC_list)
				{
					BarNum=kvp.Key;
					Price=kvp.Value;
					offset = Bars.Count - 1 - BarNum;
					DrawDot(BarNum.ToString() + "vpocDot", false, offset, Price, myVPOCsColor);
				}
			} else {
				// в реалтайме оно все просто перестанет дальше само рисоваться - тут надо спрятать в цикле все имевшееся ранеее
				foreach(KeyValuePair<Int32, double> kvp in triVPOC_list)
				{
					BarNum=kvp.Key;
					Price=kvp.Value;
					offset = Bars.Count - 1 - BarNum;
					RemoveDrawObject(BarNum.ToString() + "vpocDot");
				}

				// а если при этом были включены ГОЛЫШИ или СКРЫТЕ, то часть их сейчас удалилась - надо их перерисовать!!!
				// только СКРЫТЫЕ самыми первыми рисуем!!!
				if(showHiddenVPOCs) Show_HIDDEN_VPOCs(true);
				if(showNakedVPOCs || showStrongNakedVPOCs) Show_NAKED_VPOCs(true);
			}

			// после всего  надо дать знать графику - мол, пора перерисовать картинку
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
			// впрочем, это можно сделать и там, откуда данная функция будет вызвана
		}
		//-------------------
		#endregion CHECK_TRI_VPOCs_PODRYAD

		#region SHOW_HIDE_ALL_HIDDEN
		//-------------------

		public void Show_ALL_VPOCs(bool show)
		{
			//Print("ShowALL: " + show);
			//Print("VPOCs Count: " + VPOC.Count);
			if(show)
			{
				// в реалтайме оно все будет дальше само рисоваться - тут надо вывести в цикле все имевшееся ранеее
				for(int i = 10; i <= Bars.Count - 1; i++) 
				{
					int offset = Bars.Count - 1 - i;
					DrawDot(i.ToString() + "vpocDot", false, offset, VPOC[offset], myVPOCsColor);
				}
			} else {
				// в реалтайме оно все просто перестанет дальше само рисоваться - тут надо спрятать в цикле все имевшееся ранеее
				for(int i = 10; i <= Bars.Count - 1; i++)
				{
					int offset = Bars.Count - 1 - i;
					RemoveDrawObject(i.ToString() + "vpocDot");
				}
			}

			// после всего  надо дать знать графику - мол, пора перерисовать картинку
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
			// впрочем, это можно сделать и там, откуда данная функция будет вызвана
		}

		public void Show_HIDDEN_VPOCs(bool show)
		{
			//Print("ShowHIDDEN: " + show);
			//Print("VPOCs Count: " + VPOC.Count);

			if(show)
			{
				// в реалтайме оно все будет дальше само рисоваться - тут надо вывести в цикле все имевшееся ранеее
				for(int i = 10; i <= Bars.Count - 1; i++)
				{
					int offset = Bars.Count - 1 - i;
					DrawDot(i.ToString() + "vpocDot", false, offset, VPOC[offset], myHiddenColor);
				}
			} else {
				// в реалтайме оно все просто перестанет дальше само рисоваться - тут надо спрятать в цикле все имевшееся ранеее
				for(int i = 10; i <= Bars.Count - 1; i++)
				{
					int offset = Bars.Count - 1 - i;
					RemoveDrawObject(i.ToString() + "vpocDot");
				}
			}

			// Если при этом были показаны ранее голыши и прочие паттерны - то надо их отрисовать теперь ПОВЕРХ скрытых
			if(showNakedVPOCs || showStrongNakedVPOCs) Show_NAKED_VPOCs(true);
			if(showTriPodryad) Show_TRI_VPOCs(true);

			// после всего  надо дать знать графику - мол, пора перерисовать картинку
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
			// впрочем, это можно сделать и там, откуда данная функция будет вызвана
		}

		//-------------------
		#endregion SHOW_HIDE_ALL_HIDDEN
		
		// ------------ для сохранения текущих положений профилей --------
		
		#region SAVE_READ_PROFILE_POSITION
		//----------------------------
		public void SaveProfilePosition() {
			if(!saveread) return; // если не надо сохранять - сразу выходим
			
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
					// и формат этих строчек такой: левый бар, ширина диапазона, ширина профиля, направление и скольжение - все остальное можно из этого вычислить...
					// только левый бар задан не НОМЕРОМ... не СМЕЩЕНИЕМ... а конкретным ВРЕМЕНЕМ!!! 
					
					//file.WriteLine(Times[0][R[i].leftOffset] + " | " + R[i].rangeWidth + " | " + R[i].profileWidth + " | " + (R[i].isLeftOriented?1:0) + " | " + (R[i].isLeftSlided?1:0));
					file.WriteLine(Times[0][R[i].leftOffset] + " | " + R[i].rangeWidth + " | " + R[i].profileWidth + " | " + R[i].isLeftOriented + " | " + R[i].isLeftSlided);
				}
				file.Close();
				//Print("*** SaveProfilePosition *** Записано в файл " + myFileName);
			} catch {
				try{
				//Print(" тут ошибка?");
				// ничего страшного! значит сохранимся позже!
				StreamWriter file = new StreamWriter(myFileName);
				for(int i=1; i<=4; i++) {
					//file.WriteLine(Times[0][R[i].leftOffset] + " | " + R[i].rangeWidth + " | " + R[i].profileWidth + " | " + (R[i].isLeftOriented?1:0) + " | " + (R[i].isLeftSlided?1:0));
					file.WriteLine(Times[0][R[i].leftOffset] + " | " + R[i].rangeWidth + " | " + R[i].profileWidth + " | " + R[i].isLeftOriented + " | " + R[i].isLeftSlided);
				}
				file.Close();
				} catch {
					// ничего страшного! значит сохранимся позже!
				}
			}
		}
		
		public int ReadProfilePosition() {
			if(!saveread) return 0; // если не надо сохранять - сразу выходим
			
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
				// и формат этих строчек такой: левый бар, ширина диапазона, ширина профиля, направление и скольжение - все остальное можно из этого вычислить...
				// только левый бар задан не НОМЕРОМ... не СМЕЩЕНИЕМ... а конкретным ВРЕМЕНЕМ!!! 				
				char c = '|';
				string[] lines = file.ReadLine().Split(c);
				if(lines.Length != 5)
				{
					// что-то не так - просто выходим, тут не страшно... Значит просто профили не восстановятся и всё...
					Print("*** ReadProfilePosition *** В прочитанной строке НЕ ПЯТЬ переменных! Их тут " + lines.Length);
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
						R[result].isLeftOriented = Convert.ToBoolean(lines[3]);
						R[result].isLeftSlided = Convert.ToBoolean(lines[4]);
						// а теперь надо понять - СЛАЙДЕР у нас или обычный профиль???
						if(R[result].isLeftSlided)
						{
							// СЛАЙДЕР!
							// Значит правый бар - самый последний ВИДИМЫЙ (не путать с самым последним на графике!!!)
							R[result].rightBar = Math.Min(ChartControl.Bars[0].Count - 1, ChartControl.LastBarPainted);
							R[result].rightOffset = ChartControl.Bars[0].Count - 1 - R[result].rightBar;
							R[result].rightTime = Time[R[result].rightOffset];
							// И значит левый бар ВЫЧИСЛЯЕМ а не берем из файла
							R[result].leftBar = R[result].rightBar - R[result].rangeWidth;
							R[result].leftOffset = R[result].rightOffset + R[result].rightBar;
							R[result].leftTime = Time[R[result].leftOffset];
							
							// И значит никаких "экстендед" и прочего!!!
							R[result].profileWidth = R[result].rangeWidth;
							R[result].isExtended = false;
						} else {
							// ОБЫЧНЫЙ
							R[result].leftBar = Bars.Count - 1 - num;
							R[result].rightBar = R[result].leftBar + R[result].rangeWidth;
							if(R[result].profileWidth != R[result].rangeWidth) {
								R[result].isExtended = true;
							} else {
								R[result].isExtended = false;
							}
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
			//if(!Historical && !readOnce) {
			if(!ReadyToRead && !readOnce) 
			{
				string MarketDataTimeStr = Bars.MarketData.Connection.Now.ToString("dd.MM.yyyy HH:mm");
				string BarTimeStr = Time[0].ToString("dd.MM.yyyy HH:mm");
				if(MarketDataTimeStr == BarTimeStr) 
				{
					Print(MarketDataTimeStr +  " | Bar.Time = " + BarTimeStr);
					ReadyToRead = true;
				}
				if(ReadyToRead)
				{
					// при первых же тиках реал-тайма - то есть уже после обработки исторической части
					// читаем сохраненные данные о профилях (если есть что читать)... Правда, иногда эти первые тики ждать приходится
					// на медленных рынках очень-очень долго... Вот по этой причине и был придуман "лайфхак" с приведенной выше
					// проверкой двух разных показателей времени!!!
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

					// Сначала из накопленных тиковых данных соберем информацию про ПОК текущей свечки
					//------------
					#region VOL-Ladder
					double vpoc_price=0;
					double vpoc_vol=0;
					double vol = 0;
					//------------
					foreach(KeyValuePair<int, double> kvp in vol_price.OrderByDescending(key => key.Value))
					{	// У нас в vol_price накопились данные по всем объемно-ценовым уровням ПРОШЛОЙ СВЕЧКИ
						// и мы их перебираем от самого мелкого до самого крупного - таким обращом находим ПОК!
						// Причем и сам ценовой уровень, и значение объема наэтом уровне - а значит можем при желании ФИЛЬТРОВАТЬ
						// эти значения объемов. Но пока не будем...
						vpoc_price=kvp.Key * TickSize;
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
//					foreach(KeyValuePair<Int32, double> kvp in vol_price){
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
						bar_vol_price.Add(prevBar,new Dictionary<Int32, double>());
					} else {
						// Если же эта запись есть (чего вообще-то быть не должно!!!) - очищаем ее
						bar_vol_price[prevBar].Clear();
					}
					// Теперь у нас нужная запись есть и она чистая - можно ее заполнять
					foreach(KeyValuePair<int, double> kvp in vol_price.OrderByDescending(key => key.Value))
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
                    foreach(KeyValuePair<Int32, double> kvp in vol_price.OrderByDescending(key => key.Value))
                    {	
						poc_price2=kvp.Key * TickSize;
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

				// Если включены СКРЫТЫЕ - нам отдельно никакой копилки не надо - просто мы ПОТОМ покажем ВСЕ ПОКИ "скрытым" цветом,
				// а следом уже будем выводить НЕ СКРЫТЫЕ и они лишние скрытые просто ЗАКРАСЯТ основным цветом...
				// Это на истории... в реалтайме же будем делать так:
				if(showHiddenVPOCs && !Historical) DrawDot(CurrentBar.ToString() + "vpocDot", false, 0, VPOC[0], myHiddenColor);
				// и эта часть ВСЕГДА ПЕРВАЯ!!!

				//------------
				// А если надо что-то конкретное - то уже именно ЭТО ищем и складываем в собственные "копилки"
				Check_Naked_VPOCs();
				Check_Tri_VPOCs_Podryad();
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
				//Print("LastBar = " + ChartControl.Bars[0].Count + " | LastVisibleBar = " + ChartControl.LastBarPainted);
				for(int num = 1; num<=4; num++)
				{
					R[num].leftOffset = Bars.Count - 1 - R[num].leftBar;
					R[num].rightOffset = Bars.Count - 1 - R[num].rightBar;
					R[num].extVpocOffset = Bars.Count - 1 - R[num].extVpocBar;
				}
			}
			
			// Вставка для передачи в стратегию
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
								// Ну ничего так ничего ;-)
								break;
						}
						// и не забываем проверить кнопки "панели управления"!!!
						switch(btnStat) {
							case BtnStatusType.BtnR:
								// меняем состояние переменной R[num].isRangeFilled
								R[curSelectedRange].isRangeFilled = !R[curSelectedRange].isRangeFilled;
								break;
							case BtnStatusType.BtnP:
								// меняем состояние переменной R[num].isProfileVisible
								R[curSelectedRange].isProfileVisible = !R[curSelectedRange].isProfileVisible;
								break;
							case BtnStatusType.BtnI:
								// меняем состояние переменной R[num].isInfoVisible
								R[curSelectedRange].isInfoVisible = !R[curSelectedRange].isInfoVisible;
								break;							
							case BtnStatusType.BtnE:
								// меняем состояние переменной R[num].isValVahExtended
								R[curSelectedRange].isValVahExtended = !R[curSelectedRange].isValVahExtended;
								break;
							case BtnStatusType.BtnL:
								// меняем состояние переменной R[num].isLeftOriented
								R[curSelectedRange].isLeftOriented = !R[curSelectedRange].isLeftOriented;
								break;
							case BtnStatusType.Btn4:
								// меняем состояние переменной R[num].isLeftSlided
								R[curSelectedRange].isLeftSlided = !R[curSelectedRange].isLeftSlided;
								if(R[curSelectedRange].isLeftSlided)
								{
									// профиль только что "приклеился" к правой границе ГРАФИКА, но нам нужно оставить на месте левую границу ДИАПАЗОНА - значит надо заменить значение ШИРИНЫ
									R[curSelectedRange].rangeWidth = ChartControl.LastBarPainted - R[curSelectedRange].leftBar;
									// также мы сразу ВЫКЛЮЧИМ принудительно:
									//  - заливку фона
									R[curSelectedRange].isRangeFilled = false;
									//  - вывод текстовой информации
									R[curSelectedRange].isInfoVisible = false;
									//  - и расширение линий VAL-VAH
									R[curSelectedRange].vahvalExtendedBackup = R[curSelectedRange].isValVahExtended;
									R[curSelectedRange].isValVahExtended = false;
									// и сразу ВКЛЮЧИМ принудительно
									//  - левосторонний профиль
									R[curSelectedRange].leftOrientedBackup = R[curSelectedRange].isLeftOriented;
									R[curSelectedRange].isLeftOriented = true;
									// все остальное будет автомтически перерасчитано само
									// Только не забываем еще "забэкапить" номер правого бара
									R[curSelectedRange].rightBarBackup = R[curSelectedRange].rightBar;
								} else {
									// профиль только что "отклеился" от правой границы ГРАФИКА и нам нужно восстановить статус 
									// правой границы ДИАПАЗОНА
									R[curSelectedRange].rightBar = R[curSelectedRange].rightBarBackup;
									// ориентации профиле влево-вправо
									R[curSelectedRange].isLeftOriented = R[curSelectedRange].leftOrientedBackup;
									// расширенного/нормального отображения линий VAH и VAL
									R[curSelectedRange].isValVahExtended = R[curSelectedRange].vahvalExtendedBackup;
								}
								break;
							case BtnStatusType.BtnSizeL:
								// меняем состояние переменной R[num].profileSize в МЕНЬШУЮ сторону
								R[curSelectedRange].profileSize --;
								if(R[curSelectedRange].profileSize < 0) R[curSelectedRange].profileSize = 0;
								break;
							case BtnStatusType.BtnSizeR:
								// меняем состояние переменной R[num].profileSize в БОЛЬШУЮ сторону
								R[curSelectedRange].profileSize ++;
								if(R[curSelectedRange].profileSize > 5) R[curSelectedRange].profileSize = 5;
								break;
							case BtnStatusType.BtnH:
								// меняем состояние переменной R[num].isHollow
								R[curSelectedRange].isHollow = !R[curSelectedRange].isHollow;
								break;
							default:
								// это типа статуса "ничего"
								// Ну ничего так ничего ;-)
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
						R[curCreatedRange].isLeftOriented	= false;
						R[curCreatedRange].isLeftSlided		= false;
						R[curCreatedRange].isRightSlided	= false;
						R[curCreatedRange].isRangeFilled	= true;
						R[curCreatedRange].isProfileVisible	= true;
						R[curCreatedRange].isHollow			= false;
						R[curCreatedRange].profileSize		= 3; // NORMAL
						R[curCreatedRange].isInfoVisible	= showSummary;
						R[curCreatedRange].isValVahExtended	= extVahVal;
						R[curCreatedRange].profileWidth		= initWidth;			// профиль нового диапазона равен по ширине самому диапазону
						R[curCreatedRange].isExtended		= false;				// и расширенный режим изначально НЕ включен, если не сказано иное
						R[curCreatedRange].isPlotted		= false;				// ну конечно же новый профиль еще на нарисован, он ведь не готов...
						R[curCreatedRange].isReady			= false;				// только создали диапазон - рано считатеь его готовым, надо еще вычислять объемы, ПОКи, Профиль и так далее
						rangesChanged = true;										// все поменялось!!!

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
					//DrawDiamond("curRange", false,R[curSelectedRange].leftOffset, R[curSelectedRange].Top, Color.Yellow);
					//DrawRectangle("curFrame", true, R[curSelectedRange].leftOffset, R[curSelectedRange].Bottom, R[curSelectedRange].rightOffset, R[curSelectedRange].Top, borderColor, R[curSelectedRange].rangeColor, 0);
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
					//DrawDiamond("curRange", false,R[curSelectedRange].leftOffset, R[curSelectedRange].Top, Color.Yellow);
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
			} else if(e.Shift && e.Control && e.KeyCode==Keys.G) {
				// Включаем или прячем ЛУЧИ на ПОКах
				showRays = !showRays;
				Print("Rays is " + showRays);
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
				//showMouseCursorChanges = !showMouseCursorChanges;
				// нет - для себя я тут поставлю включалку и отключалку "дырок" в мрофиле на МЕЛКИХ обьемах (потому и М)
				showStrongProfile = !showStrongProfile;
				R[1].isReady = false;
				R[2].isReady = false;
				R[3].isReady = false;
				R[4].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
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
				if(R[curSelectedRange].isLeftSlided)
				{
					R[curSelectedRange].rangeWidth ++;
				} else {
					R[curSelectedRange].leftBar -= 1;
					if(R[curSelectedRange].leftBar <= 2) R[curSelectedRange].leftBar = 2;
				}				
				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Control && e.KeyCode==Keys.OemPeriod){
				// Двигаем ЛЕВУЮ границу текущего диапазона ВПРАВО
				//showRange = !showRange;
				//Print("CTRL+RIGHT");
				if(R[curSelectedRange].isLeftSlided)
				{
					if(R[curSelectedRange].rangeWidth > 5) R[curSelectedRange].rangeWidth --;
				} else {
					R[curSelectedRange].leftBar += 1;					
					if(R[curSelectedRange].leftBar >= R[curSelectedRange].rightBar - 2) R[curSelectedRange].leftBar = R[curSelectedRange].rightBar - 2;
				}
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
				R[curSelectedRange].isReady = false;
				this.ChartControl.ChartPanel.Invalidate();
				this.ChartControl.Refresh();
			} else if(e.Shift && e.KeyCode==Keys.OemPeriod){
				// Двигаем ПРАВУЮ границу текущего диапазона ВПРАВО
				//showRange = !showRange;
				//Print("SHIFT+RIGHT");
				R[curSelectedRange].rightBar += 1;
				if(R[curSelectedRange].rightBar >= Bars.Count - 1) R[curSelectedRange].rightBar = Bars.Count - 1;
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
			//} else if(!e.Alt && !e.Control && !e.Shift && e.KeyCode==Keys.Tab){
			} else if((e.Control && e.KeyCode==Keys.Tab) || (e.KeyCode==Keys.Tab)){
				// Меняем номер ТЕКУЩЕГО диапазона для дальнейшего управления клавишами в сторону "слева-направо"
				//Print("TAB");
				curSelectedRange ++;
				if(curSelectedRange > Math.Min(nowRanges,4)) curSelectedRange = 1;  // ДА! Вот так! Нам же надо еще учесть РЕАЛЬНОЕ количество имеющихся профилей!!!
			} else if(!e.Alt && !e.Control && e.Shift && e.KeyCode==Keys.A){
				// Show ALL VPOCs of Bars - Показываем ВСЕ ПОКи баров!!! - Ну или прячем их!
				showAllVPOCs = !showAllVPOCs;
				if(showAllVPOCs)
				{
					showVPOCsVolume	= false;
					showNakedVPOCs	= false;
					showStrongNakedVPOCs	= false;
					showTriPodryad	= false;
					showHiddenVPOCs = false;
				}
				Show_ALL_VPOCs(showAllVPOCs);
			} else if(!e.Alt && !e.Control && e.Shift && e.KeyCode==Keys.H){
				// Show HIDDEN VPOCs of Bars - Показываем СКРЫТЫЕ ПОКи баров!!! - Ну или прячем их!
				showHiddenVPOCs = !showHiddenVPOCs;
				if(showHiddenVPOCs)
				{
					showAllVPOCs	= false;	// если включен режим СКРЫТЫХ - то уж точно не нужен режим ВСЕХ!!!
				}
				Show_HIDDEN_VPOCs(showHiddenVPOCs);
			} else if(!e.Alt && !e.Control && e.Shift && e.KeyCode==Keys.N){
				// Show NAKED VPOCs of Bars - Показываем ГОЛЫЕ ПОКи баров!!! - Ну или прячем их!
				showNakedVPOCs = !showNakedVPOCs;
				if(showNakedVPOCs)
				{
					showAllVPOCs = false;
				}
				Show_NAKED_VPOCs(showNakedVPOCs);
			} else if(!e.Alt && !e.Control && e.Shift && e.KeyCode==Keys.S){
				// Show STRONG NAKED VPOCs of Bars - Показываем СИЛЬНЫЕ ГОЛЫЕ ПОКи баров!!! - Ну или прячем их!
				showStrongNakedVPOCs = !showStrongNakedVPOCs;
				if(showStrongNakedVPOCs)
				{
					showAllVPOCs = false;
				}
				Show_NAKED_VPOCs(showStrongNakedVPOCs);
			} else if(!e.Alt && !e.Control && e.Shift && e.KeyCode==Keys.T){
				// Show TRI VPOCs of Bars - Показываем ТРИ ПОДРЯД ПОКа баров!!! - Ну или прячем их!
				showTriPodryad = !showTriPodryad;
				if(showTriPodryad)
				{
					showAllVPOCs = false;
				}
				Show_TRI_VPOCs(showTriPodryad);
			}


			// Выведем статус наших основных переключателей-хоткеев, чтобы было наглядно
			string statHotKeys = "";
			if(showAllVPOCs) {
				statHotKeys += "|A=on";
			} else {
				statHotKeys += "|A=off";
			}			
//			if(showMouseCursorChanges) {
//				statHotKeys += "|M=on";
//			} else {
//				statHotKeys += "|M=off";
//			}
			if(showStrongProfile) {
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
				//statHotKeys += "|*=off";
			}			
			Show_Debug_Screen(showDebugScreen); // теперь при включенном ДЕБАГ-окне ВСЕ нажатия на хоткеи будут тут же менять информацию в нем
			DrawTextFixed("HotKeyStat", statHotKeys + "|\r\n\r\n", TextPosition.BottomLeft, helpFontColor, new Font("Arial", 7, FontStyle.Regular), Color.Transparent, Color.Transparent, 0);
			if(nowRanges > 0) //DrawDiamond("curRange", false,R[curSelectedRange].leftOffset, R[curSelectedRange].Top, Color.Yellow);
			this.ChartControl.ChartPanel.Invalidate();
			this.ChartControl.Refresh();
	
		}
		#endregion HotKeysLL
				
//		#region Plots/RangeVPOC
		//------------
		#region PLOT
		public override void Plot(Graphics graphics, Rectangle bounds, double min, double max)	
		{	
			RecalculateVPOCs();
			//PlotVPOCs();	// пока я вижу что он нужен только при удалении
			
			//Print("LastBar = " + ChartControl.Bars[0].Count + " | LastVisibleBar = " + ChartControl.LastBarPainted + " | LAST = " + Math.Min(ChartControl.Bars[0].Count, ChartControl.LastBarPainted));
			
			// Отрисовываем базовые вещи, только сам индикатор делаем прозрачным
			Plots[0].Pen.Color = Color.Transparent;
			base.Plot(graphics,bounds,min,max);	
			
			try
			{
				SmoothingMode oldSmoothingMode = graphics.SmoothingMode;
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				
				// тут рисуем что надо
				
				if(nowRanges > 0) 
				{
					//Print("* OnRender * ==========");
					for (int i=1; i <= nowRanges; i++)
					{
						//Print("Profile #" + i);
						if(showProfile) PlotProfileByNum(i, graphics);
						PlotRangeByNum(i, graphics);
					}
					// Попробуем снять выделение с прямоугольника путем отрисовки нового прямоугольника
					//DrawRectangle("zeroFrame", true, 2, High[0], 1, Low[0], borderColor, Color.Transparent, 0);
					// НЕ РАБОТАЕТ!!! ТОГДА ВОТ ТАК:
//					Print("STATUS: " + lastStatus);
//					if(lastStatus == StatusType.NotMoving)
//					{
//						RemoveDrawObject("curFrame");
//					}
					// РАБОТАЕТ!!! но попробуем перенести внутрь рисования диапазонов
				} else {
					// кое-что надо подчистить за собой
					RemoveDrawObject("curFrame");
					//RemoveDrawObject("zeroFrame");
				}
				
				// а после окончания делаем возврат
				graphics.SmoothingMode = oldSmoothingMode;
			} catch (Exception ex) {
				//if (this.Print == null)
				//    return;
				Print (ex.ToString ());
			}
			

			//Print("Plot:  Now=" + nowRanges + " | Selected=" + curSelectedRange + " | Created=" + curCreatedRange);
		} // END of Plot()
		//------------
		#endregion PLOT
				
		//------------
		#region PLOT_RANGE_by_NUM
		public void PlotRangeByNum(int num, System.Drawing.Graphics graphics) {				

			// определяем базовые координаты и размеры
			float x = ChartControl.GetXByBarIdx(Bars, R[num].leftBar);
			float y = ChartControl.GetYByValue (Bars, R[num].Top);
			float yV = ChartControl.GetYByValue (Bars, R[num].VpocPrice);
			float yVAH = ChartControl.GetYByValue (Bars, R[num].VAH);
			float yVAL = ChartControl.GetYByValue (Bars, R[num].VAL);
			float deltaX = (float)(ChartControl.GetXByBarIdx(Bars, R[num].rightBar) - ChartControl.GetXByBarIdx(Bars, R[num].leftBar));
			float deltaXV = (float)(ChartControl.GetXByBarIdx(Bars, R[num].rightBar) - ChartControl.GetXByBarIdx(Bars, R[num].leftBar));
			float deltaY = (float)(ChartControl.GetYByValue (Bars, R[num].Bottom) - ChartControl.GetYByValue (Bars, R[num].Top));
			float offsetY = (deltaX > 100F) ? 0F : 20F;
			SolidBrush myBrushV	= new SolidBrush(vpocLineColor);
			SolidBrush myBrushP	= new SolidBrush(priceColor);
			SolidBrush myBrushS	= new SolidBrush(summaryColor);

			// Если включен режим показа ДИАПАЗОНА - рисуем его
			if(showRange) 
			{
				// Если надо - выводим информацию
				
				if(showSummary && R[num].isInfoVisible) 
				{
					StringFormat formatS = new StringFormat (StringFormatFlags.NoClip);
					formatS.Alignment = StringAlignment.Far;
					graphics.DrawString ("Bars: " + R[num].rangeBars + "\nVpocVol: " + R[num].VpocVol.ToString() + "\nRangeVol: " + R[num].rangeVol.ToString(),
						new Font (FontFamily.GenericSansSerif, (float)summaryFontSize, FontStyle.Regular),
						myBrushS, x + deltaX, y + deltaY + 5 + offsetY, formatS);				
				}
				if (showPrice && R[num].isInfoVisible)
				{
					StringFormat formatP = new StringFormat (StringFormatFlags.NoClip);
					formatP.Alignment = StringAlignment.Far;
					graphics.DrawString (R[num].VpocPrice.ToString() + " VPOC =>",
						new Font (FontFamily.GenericSansSerif, (float)priceFontSize, FontStyle.Bold),
						myBrushP, x - 3, yV - 2 - (float)(priceFontSize/2), formatP);
				}
			}			
			
			// Теперь нарисуем линию VPOC внутри диапазона
			graphics.FillRectangle (myBrushV, x, yV - vpocLineSize / 2, deltaX, vpocLineSize);
			// А вот теперь надо еще эту линию продолжать ЗА диапазон (если есть куда продолжать)
			float xE = ChartControl.GetXByBarIdx(Bars, R[num].rightBar);
			float deltaXE = 0F;
			
			if(R[num].extVpocBar == 0) {
				deltaXE = (float)(ChartControl.GetXByBarIdx(Bars, ChartControl.LastBarPainted) - ChartControl.GetXByBarIdx(Bars, R[num].rightBar)) + 5*ChartControl.BarSpace;
			} else {
				deltaXE = (float)(ChartControl.GetXByBarIdx(Bars, R[num].extVpocBar) - ChartControl.GetXByBarIdx(Bars, R[num].rightBar));
			}
			
			// и нарисуем линию VPOC снаружи (так называемый Extended VPOC - только на для СЛАЙДЕРА!!!
			if(!R[num].isLeftSlided)
			{
				if(useTrendColor) {
					if(R[num].VpocType == 1) {					
						Pen linePen = new Pen(vpocExtShortColor, vpocExtShortSize);
						linePen.DashStyle = vpocExtShortStyle;
						graphics.DrawLine(linePen, xE, yV, xE + deltaXE, yV);
					} else if(R[num].VpocType == -1) {
						Pen linePen = new Pen(vpocExtLongColor, vpocExtLongSize);
						linePen.DashStyle = vpocExtLongStyle;
						graphics.DrawLine(linePen, xE, yV, xE + deltaXE, yV);
					}
				} else {
					if(R[num].VpocType != 0) {
						Pen linePen = new Pen(vpocLineColor, vpocLineSize);
						linePen.DashStyle = vpocLineStyle;
						graphics.DrawLine(linePen, xE, yV, xE + deltaXE, yV);
					}
				}
			}
			// Теперь нарисуем линии VAH и VAL ( в зависимости от настроек - только внутри диапазона или же до самого правого края графика )
			// В отличии от линии ПОКа эти линии не будут заканчиваться при пересечении со свечками на графике!!!
			// Вообще-то, они рисуются в ПРОФИЛЕ, а тут мы это будем далеть ТОЛЬКО если показ профиля отключен!!!
			if(!showProfile)
			{
				if(extVahVal && !R[num].isLeftOriented) {
					// если надо - уводим линию за горизонт в далёкие дали... конечно в том случае, когда у нас весь профиль смотрит вправо
					deltaXV = (float)(ChartControl.GetXByBarIdx(Bars, ChartControl.LastBarPainted) - ChartControl.GetXByBarIdx(Bars, R[num].leftBar)) + 5*ChartControl.BarSpace;
				} else {
					// а если не надо - мы ширину уже получили выше и тут ничего не делаем
				}

				myPen = new Pen(vahLineColor, vahLineSize);
				myPen.DashStyle = vahLineStyle;
				graphics.DrawLine(myPen, x, yVAH, x + deltaXV, yVAH);
				myPen = new Pen(valLineColor, valLineSize);
				myPen.DashStyle = valLineStyle;
				graphics.DrawLine(myPen, x, yVAL, x + deltaXV, yVAL);
			}
//			Print("* RANGE * -> R[" + num +"].VAH = " + R[num].VAH + "\t yVAH = " + yVAH + "\t R[" + num +"].VAL = " + R[num].VAL + "\t yVAL = " + yVAL);
			
			// И еще рисуем что-то типа кнопки зкрытия окна - пока просто квадратик с диагональным крестиком
			// ТОЛЬКО НЕ У ПРИКЛЕЕННОГО/СКОЛЬЗЯЩЕГО профиля!!!
			if(((showRange) || ( num == curSelectedRange)) && !R[num].isLeftSlided)
			{
				myPen = new Pen(borderColor, 1);
				graphics.DrawRectangle(myPen, x + deltaX - 10, y, 10, 10);
				graphics.DrawLine(myPen, x + deltaX - 10, y, x + deltaX, y + 10);
				graphics.DrawLine(myPen, x + deltaX - 10, y + 10, x + deltaX, y);
			}

			// Ну и в самом конце можно нарисовать рамку с фоном
			// Причем именно РАМКУ будем рисовать ТОЛЬКО на текущем выделенном диапазоне
			// ДАЖЕ ЕСЛИ РЕЖИМ ПОКАЗА ДИАПАЗОНОВ ВЫКЛЮЧЕН - будет хотя бы РАМКА на текущем диапазоне!!!
			
			if( num == curSelectedRange) 
			{
				// Теперь заливка фона
				if(showRange && R[num].isRangeFilled) 
				{
					SolidBrush myBrushBG	= new SolidBrush(Color.FromArgb(regTransparency, R[num].rangeColor));
					graphics.FillRectangle(myBrushBG, x, y, deltaX, deltaY);
				}
				// Теперь рамка, вернее её замена: у активного диапазона будем рисовать левую или правую границы жирной линией
				// в тот момент, когда мышка находится НАД этой границей. Или будем рисовать ОБЕ, если мышка находится внутри диапазона.
				// Повторяю - АКТИВНОГО диапазона!!! Другие игнорируем! И НЕ РИСУЕМ НА СЛАЙДЕРЕ!!!
				
//				Print("STATUS: " + lastStatus);
				if(!R[num].isLeftSlided && lastStatus != StatusType.NotMoving && lastStatusNum == num)
				{
					//DrawRectangle("curFrame", true, R[num].leftOffset, R[num].Bottom, R[num].rightOffset, R[num].Top, borderColor, Color.Transparent, 0);
					if(lastStatus == StatusType.ReadyToMove1 || lastStatus == StatusType.ReadyToMove2 || lastStatus == StatusType.ReadyToMove3 || lastStatus == StatusType.ReadyToMove4)
					{
						DrawLine("curFrameL", true, R[num].leftOffset, R[num].Bottom, R[num].leftOffset, R[num].Top - TickSize, borderColor, borderLineStyle, borderLineSize);
						RemoveDrawObject("curFrameR");
					}
					if(lastStatus == StatusType.ReadyToChange1 || lastStatus == StatusType.ReadyToChange2 || lastStatus == StatusType.ReadyToChange3 || lastStatus == StatusType.ReadyToChange4)
					{
						DrawLine("curFrameR", true, R[num].rightOffset, R[num].Bottom, R[num].rightOffset, R[num].Top - TickSize, borderColor, borderLineStyle, borderLineSize);
						RemoveDrawObject("curFrameL");
					}
					if(lastStatus == StatusType.MovingInside1 || lastStatus == StatusType.MovingInside2 || lastStatus == StatusType.MovingInside3 || lastStatus == StatusType.MovingInside4)
					{
						DrawLine("curFrameL", true, R[num].leftOffset, R[num].Bottom, R[num].leftOffset, R[num].Top - TickSize, borderColor, borderLineStyle, borderLineSize);
						DrawLine("curFrameR", true, R[num].rightOffset, R[num].Bottom, R[num].rightOffset, R[num].Top - TickSize, borderColor, borderLineStyle, borderLineSize);
					}
				} else {
					//RemoveDrawObject("curFrame");
					RemoveDrawObject("curFrameL");
					RemoveDrawObject("curFrameR");
				}

				// И еще текущий диапазон отметим кружочком слева вверху (вместо прошлого "алмазика")
				if(!R[num].isLeftSlided)
				{
					graphics.FillEllipse(new SolidBrush(Color.Yellow), x-5, y-5, 10, 10);
					graphics.DrawEllipse(new Pen(new SolidBrush(borderColor), 1), x-5, y-5, 10, 10);
				}
				// И - новшество - у активного диапазона покажем "панель управления"
				Pen myPenD = new Pen(btnDisableColor, 1);
				Pen myPenE = new Pen(btnEnableColor, 1);
				Pen myPenA = new Pen(btnActiveColor, 1);
				SolidBrush myBrush	= new SolidBrush(btnDisableColor);
				SolidBrush myBrushD	= new SolidBrush(btnDisableColor);
				SolidBrush myBrushE	= new SolidBrush(btnEnableColor);
				SolidBrush myBrushA	= new SolidBrush(btnActiveColor);
				StringFormat formatBtn = new StringFormat (StringFormatFlags.NoClip);
				formatBtn.Alignment = StringAlignment.Center;
				float panelX =  x + 5F;
				//float panelX =  R[num].isLeftSlided ? x + 5F : x + deltaX - 66F;
				float panelY = y - 20F;
				float btnW = 15F; // ширина кнопки
				float btnH = 15F; // высота кнопки
				float btnS = 2F;  // расстояние между кнопками
				// начинаем рисовать кнопки таким цветом, который соответствует значению переключателей
				
				// isRightSlided
//				myPen = R[num].isRightSlided ? myPenA : myPenE;
//				myBrush = R[num].isRightSlided ? myBrushA : myBrushE;
//				graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
//				graphics.DrawString ("<<",
//					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
//					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
//				);
//				panelX += btnW + btnS;
				
				// Буква "R" - RANGE FILL!!!			
				myPen = R[num].isRangeFilled ? myPenA : myPenE;
				myBrush = R[num].isRangeFilled ? myBrushA : myBrushE;
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawString ("R",
					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
				);
				panelX += btnW + btnS;

				// Буква "P" - PROFILE VISIBLE!!!		
				myPen = R[num].isProfileVisible ? myPenA : myPenE;
				myBrush = R[num].isProfileVisible ? myBrushA : myBrushE;
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawString ("P",
					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
				);
				panelX += btnW + btnS;

				// Буква "I" - INFO VISIBLE!!!		
				myPen = R[num].isInfoVisible ? myPenA : myPenE;
				myBrush = R[num].isInfoVisible ? myBrushA : myBrushE;
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawString ("I",
					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
				);
				panelX += btnW + btnS;

				// Буква "E" - EXTENDED LINES VAL-VAH!!!
				if(R[num].isLeftOriented)
				{
					// когда профиль является СЛАЙДЕРОМ - кнопку "E" делаем недоступной
					myPen = myPenD;
					myBrush = myBrushD;
				} else {
					// а иначе - выбор по значению переменной
					myPen = R[num].isValVahExtended ? myPenA : myPenE;
					myBrush = R[num].isValVahExtended ? myBrushA : myBrushE;
				}
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawString ("E",
					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
				);				
				panelX += btnW + btnS;

				// Буква "H" - HOLLOW PROFILE MODE!!!
				myPen = R[num].isHollow ? myPenA : myPenE;
				myBrush = R[num].isHollow ? myBrushA : myBrushE;
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawString ("H",
					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
				);
				panelX += btnW + btnS;

				// Теперь кнопки снизу
				panelY = y + deltaY + 5F;
				panelX =  x + 5F;
				
				// Size: уменьшаем
				myPen = (R[num].profileSize > 0) ? myPenA : myPenD;
				myBrush = (R[num].profileSize > 0) ? myBrushA : myBrushD;
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawString ("<=",
					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
				);
				panelX += btnW + btnS;
				
				// Size: текущий статус
				myPen = myPenE;
				myBrush = myBrushE;
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				//graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				string btnStr = "";
				if(R[num].profileSize == 0) btnStr = "M"; // MINIMUM - 10% от реальной ширины диапазона
				if(R[num].profileSize == 1) btnStr = "Q"; // QUATER - 25% от реальной ширины диапазона
				if(R[num].profileSize == 2) btnStr = "H"; // HALF	- 50% от реальной ширины диапазона
				if(R[num].profileSize == 3) btnStr = "N"; // NORMAL - 100% от реальной ширины диапазона
				if(R[num].profileSize == 4) btnStr = "D"; // DOUBLE - 200% от реальной ширины диапазона
				if(R[num].profileSize == 5) btnStr = "T"; // TRIPLE - 300% от реальной ширины диапазона
				graphics.DrawString (btnStr,
					new Font (FontFamily.GenericSansSerif, 8F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 8F)/2) - 2F, formatBtn
				);
				panelX += btnW + btnS;

				// Size: увеличиваем
				myPen = (R[num].profileSize < 5) ? myPenA : myPenD;
				myBrush = (R[num].profileSize < 5) ? myBrushA : myBrushD;
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawString ("=>",
					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
				);
				panelX += btnW + btnS;

				// Буква "L" - LEFT-ORIENTED!!!
				myPen = R[num].isLeftOriented ? myPenA : myPenE;
				myBrush = R[num].isLeftOriented ? myBrushA : myBrushE;
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawString ("L",
					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
				);				
				panelX += btnW + btnS;

				// Буква "S" - SLIDER!!!
				myPen = R[num].isLeftSlided ? myPenA : myPenE;
				myBrush = R[num].isLeftSlided ? myBrushA : myBrushE;
				//graphics.DrawRectangle(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawEllipse(myPen, panelX, panelY, btnW, btnH);
				graphics.DrawString ("S",
					new Font (FontFamily.GenericSansSerif, 7F, FontStyle.Bold),
					myBrush, panelX + (float)(btnW/2), panelY + (float)((btnH - 7F)/2) - 2F, formatBtn
				);
				panelX += btnW + btnS;

				
			} else {
				if(showRange && R[num].isRangeFilled) 
				{
					SolidBrush myBrushBG	= new SolidBrush(Color.FromArgb(regTransparency, R[num].rangeColor));
					graphics.FillRectangle (myBrushBG, x, y, deltaX, deltaY);
				}
			}
			
			R[num].isPlotted = true; 	// укажем, что мы уже все отрисовали!!!
		}  // END of PlotRangeByNum()
		
		
		//------------
		#endregion PLOT_RANGE_by_NUM

		//------------
		#region PLOT_PROFILE_by_NUM
		public void PlotProfileByNum(int num, System.Drawing.Graphics graphics)
		{
		
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
			
			// а теперь на полученные значение ширины профиля накладываем маску из "размеров"
			int percent = 100;
			if(R[num].profileSize == 0) percent = 10; // MINIMUM - 10% от реальной ширины диапазона
			if(R[num].profileSize == 1) percent = 25; // QUATER - 25% от реальной ширины диапазона
			if(R[num].profileSize == 2) percent = 50; // HALF - 50% от реальной ширины диапазона
			if(R[num].profileSize == 3) percent = 100; // NORMAL - 100% от реальной ширины диапазона
			if(R[num].profileSize == 4) percent = 200; // DOUBLE - 200% от реальной ширины диапазона
			if(R[num].profileSize == 5) percent = 300; // TRIPLE - 300% от реальной ширины диапазона
			R[num].profileWidth = R[num].profileWidth * percent / 100;
			float profilePixelW = (_X_from_BarNum(R[num].rightBar) - _X_from_BarNum(R[num].leftBar)) * percent / 100;
			
			// Так... у нас объем на ПОКе - это будет 100% нашего профиля. И количество баров в профиле - тоже 100% профиля...
			// То есть можем узнать, сколько ОБЬЕМА приходится на "один БАР по ширине" а не по фактическому обьему этого конкретного бара
			// Просто делим объем на количество баров...
			double inOneBar = R[num].VpocVol / R[num].profileWidth;
			double inOnePixel = R[num].VpocVol / profilePixelW;
			
			float y = ChartControl.GetYByValue (Bars, R[num].Top);
			float yVAH = ChartControl.GetYByValue (Bars, R[num].VAH);
			float yVAL = ChartControl.GetYByValue (Bars, R[num].VAL);
			float deltaX = 0;

			float startX = 0;
			float endX	 = 0;
			float startY = 0;
			float startYL = 0;
			float endY	 = 0;
			float endYL	 = 0;
			float widthDX = 0;
			float heightDX = 0;
			float heightDXL = 0;
			
//			Print("* OnRender * PlotProfile num=" + num);						
			
			if(R[num].isProfileVisible)
			{
				//foreach(KeyValuePair<Int32, double> kvp in R[num].vol_price.OrderByDescending(key => key.Value))		// сортировка по ОБЪЁМАМ
				foreach(KeyValuePair<Int32, double> kvp in R[num].vol_price.OrderByDescending(key => key.Key))			// сортировка по ЦЕНАМ
				{
					// так... у нас объем на ПОКе - это будет 100% нашего профиля
					double profileLineWidth = kvp.Value / inOneBar;
					double profileLineWidthPixel = kvp.Value / inOnePixel;
					
					// и еще немножко красоты
					if(showStrongProfile) {
						// если включен этот режим - надо проверить, "достойна" ли данная линия гистограммы вообще быть на профиле???
						if(profileLineWidth < R[num].profileWidth/10) {
							// если эта наша линия по ширине меньше чем 10% от ширины профиля - игнорируем ее и выходим из цикла
							break;
						}
						if(profileLineWidthPixel < profilePixelW/10) {
							// если эта наша линия по ширине меньше чем 10% от ширины профиля - игнорируем ее и выходим из цикла
							break;
						}
					}				
					int profileLineBars = Convert.ToInt32(profileLineWidth);
					int profileLinePixels = Convert.ToInt32(profileLineWidthPixel);
					double price = kvp.Key * TickSize;
					int priceBar = kvp.Key;
					int prVAH = Convert.ToInt32(R[num].VAH / TickSize);
					int prVAL = Convert.ToInt32(R[num].VAL / TickSize);
					int prVPOC = Convert.ToInt32(R[num].VpocPrice / TickSize);
	//				Print("KEY = " + price + "\tVALUE = " + kvp.Value);
					
					// и можем теперь это отобразить				
					// переводим бары и цены в координаты X и Y
					if (R[num].isLeftOriented)
					{
						// это ПЕРЕВЕРНУТЫЙ влево вариант
						startX	= _X_from_BarNum(R[num].rightBar - profileLineBars);
						startX	= _X_from_BarNum(R[num].rightBar) - profileLinePixels;
						endX	= _X_from_BarNum(R[num].rightBar);
					} else {
						// это НОРМАЛЬНЫЙ вариант, тут нам не надо выдумывать ничего
						startX	= _X_from_BarNum(R[num].leftBar);
						endX	= _X_from_BarNum(R[num].leftBar + profileLineBars);
						endX	= _X_from_BarNum(R[num].leftBar) + profileLinePixels;
					}
					widthDX	= Math.Abs(endX - startX);
					startY	= _Y_from_Price(price + TickSize / 2);
					startYL = _Y_from_Price(price) - Convert.ToInt32(vpocLineSize / 2);
					endY	= _Y_from_Price(price - TickSize / 2);
					endYL = _Y_from_Price(price) + Convert.ToInt32(vpocLineSize / 2);
					// чтобы у нас "бары" профиля не вылезали за край рамки диапазона - обрежем лишнее
					if(startY < R[num]._Yt) startY = R[num]._Yt;
					if(startYL < R[num]._Yt) startYL = R[num]._Yt;
					if(endY > R[num]._Yb) endY = R[num]._Yb;
					if(endYL > R[num]._Yb) endYL = R[num]._Yb;
					// Вот теперь можно уже вычислять высоту "бара" профиля
					heightDX = endY - startY;
					heightDXL = endYL - startYL;
									
	//				Print("vpocValueAreaColor: " + vpocValueAreaColor);
					
					// Определеямся с нужным цветом
					Color curBarColor;
					if(priceBar == prVPOC) 
					{
						// вот так мы рисуем линию ПОКа в профиле
						curBarColor = vpocLineColor;
						// и сохраним его ширину для последующей отрисовки линий VAH и VAL
						deltaX = widthDX;
					} else if(priceBar <= prVAH && priceBar >= prVAL) 
					{
						// вот так мы рисуем внутренние бары VALUE AREA в профиле
						curBarColor = vpocValueAreaColor;
					} else {
						// а вот так рисуем остальные бары гистограммы профиля за пределами VALUE AREA
						curBarColor = vpocProfileColor;
					}
	//				Print("* PROFILE * -> R[" + num +"].VAH = " + R[num].VAH + "\t price = " + price + "\t R[" + num +"].VAL = " + R[num].VAL + "\t Color = " + curBarColor);
					
					// И теперь рисуем либо БАР (прямоугольник), либо ЛИНИЮ
					//SolidBrush myBrushPR	= new SolidBrush(curBarColor);
					SolidBrush myBrushPR	= new SolidBrush(Color.FromArgb(profTransparency, curBarColor));
					if(showRectangles) {
						// если НЕ включен режим "пустоты" - заливаем прямоугольник выбранным цветом
						if(!R[num].isHollow) graphics.FillRectangle(myBrushPR, startX, startY, widthDX, heightDX);
						// если цвет бара гистограммы не равен обычному - то обведем его рамочкой обычного цвета
						if(curBarColor != vpocProfileColor) {
							myPen = new Pen(vpocProfileColor, 1);
						}
						// а если цвет бара гистограммы равен обычному - то обведем его рамочкой цвета ValueArea
						if(curBarColor == vpocProfileColor) {
							myPen = new Pen(vpocValueAreaColor, 1);
						}
						// вот, собственно - ОБВОДИМ-с...
						graphics.DrawRectangle(myPen, startX, startY, widthDX, heightDX);
					} else {
						myPen = new Pen(curBarColor, vpocLineSize);
						//graphics.FillRectangle(myBrushPR, startX, startYL, widthDX, heightDXL);
						graphics.DrawLine(myPen, startX, startY, startX + widthDX, startY);
					}
					
					// А еще если включен режим ЛУЧЕЙ - нарисуем и лучик
					if(showRays) {
						DrawRay("VPOC_Ray"+num, false, R[num].leftTime, R[num].VpocPrice, DateTime.Now, R[num].VpocPrice, raysColor, raysLineStyle, raysLineSize);
					}
					//Print("P="+kvp.Key + " V="+kvp.Value + "B="+profileLineBars + " O="+profileLineOffset);
				}
			}
			// Теперь нарисуем линии VAH и VAL ( в зависимости от настроек - только внутри диапазона или же до самого правого края графика )
			// В отличии от линии ПОКа эти линии не будут заканчиваться при пересечении со свечками на графике!!!
			if(R[num].isValVahExtended && !R[num].isLeftSlided) {
				// если надо - уводим линию за горизонт в далёкие дали... конечно в том случае, когда сам профиль смотрит вправо
				//deltaX = (float)(ChartControl.GetXByBarIdx(Bars, ChartControl.LastBarPainted) - ChartControl.GetXByBarIdx(Bars, R[num].leftBar)) + 5*ChartControl.BarSpace;
				endX = (float)(ChartControl.GetXByBarIdx(Bars, ChartControl.LastBarPainted)) + 5*ChartControl.BarSpace;
			} else {
				// а если не надо
				endX = _X_from_BarNum(R[num].rightBar);
			}
			startX = _X_from_BarNum(R[num].leftBar);
			myPen = new Pen(vahLineColor, vahLineSize);
			myPen.DashStyle = vahLineStyle;
			graphics.DrawLine(myPen, startX, yVAH, endX, yVAH);
			myPen = new Pen(valLineColor, valLineSize);
			myPen.DashStyle = valLineStyle;
			graphics.DrawLine(myPen, startX, yVAL, endX, yVAL);

			
			
			
			// В заключении рисования профиля проведем по его левой (или правой - смотря куда направлен) границе вертикальную линию
			myPen = new Pen(vpocProfileColor, 1);
			graphics.DrawLine(myPen, R[num].isLeftOriented?R[num]._Xr:R[num]._Xl, R[num]._Yt, R[num].isLeftOriented?R[num]._Xr:R[num]._Xl, R[num]._Yb);
			// И укажем, что мы уже все отрисовали!!!
			R[num].isPlotted = true; 	
		}
		#endregion PLOT_PROFILE_by_NUM
		
		
		//------------		
//		#endregion Plots/RangeVPOC

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
				msg += "\r\n  CTRL+\"<\" or CTRL+\">\"      CHANGE Left Border of Selected Range\r\n";
				msg += "\r\n  Shift+\"P\"                 Toggle Show|Hide PROFILE";
				msg += "\r\n  Shift+\"R\"                 Toggle Show|Hide RANGE";
				msg += "\r\n  Shift+\"L\"                 Toggle Lines|Rectangles Profile mode";
				msg += "\r\n\r\n  ***   ! BONUS !   ***";
			    msg += "\r\n  Shift+\"A\"                 Show|Hide All VPOCs on each bar";
//				msg += "\r\n  Video Review	              http://youtu.be/sQHUWKi71Lc";
//				msg += "\r\n  Video Stress-Test         https://youtu.be/wETPwWeU1w0";
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
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public DataSeries VPOC_1
		{
			get { return Values[1]; }
		}
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public DataSeries VPOC_2
		{
			get { return Values[2]; }
		}
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public DataSeries VPOC_3
		{
			get { return Values[3]; }
		}
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public DataSeries VPOC_4
		{
			get { return Values[4]; }
		}
		
		
		//[XmlIgnore()]
		[Description("Сохранять ли профили на диске при перерисовке графика")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("00. Save & Read")]
		public bool SaveRead
		{
			get { return saveread; }
			set { saveread = value;} 	
		}
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
		[Description("Рисовать ли ПОКи на Лонг и Шорт разными цветами")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("07. Use Trend Color")]
		public bool UseTrendColor
		{
			get { return useTrendColor; }
			set { useTrendColor = value; } 	
		}
		//[XmlIgnore()]
		[Description("Расширять ли линии VAH и VAL за границы выделенных диапазонов - то есть продолжать ли их до самого правого края экрана???")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("08. Extend VAH-VAL Lines")]
		public bool ExtVahVal
		{
			get { return extVahVal; }
			set { extVahVal = value; } 	
		}
		//[XmlIgnore()]
		[Description("Степень прозрачности цветной заливки выбранных диапазонов: от 0 до 255. Чем МЕНЬШЕ - тем ПРОЗРАЧНЕЕ!")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("09. Region Fill Transparency")]
		public int RegTransparency
		{
			get { return regTransparency; }
			set { regTransparency = value; } 	
		}
		//[XmlIgnore()]
		[Description("Степень прозрачности цветной заливки гистограммы профиля выбранных диапазонов: от 0 до 255. Чем МЕНЬШЕ - тем ПРОЗРАЧНЕЕ!")]
		[Category("Parameters")]
		[Gui.Design.DisplayNameAttribute("10. Profile Fill Transparency")]
		public int ProfTransparency
		{
			get { return profTransparency; }
			set { profTransparency = value; } 	
		}
		
// COLORS //
		
		//[XmlIgnore()]
        [Description("Цвет текста ПОД диапазонами (ИНФО, если его вообще показывать)")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("01. Summary Color")]
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
		[Gui.Design.DisplayNameAttribute("02. Price Color")]
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
        [Description("Цвет первого диапазона")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("03. Range-1 Color")]
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
		[Gui.Design.DisplayNameAttribute("04. Range-2 Color")]
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
		[Gui.Design.DisplayNameAttribute("05. Range-3 Color")]
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
		[Gui.Design.DisplayNameAttribute("06. Range-4 Color")]
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
        [Description("Цвет линий гистограммы ПРОФИЛЯ вне зоны ValueArea ")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("07. PROFILE Color")]
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
        [Description("Цвет линии ПОКА на ПРОФИЛЕ (не на той части, где идет расширение за правую границу диапазона, а внутри)")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("08. VPOC LINE Color")]
        public Color VpocLineColor
        {
            get { return vpocLineColor; }
            set { vpocLineColor = value; }
        }
		[Browsable(false)]
		public string VpocLineColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(VpocLineColor); }
			set { VpocLineColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет линий гистограммы ПРОФИЛЯ (кроме линии ПОКа) внутри Value Area")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("09. VALUE AREA Color")]
        public Color VpocValueAreaColor
        {
            get { return vpocValueAreaColor; }
            set { vpocValueAreaColor = value; }
        }
		[Browsable(false)]
		public string VpocValueAreaColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(VpocValueAreaColor); }
			set { VpocValueAreaColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет букв справки-подсказки (HELP-SCREEN) и строки рабочих статусов")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("10. HELP-SCREEN Font Color")]
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
		//[XmlIgnore()]
        [Description("Цвет АКТИВИРОВАННОЙ \"кнопки\" на панели управления")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("11. Active Button Color")]
        public Color BtnActiveColor
        {
            get { return btnActiveColor; }
            set { btnActiveColor = value; }
        }
		[Browsable(false)]
		public string BtnActiveColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(BtnActiveColor); }
			set { BtnActiveColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет НЕ АКТИВИРОВАННОЙ, но доступной для нажатия \"кнопки\" на панели управления")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("12. Enabled Button Color")]
        public Color BtnEnableColor
        {
            get { return btnEnableColor; }
            set { btnEnableColor = value; }
        }
		[Browsable(false)]
		public string BtnEnableColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(BtnEnableColor); }
			set { BtnEnableColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		//[XmlIgnore()]
        [Description("Цвет ОТКЛЮЧЕННОЙ, то есть и НЕ активной и НЕ доступной \"кнопки\" на панели управления")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("13. Disabled Button Color")]
        public Color BtnDisableColor
        {
            get { return btnDisableColor; }
            set { btnDisableColor = value; }
        }
		[Browsable(false)]
		public string BtnDisableColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(BtnDisableColor); }
			set { BtnDisableColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}

// LINES //
		
		//[XmlIgnore()]
        [Description("Цвет линии VAH")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("01.1. VAH Line Color")]
        public Color VahLineColor
        {
            get { return vahLineColor; }
            set { vahLineColor = value; }
        }
		[Browsable(false)]
		public string VahLineColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(VahLineColor); }
			set { VahLineColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
        [Description("Тип линии VAH")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("01.2. VAH Line Style")]
        public DashStyle VahLineStyle
        {
            get { return vahLineStyle; }
            set { vahLineStyle = value; }
        }
        [Description("Толщина линии VAH")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("01.3. VAH Line Width")]
        public int VahLineSize
        {
            get { return vahLineSize; }
            set { vahLineSize = value; }
        }
		//[XmlIgnore()]
        [Description("Цвет линии VAL")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("02.1. VAL Line Color")]
        public Color ValLineColor
        {
            get { return valLineColor; }
            set { valLineColor = value; }
        }
		[Browsable(false)]
		public string ValLineColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(ValLineColor); }
			set { ValLineColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
        [Description("Тип линии VAL")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("02.2. VAL Line Style")]
        public DashStyle ValLineStyle
        {
            get { return valLineStyle; }
            set { valLineStyle = value; }
        }
        [Description("Толщина линии VAL")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("02.3. VAL Line Width")]
        public int ValLineSize
        {
            get { return valLineSize; }
            set { valLineSize = value; }
        }
		//[XmlIgnore()]
        [Description("Цвет линии РАМКИ")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("03.1. Border Color")]
        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; }
        }
		[Browsable(false)]
		public string BorderColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(BorderColor); }
			set { BorderColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
        [Description("Тип линии РАМКИ")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("03.2. Border Style")]
        public DashStyle BorderLineStyle
        {
            get { return borderLineStyle; }
            set { borderLineStyle = value; }
        }
        [Description("Толщина линии РАМКИ")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("03.3. Border Width")]
        public int BorderLineSize
        {
            get { return borderLineSize; }
            set { borderLineSize = value; }
        }
		//[XmlIgnore()]
        [Description("Цвет линии EXT-VPOC для ЛОНГа")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("04.1. EXT-VPOC Long Color")]
        public Color VpocExtLongColor
        {
            get { return vpocExtLongColor; }
            set { vpocExtLongColor = value; }
        }
		[Browsable(false)]
		public string VpocExtLongColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(VpocExtLongColor); }
			set { VpocExtLongColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
        [Description("Тип линии EXT-VPOC для ЛОНГа")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("04.2. EXT-VPOC Long Style")]
        public DashStyle VpocExtLongStyle
        {
            get { return vpocExtLongStyle; }
            set { vpocExtLongStyle = value; }
        }
        [Description("Толщина линии EXT-VPOC для ЛОНГа")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("04.3. EXT-VPOC Long Width")]
        public int VpocExtLongSize
        {
            get { return vpocExtLongSize; }
            set { vpocExtLongSize = value; }
        }
		//[XmlIgnore()]
        [Description("Цвет линии EXT-VPOC для ШОРТа")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("05.1. EXT-VPOC Short Color")]
        public Color VpocExtShortColor
        {
            get { return vpocExtShortColor; }
            set { vpocExtShortColor = value; }
        }
		[Browsable(false)]
		public string VpocExtShortColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(VpocExtShortColor); }
			set { VpocExtShortColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
        [Description("Тип линии EXT-VPOC для ШОРТа")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("05.2. EXT-VPOC Short Style")]
        public DashStyle VpocExtShortStyle
        {
            get { return vpocExtShortStyle; }
            set { vpocExtShortStyle = value; }
        }
        [Description("Толщина линии EXT-VPOC для ШОРТа")]
        [Category("Lines")]
		[Gui.Design.DisplayNameAttribute("05.3. EXT-VPOC Short Width")]
        public int VpocExtShortSize
        {
            get { return vpocExtShortSize; }
            set { vpocExtShortSize = value; }
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
        private _Savos_Range_Profile_v003[] cache_Savos_Range_Profile_v003 = null;

        private static _Savos_Range_Profile_v003 check_Savos_Range_Profile_v003 = new _Savos_Range_Profile_v003();

        /// <summary>
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        public _Savos_Range_Profile_v003 _Savos_Range_Profile_v003(int extProfile, bool extVahVal, int initWidth, int priceFontSize, int profTransparency, int regTransparency, bool saveRead, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            return _Savos_Range_Profile_v003(Input, extProfile, extVahVal, initWidth, priceFontSize, profTransparency, regTransparency, saveRead, showPrice, showSummary, summaryFontSize, useTrendColor);
        }

        /// <summary>
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        public _Savos_Range_Profile_v003 _Savos_Range_Profile_v003(Data.IDataSeries input, int extProfile, bool extVahVal, int initWidth, int priceFontSize, int profTransparency, int regTransparency, bool saveRead, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            if (cache_Savos_Range_Profile_v003 != null)
                for (int idx = 0; idx < cache_Savos_Range_Profile_v003.Length; idx++)
                    if (cache_Savos_Range_Profile_v003[idx].ExtProfile == extProfile && cache_Savos_Range_Profile_v003[idx].ExtVahVal == extVahVal && cache_Savos_Range_Profile_v003[idx].InitWidth == initWidth && cache_Savos_Range_Profile_v003[idx].PriceFontSize == priceFontSize && cache_Savos_Range_Profile_v003[idx].ProfTransparency == profTransparency && cache_Savos_Range_Profile_v003[idx].RegTransparency == regTransparency && cache_Savos_Range_Profile_v003[idx].SaveRead == saveRead && cache_Savos_Range_Profile_v003[idx].ShowPrice == showPrice && cache_Savos_Range_Profile_v003[idx].ShowSummary == showSummary && cache_Savos_Range_Profile_v003[idx].SummaryFontSize == summaryFontSize && cache_Savos_Range_Profile_v003[idx].UseTrendColor == useTrendColor && cache_Savos_Range_Profile_v003[idx].EqualsInput(input))
                        return cache_Savos_Range_Profile_v003[idx];

            lock (check_Savos_Range_Profile_v003)
            {
                check_Savos_Range_Profile_v003.ExtProfile = extProfile;
                extProfile = check_Savos_Range_Profile_v003.ExtProfile;
                check_Savos_Range_Profile_v003.ExtVahVal = extVahVal;
                extVahVal = check_Savos_Range_Profile_v003.ExtVahVal;
                check_Savos_Range_Profile_v003.InitWidth = initWidth;
                initWidth = check_Savos_Range_Profile_v003.InitWidth;
                check_Savos_Range_Profile_v003.PriceFontSize = priceFontSize;
                priceFontSize = check_Savos_Range_Profile_v003.PriceFontSize;
                check_Savos_Range_Profile_v003.ProfTransparency = profTransparency;
                profTransparency = check_Savos_Range_Profile_v003.ProfTransparency;
                check_Savos_Range_Profile_v003.RegTransparency = regTransparency;
                regTransparency = check_Savos_Range_Profile_v003.RegTransparency;
                check_Savos_Range_Profile_v003.SaveRead = saveRead;
                saveRead = check_Savos_Range_Profile_v003.SaveRead;
                check_Savos_Range_Profile_v003.ShowPrice = showPrice;
                showPrice = check_Savos_Range_Profile_v003.ShowPrice;
                check_Savos_Range_Profile_v003.ShowSummary = showSummary;
                showSummary = check_Savos_Range_Profile_v003.ShowSummary;
                check_Savos_Range_Profile_v003.SummaryFontSize = summaryFontSize;
                summaryFontSize = check_Savos_Range_Profile_v003.SummaryFontSize;
                check_Savos_Range_Profile_v003.UseTrendColor = useTrendColor;
                useTrendColor = check_Savos_Range_Profile_v003.UseTrendColor;

                if (cache_Savos_Range_Profile_v003 != null)
                    for (int idx = 0; idx < cache_Savos_Range_Profile_v003.Length; idx++)
                        if (cache_Savos_Range_Profile_v003[idx].ExtProfile == extProfile && cache_Savos_Range_Profile_v003[idx].ExtVahVal == extVahVal && cache_Savos_Range_Profile_v003[idx].InitWidth == initWidth && cache_Savos_Range_Profile_v003[idx].PriceFontSize == priceFontSize && cache_Savos_Range_Profile_v003[idx].ProfTransparency == profTransparency && cache_Savos_Range_Profile_v003[idx].RegTransparency == regTransparency && cache_Savos_Range_Profile_v003[idx].SaveRead == saveRead && cache_Savos_Range_Profile_v003[idx].ShowPrice == showPrice && cache_Savos_Range_Profile_v003[idx].ShowSummary == showSummary && cache_Savos_Range_Profile_v003[idx].SummaryFontSize == summaryFontSize && cache_Savos_Range_Profile_v003[idx].UseTrendColor == useTrendColor && cache_Savos_Range_Profile_v003[idx].EqualsInput(input))
                            return cache_Savos_Range_Profile_v003[idx];

                _Savos_Range_Profile_v003 indicator = new _Savos_Range_Profile_v003();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.ExtProfile = extProfile;
                indicator.ExtVahVal = extVahVal;
                indicator.InitWidth = initWidth;
                indicator.PriceFontSize = priceFontSize;
                indicator.ProfTransparency = profTransparency;
                indicator.RegTransparency = regTransparency;
                indicator.SaveRead = saveRead;
                indicator.ShowPrice = showPrice;
                indicator.ShowSummary = showSummary;
                indicator.SummaryFontSize = summaryFontSize;
                indicator.UseTrendColor = useTrendColor;
                Indicators.Add(indicator);
                indicator.SetUp();

                _Savos_Range_Profile_v003[] tmp = new _Savos_Range_Profile_v003[cache_Savos_Range_Profile_v003 == null ? 1 : cache_Savos_Range_Profile_v003.Length + 1];
                if (cache_Savos_Range_Profile_v003 != null)
                    cache_Savos_Range_Profile_v003.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cache_Savos_Range_Profile_v003 = tmp;
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
        public Indicator._Savos_Range_Profile_v003 _Savos_Range_Profile_v003(int extProfile, bool extVahVal, int initWidth, int priceFontSize, int profTransparency, int regTransparency, bool saveRead, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            return _indicator._Savos_Range_Profile_v003(Input, extProfile, extVahVal, initWidth, priceFontSize, profTransparency, regTransparency, saveRead, showPrice, showSummary, summaryFontSize, useTrendColor);
        }

        /// <summary>
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        public Indicator._Savos_Range_Profile_v003 _Savos_Range_Profile_v003(Data.IDataSeries input, int extProfile, bool extVahVal, int initWidth, int priceFontSize, int profTransparency, int regTransparency, bool saveRead, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            return _indicator._Savos_Range_Profile_v003(input, extProfile, extVahVal, initWidth, priceFontSize, profTransparency, regTransparency, saveRead, showPrice, showSummary, summaryFontSize, useTrendColor);
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
        public Indicator._Savos_Range_Profile_v003 _Savos_Range_Profile_v003(int extProfile, bool extVahVal, int initWidth, int priceFontSize, int profTransparency, int regTransparency, bool saveRead, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            return _indicator._Savos_Range_Profile_v003(Input, extProfile, extVahVal, initWidth, priceFontSize, profTransparency, regTransparency, saveRead, showPrice, showSummary, summaryFontSize, useTrendColor);
        }

        /// <summary>
        /// Индикатор ПРОФИЛЕЙ и ПОКов для диапазонов (до ЧЕТЫРЕХ!), которые можно двигать по графику, менять размеры и удалять в произвольном порядке... и ничего лишнего!
        /// </summary>
        /// <returns></returns>
        public Indicator._Savos_Range_Profile_v003 _Savos_Range_Profile_v003(Data.IDataSeries input, int extProfile, bool extVahVal, int initWidth, int priceFontSize, int profTransparency, int regTransparency, bool saveRead, bool showPrice, bool showSummary, int summaryFontSize, bool useTrendColor)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator._Savos_Range_Profile_v003(input, extProfile, extVahVal, initWidth, priceFontSize, profTransparency, regTransparency, saveRead, showPrice, showSummary, summaryFontSize, useTrendColor);
        }
    }
}
#endregion
