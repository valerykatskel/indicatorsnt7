#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
//using System.Design;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Design;
using System.Xml;
using System.Net;
using System.Net.Cache;
using System.IO;
using System.Text;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Text.RegularExpressions;
#endregion
/// <summary>
/// ЭТО НЕ НАПОМИНАЛКА!!! ТУТ НЕЛЬЗЯ УКАЗАТЬ ВРЕМЯ УВЕДОМЛЕНИЙ!!! 
/// Для таких вещей используйте индикатор "SavosEventsReminder"!
/// А ЭТОТ ИНДИКАТОР ПОЛУЧАЕТ ДАННЫЕ ИЗ ФИНАНСОВЫХ ВЕБ-КАЛЕНДАРЕЙ
/// И он на графике покажет ожидаемую новость ДО ее появления - текстом, цветовым выделением части графика, звуком!!!
/// Распространяется как есть - но просьба не удалять эту информацию:
/// Copyright by SavosRU aka "дядя Савос" 2016. All questions - to savos@bk.ru
/// </summary>

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
	/// <summary>
	/// Simple News or any other events reminder.
	/// </summary>
	[Description("Simple News or any other events remainder.")]
	public class _Savos_News_For_Strategy : Indicator
	{
		// Несколько дополнительных классов
		#region NewsEventClass
		/// <summary>
		/// Дополнительный внутренний класс-структура для хранения и обработки событий и/или новостей.
		/// </summary>
		public class NewsEvent		// если мы хотим еще сортировать этот список - то вот эти 
									// : IEquatable<NewsEvent> , IComparable<NewsEvent> - просто необходимы... И еще надо будет реализовать методы сравнения, наверное
		{
			// На самом деле мне нужен простой список из ВРЕМЁН - полученный при инициализации из настроек индикатора
			// И куча дополнительных полей тут ни к чему... Это из другого индикатора сюда былл скопировано для "разобраться"!!!
			public int ID;
			public string Title;
			public string Country;
			public string Flag;
			public string Date;
			public string Time;
			public string Priority;
			//public string Impact;
			//public string Forecast;
			//public string Previous;
			public DateTime DateTimeLocal;
			public override string ToString(){
				return string.Format("ID: {0}, DateTimeLocal: {1}, Country: {2}, Priority: {3}, Title: {4}",
					ID, DateTimeLocal, Country, Priority, Title);			
			}
			public string ToStatus(){
				return string.Format("{0} | {1} | {2} | {3}",
					DateTimeLocal.ToString("HH:mm"), Flag, Priority, Title);			
			}

		}
		#endregion NewsEventClass
					
		#region TextColumnClass
		/// <summary>
		/// Дополнительный класс - думаю понадобится и в полноценном индикаторе, тут же нужен для временного списка новостей
		/// </summary>
		
		private class TextColumn {
			public TextColumn(float padding, string text){
				this.padding = padding;
				this.text = text;
			}
			public float padding;
			public string text;
			public float GetLength(Graphics graphics, Font font){
				return graphics.MeasureString(text, font).Width + padding;
			}
		}
		
		#endregion TextColumnClass
		
		#region TextLineClass
		/// <summary>
		/// Дополнительный класс - думаю понадобится и в полноценном индикаторе, тут же нужен для временного списка новостей
		/// </summary>
		
		private class TextLine {
			public TextLine(Font font, Brush brush){
				this.graphics = graphics;
				this.font = font;
				this.brush = brush;
			}
			public TextColumn timeColumn;
			public TextColumn impactColumn;
			public TextColumn descColumn;
			public Font font;
			public Brush brush;
			private Graphics graphics;
		}
		
		#endregion TextLineClass

		// Набор переменных
		#region Variables
		// property variables
		private bool debug = false;				// Если изменить значение на TRUE - будет выводиться масса отладочной
												// информации в окно "OUTPUT"
		
		private NewsEvent[] newsEvents = null;	// Список из ожидаемых событий
		private int maxNewsItems = 10;			// Всего мы можем задать до 10 новостей-событий
		private int newsItemPtr = 0;			// Номер следующей новости в списке новостей
		private int lastItemPtr = 0;			// Номер предыдущей новости в списке новостей

		private DateTime NOW				= DateTime.MinValue;// Переменная, для получения текущего времени ВНЕ ЗАВИСИМОСТИ
																// от того, реальное ли подключение используется на графике 
																// или же МаркетРеплей (Market Replay)
		
		private bool 	FirstLoadNews		= false;			// Нам нужно для начала работы ОБЯЗАТЕЛЬНО хотя бы раз загрузить новости
																// Тогда мы эту переменную переводим в TRUE и дальше новости грузим ТОЛЬКО по таймеру
																// в полноценном новостном индикаторе, конечно... А в напоминалке после первого раза
																// больше не грузим вообще!!!
		
		private bool	showEC     			= true;				// Показывать событие на графике???
		private int		minutesBefore		= 3;				// За сколько минут ДО события начинать рисовать колонну на графике
		private int		minutesAfter		= 3;				// Сколько минут ПОСЛЕ события еще рисовать колонну на графике
		private Color	elColor    			= Color.Gray;		// Цвет всей "событийной" колонны за период ДО и ПОСЛЕ события
		private Color	ecColor    			= Color.Yellow;		// Цвет именно той линии, которая обозначит событие внутри колонны

		private bool	sendAlerts			= true;				// Посылать ли алерт и звуковой сигнал???
		private int		alertInterval		= 3;				// За сколько минут ДО события это делать
		private string	alertWavFileName	= "Alert1.wav";		// Звук для алерта
		private Priority alertPriority		= Priority.High;	// Приоритет для алерта

		private bool	showPopUp			= true;				// Показывать ли всплывающее окно с сообщением???
		private int		popupInterval		= 3;				// За сколько минут ДО события это делать
		private string	msgTextLine1		= "_*_    НОВОСТИ    _*_";	// Первая строка всплывающего сообщения
		private string	msgTextLine2		= "!Снимайте все ордера!";	// Вторая строка всплывающего сообщения
		private bool	showNewsTitle		= false;			// Показывать ли название новости во всплывающем окне? 
																// По умолчанию не вижу надобности. Но если кому-то надо - можно включить в настройках
		
		private bool	showStat			= true;									// Показывать ли статус???
		private string	statusTextLine1 	= "Сегодня больше нет новостей...";		// Первая строка сообщения ПРИ отсутствии новостей
		//private string	statusTextLine2 	= "...нет в календаре на Cl-Trade.Net";	// Вторая строка сообщения ПРИ отсутствии новостей
		private string	statusTextLine2 	= "...нет в календаре на Invest.Com";	// Вторая строка сообщения ПРИ отсутствии новостей
		//private Font	stFont				= new Font("Arial", 10);				// Шрифт статусных сообщений
		private Font	stFont				= new Font("Courier New", 8);			// Шрифт статусных сообщений
		
		private Color	stColor    			= Color.Green;							// Цвет статусных сообщений
		private bool	showStatNext		= true;									// Показывать ли в статусе ТРИ следующие новости???
		
		private int		newsRefeshInterval	= 60;				// Интервал обновления списка новостей - на самом деле нужен один раз в день - ну календарь ведь часто не обновляется??? Ну пусть будет раз в час...
																// новостного индикатора и тут использован просто для единообразия
		private string	lastLoadError		= "";				// Последнее сообщение об ошибке загрузки веб-календаря
		private DateTime lastNewsUpdate		= DateTime.MinValue;// Когда последний раз обновляли данные о новостях
		private DateTime lastMinute			= DateTime.MinValue;// Посдедняя минута - нужен для учета времени на графиках с вне-временной
																// структурой - тиковых, рейнджбарах и так далее... и на графиках, где продолжительность
																// времени формирования свечки больше интервала для рефреша новостей
		private bool	todaysNewsOnly		= true;				// Показывать только сегодняшние новости - конечно, а зачем нам другие???

		private	System.Collections.Generic.List<TextLine> listLine;
		private System.Collections.ArrayList list = new System.Collections.ArrayList(); // Готовим хранилище для списка новостей
		
		// URL'ы для получения календарей от ECONODAY.COM - если я решу делать что-то с ним
		private string NewsUrlEconoDay1 = "http://mam.econoday.com/byday.asp?day=21&month=8&year=2016&cust=mam&lid=0";							// Только США - дневной календарь
		private string NewsUrlEconoDay2 = "http://global-premium.econoday.com/byday.asp?day=18&month=8&year=2016&cust=global-premium&lid=0";	// Все страны - дневной календарь
		private string NewsUrlEconoWeek1 = "http://mam.econoday.com/byweek.asp?day=21&month=8&year=2016&cust=mam&lid=0";						// Только США - недельный календарь
		private string NewsUrlEconoWeek2 = "http://global-premium.econoday.com/byweek.asp?day=21&month=8&year=2016&cust=global-premium&lid=0";	// Все страны - недельный календарь
		// Можно даже получить легкий и простой RSS-вариант после регистрации - в урле нужен e-mail и айдишник, индивидуальный для каждого
		private string NewsUrlEconoRSS = "my.econoday.com/getrss.asp?UID=savos@bk.ru&rid=27569";
		
		// URL для получения календаря ЯКОБЫ от Investing.COM на самом деле выглядит вот так:
		private const string ffNewsUrl = @"http://ec.ru.forexprostools.com?columns=exc_flags,exc_currency,exc_importance,exc_actual,exc_forecast,exc_previous&importance=2,3&features=datepicker,timezone,timeselector,filters&countries=4,72,5&calType=day&timeZone=18&lang=7";
		// На самом деле из всего этого нам РЕАЛЬНО НАДО вот так
		private string NewsUrl = @"http://ec.ru.forexprostools.com/?columns=exc_flags,exc_currency,exc_importance,&importance=1,2,3&countries=4,72,5&calType=day&timeZone=18&lang=7";
		private string NewsUrlCustom = @"http://ec.ru.forexprostools.com/?columns=exc_flags,exc_currency,exc_importance,&importance=@@@IMPORTANCE@@@&countries=@@@COUNTRIES@@@&calType=week&timeZone=18&lang=7";
		
		// Часовые пояса - не знаю как они у них там считаются, но МОСКВА - 18-я зона
		private int timeZone=3; // ну вот так у них Москва задана 18... Мы УРЛ менять не будем - будем менять результат!
		
		// Важность получаемых новостей - раскомментировать ТОЛЬКО одну строку из трех - но вообще меняется из настроек индикатора
		private string strImportance	= "1,2,3";	// если нужны все новости
		//private string strImportance	= "2,3";	// если нужны новости средней и высокой важности
		//private string strImportance	= "3";		// если нужны ТОЛЬКО новости высокой важности

		// Новости по каким странам(зонам) показывать - раскомментировать ТОЛЬКО одну строку из трех - но вообще меняется из настроек индикатора
		//private string strCountries		= "5";					// если нужны новости ТОЛЬКО по США
		//private string strCountries		= "4";					// если нужны новости ТОЛЬКО по Великобритании
		//private string strCountries		= "72";					// если нужны новости ТОЛЬКО по ЕвроЗоне
		//private string strCountries		= "4,72,5";				// если нужны новости по таким странам(зонам) как США, Еврозона и Великобритания
		private string strCountries		= "4,5,17,72";			// если нужны новости по таким странам(зонам) как США, Еврозона, Великобритания и Германия
		//private string strCountries		= "10,17,22,26";		// если нужны новости по таким странам(зонам) Германия, Италия, Испания, Франция
		//private string strCountries		= "4,5,10,17,22,26,72";	// если нужны новости по всем вышеперечисленным, то есть по таким странам(зонам)
																// как США, Еврозона, Великобритания, Германия, Италия, Испания, Франция
		// А вообще вот такие коды можно использовать:		
		// 1-2-3 - показывает ВСЕ страны... 4 - Великобритания, 5 - США, 6 - Канада, 7 - Мексика, 8 - опять ВСЕ , 9 - Швеция, 10 - Италия
		// 11 - Южная Корея, 12 - Швейцария, 13 - вроде снова ВСЕ, 14 - Индия, 15 - Коста-Рика, 16 - все, 17 - Германия, 18 - все, 19 - все, 20 - Нигерия
		// 21 - Нидерланды, 22 - Франция, 23 - Израиль, 24 - Дания, 25 - Австралия, 26 - Испания, 27 - Чили, 28 - все, 29 - Аргентина, 30 - все
		// 31 - , 32 - Бразилия, 33 - Ирландия, 34 - Бельгия, 35 - Япония, 36 - Сингапур, 37 - Китай, 38 - Португалия, 39 - Гонконг, 40 - все
		// 41 - Таиланд, 42 - Малайзия, 43 - Новая Зеландия, 44 - Пакистан, 45 - Филиппины, 46 - Тайвань, 47 - все, 48 - Индонезия, 49 - все, 50 - все
		// 51 - Греция, 52 - Суадовская Аравия, 53 - Польша, 54 - Австрия, 55 - Чехия, 56 - Россия, 57 - Кения, 58 - все, 59 - Египет, 60 - Норвегия
		// 61 - Украина, 62 - все, 63 - Турция, 64 - все, 65 - все, 66 - все, 67 - все, 68 - Ливан, 69 - все, 70 - все
		// 71 - Финляндия, 72 - ЕвроЗона, 73 - все, 74 - все, 75 - Зимбабве, 76 - все, 77 - все, 78 - все, 79 - все, 80 - Руанда 
		
		// То есть комбинированный URL в котором будут учтены выбранные страны и важность
		//private string NewsUrl = @"http://ec.ru.forexprostools.com/?columns=exc_currency,exc_importance,&importance="+strImportance+"&countries="+strCountries+"&calType=day&timeZone=18&lang=7";
		private bool showUS = true;		// показывать ли новости по США
		private bool showEU = true;		// показывать ли новости по ЕвроЗоне
		private bool showGB = true;		// показывать ли новости по ВеликоБритании
		private bool showDE = true;		// показывать ли новости по Германии
		private bool showIT = true;		// показывать ли новости по Италии
		private bool showES = true;		// показывать ли новости по Испании
		private bool showFR = true;		// показывать ли новости по Франции
		private bool showLow = false;	// показывать ли новости с низкой важностью
		private bool showMed = true;	// показывать ли новости со средней важностью
		private bool showHigh = true;	// показывать ли новости с высокой важностью
		
		private	string tmpStat1 = "";
		private string tmpStat2 = "";
		
		private bool isMarketReplay	= false;	// Изначально предполагаем, что мы НЕ в Маркет-Реплэе
		#endregion

		// Инициализация индикатора
		#region INIT
		/// <summary>
		/// This method is used to configure the indicator and is called once before any bar data is loaded.
		/// </summary>
		protected override void Initialize()
		{
			ClearOutputWindow();
			//Add(new Plot(Color.Orange, "_Savos_News_For_Strategy"));
			Add(new Plot(Color.Green, PlotStyle.TriangleUp, "CanTrade"));
			//Plots[0].Pen.Width = 2;

			Overlay				= true;
			//PriceTypeSupported	= true;
			CalculateOnBarClose = false;
			BarsRequired = 200;
			tmpStat1 = statusTextLine1;
			tmpStat2 = statusTextLine2;
			
		}
		#endregion INIT
		
		// Парсинг WEB-календаря новостей
		// И загрузка полученных новостей во внутреннюю структуру
		#region LOADNEWS

		/// <summary>
		/// Получаем данные из внешнего источника и отрезаем нафиг все лишнее
		/// </summary>
		private void LoadNews(){
			int itemId = 0; // счетчик
			//System.Collections.ArrayList list = new System.Collections.ArrayList(); // Готовим хранилище для списка новостей
			
			// Теперь если мы в МаркетРеплэе - данные берем из одного места, а на реале - из другого
			string urltweak = "";
			if(isMarketReplay) {
				// будем по дате вычислять нужный нам файл с диска С				
				Print("We now in the Market Replay mode!!!");
				urltweak = "http://savos.ru/calendar/2016_" + NOW.ToString("MM")+".html";
			} else {
				// Подставляем в URL нужные нам страны и приоритеты новостей
				NewsUrl = NewsUrlCustom.Replace("@@@COUNTRIES@@@",strCountries);
				NewsUrl = NewsUrl.Replace("@@@IMPORTANCE@@@",strImportance);
				//NewsUrl = NewsUrl.Replace("@@@TIMEZONE@@@",timeZone.ToString());
				
				// чтобы избежать кеширования данных на стороне веб-сервера - добавим в УРЛ "левую" переменную, которая будет меняться при каждом вызове
				urltweak = NewsUrl + "?x=" + Convert.ToString(NOW.Ticks);
			}
			if (debug) Print("Loading news from URL: " + urltweak);
			Print("Loading news from URL: " + urltweak);
			
			HttpWebRequest newsReq = (HttpWebRequest)HttpWebRequest.Create(urltweak);
			newsReq.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Reload);
			
			try {
				using (HttpWebResponse newsResp = (HttpWebResponse)newsReq.GetResponse()){
					// Проверяем, что получили какой-то внятный ответ от сервера
					if (newsResp != null && newsResp.StatusCode == HttpStatusCode.OK){
						// И в удачном варианте - загружаем теперь поток данных от сервера в переменную для дальнейшей обработки
						Stream receiveStream = newsResp.GetResponseStream();
						Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
						StreamReader readStream = new StreamReader( receiveStream, encode );
						string htmlString = readStream.ReadToEnd();
						if (debug) Print("RAW http response: " + htmlString);
						// обработка
						
						// Попробуем через REGEX выбрать все даты событий...
						Regex myRegTS = new Regex("event_timestamp=\"([-\\d]*)\\s([:\\d]*)");				// таймштамп - ТОЛЬКО ДАТЫ отдельно и ВРЕМЯ отдельно!!!
						//Regex myRegZN = new Regex("flagCur\"><span title=\"([а-яА-Яa-zA-Z]*)\" class=\"[a-zA-Z\\s\\d_]*\">&nbsp;</span>\\s*([A-Z]{3})\\s*</td>");				// страна (скорее зона) действия новости
						Regex myRegZN = new Regex("flagCur[ noWrap]*\"><span title=\"([\\sа-яА-Яa-zA-Z]*)\"\\s*class=\"[a-zA-Z\\s\\d_]*\">&nbsp;</span>\\s*([A-Z]{3})");		// страна (скорее зона) действия новости
						//Regex myRegPR = new Regex("sentiment\" title=\"([\\S]*)");				// важность новости - три варианта возможны: "Низкая", "Умеренная", "Высокая"
						Regex myRegPR = new Regex("sentiment[ noWrap]*\" title=\"([\\S]*)");				// важность новости - три варианта возможны: "Низкая", "Умеренная", "Высокая"
						//Regex myRegNM = new Regex("left event\">([\\S ]*)</");					// название новости НО!!! тут могут быть
						Regex myRegNM = new Regex("<td class=\"left event\">\\s*([ a-zA-Zа-яА-ЯёЁ\\(\\)\\/]*)\\s*");					// название новости НО!!! тут могут быть
											// подводные камни: значки "Речь" "Отчет" и так далее - надо эти поля резать по признаку
											// "до слова СПАН" то есть до "&nbsp;<span"
						
						MatchCollection matchesTS = myRegTS.Matches(htmlString);
						//MatchCollection matchesLT = myRegLT.Matches(htmlString);
						MatchCollection matchesZN = myRegZN.Matches(htmlString);
						MatchCollection matchesPR = myRegPR.Matches(htmlString);
						MatchCollection matchesNM = myRegNM.Matches(htmlString);
						
						
						Print("TS count = " + matchesTS.Count);
						//Print("LT count = " + matchesLT.Count);
						Print("ZN count = " + matchesZN.Count);
						Print("PR count = " + matchesPR.Count);
						Print("NM count = " + matchesNM.Count);
						/*
						foreach (Match m in matchesTS) {
							string result = m.Groups[1].Value;
							string result2 = m.Groups[2].Value;
							Print(result + " " + result2);
						}						
						Print ("Time " + matchesTS[1].Groups[1].Value);
						*/
						
						// У нас должны получиться ОДИНАКОВЫЕ по количеству коллекции данных!!!
						// То есть, сколько ВРЕМЕН, столько и НАЗВАНИЙ, ЗОН(СТРАН), ВАЖНОСТЕЙ и так далее!!!
						// ЕСЛИ это не так - ОШИБКА!!!
						//if((matchesTS.Count == matchesLT.Count) && (matchesTS.Count == matchesZN.Count) && (matchesTS.Count  == matchesPR.Count) && (matchesTS.Count  == matchesNM.Count)) {
						if((matchesTS.Count ==  matchesZN.Count) && (matchesTS.Count  == matchesPR.Count) && (matchesTS.Count  == matchesNM.Count)) {
							// все в порядке!!! - Можем предыдущий ЛИСТ очистить и начать загружать данные заново
							list.Clear();
							// восстанавливаем значения статусных строк по умолчанию, ведь в случае ошибки они были переписаны!!!
							statusTextLine1 = tmpStat1;
							statusTextLine2 = tmpStat2;

							for(int x = 1; x < matchesTS.Count ; x++){
								// Создаем в цикле обьекты СОБЫТИЕ(НОВОСТЬ)
								NewsEvent newsEvent = new NewsEvent();								
								newsEvent.Date = matchesTS[x].Groups[1].Value.Trim();
								newsEvent.Time = matchesTS[x].Groups[2].Value.Trim();
								if(debug) Print("Parsed DateTime STRING is " + newsEvent.Date + " " + newsEvent.Time);
								newsEvent.DateTimeLocal = DateTime.ParseExact(newsEvent.Date + " " + newsEvent.Time, "yyyy-MM-dd HH:mm:ss",null);
								newsEvent.DateTimeLocal = newsEvent.DateTimeLocal.AddHours(timeZone);	// ВОТ ТУТ мы с тайм-зонами и работаем!!!
								if(debug) Print("Parsed DateTime DATE-TIME is " + newsEvent.DateTimeLocal.ToString());
								
								// сразу на ходу будем отфильтровывать лишнее...
								DateTime startTime = NOW;
								DateTime endTime = startTime.AddDays(1);															
								// фильтруем по нескольким разным свойствам...
								if (newsEvent.DateTimeLocal >= startTime && (!todaysNewsOnly || newsEvent.DateTimeLocal.Date < endTime.Date))
								{
									newsEvent.ID = ++itemId;
									newsEvent.Country = matchesZN[x].Groups[1].Value.Trim();
									newsEvent.Flag = matchesZN[x].Groups[2].Value.Trim();
									if (showDE && newsEvent.Country.ToUpper() == "ГЕРМАНИЯ") newsEvent.Flag = " DE";
									
									// Если у нас новость по одной из этих ЧЕТЫРЕХ зон, но ее отображение в настройках выключено
									// то выкидываем такую новость нафиг!
									if (!showUS && newsEvent.Country.ToUpper() == "США") continue;
									if (!showEU && newsEvent.Country.ToUpper() == "ЕВРОЗОНА") continue;
									if (!showGB && newsEvent.Country.ToUpper() == "ВЕЛИКОБРИТАНИЯ") continue;
									if (!showDE && newsEvent.Country.ToUpper() == "ГЕРМАНИЯ") continue;
									// Но если у нас новость ВООБЩЕ не из этих зон - в данной версии индикатора
									// мы такие новости ТОЖЕ выкидываем нафиг!!!
									if ((newsEvent.Country.ToUpper() != "США") && (newsEvent.Country.ToUpper() != "ЕВРОЗОНА") && (newsEvent.Country.ToUpper() != "ВЕЛИКОБРИТАНИЯ") && (newsEvent.Country.ToUpper() != "ГЕРМАНИЯ")) continue;
									
									newsEvent.Priority = matchesPR[x].Groups[1].Value.Trim();
									if (newsEvent.Priority.ToUpper() == "НИЗКАЯ")		newsEvent.Priority = "  *";
									if (newsEvent.Priority.ToUpper() == "УМЕРЕННАЯ")	newsEvent.Priority = " **";
									if (newsEvent.Priority.ToUpper() == "ВЫСОКАЯ")		newsEvent.Priority = "***";
									
									if (!showLow && newsEvent.Priority	== "  *") continue;
									if (!showMed && newsEvent.Priority	== " **") continue;
									if (!showHigh && newsEvent.Priority == "***") continue;
																											
									newsEvent.Title = matchesNM[x].Groups[1].Value.Trim();
									// вот тут могут быть засады и надо резать лишние СПАНы с картинками "речь" и так далее
									// пробуем отрезать лишнее
									string subString = "&nbsp;<span";	// находим (если есть) начало этого фрагмента
									int indexOfSubstring = newsEvent.Title.IndexOf(subString); // Найдем?
									// отрезаем (удаляем) начало до найденного номера							
									if(indexOfSubstring > 0) newsEvent.Title = newsEvent.Title.Substring(0,indexOfSubstring).Trim();
								
									// И раз уж мы добрались сюда - то все ок и добавляем событие в наш лист
									list.Add(newsEvent);
									if (debug) Print("Added: " + newsEvent.ToString());
								}								
							}
						} else {
							Print("ERROR of REGEX Matches!");
							// Меняем значения строк статуса на сообщение об ошибке
							statusTextLine1 = "Новостей нет... Но это, кажется, какая-то ошибка!";
							statusTextLine2 = "Ошибка в распознавании REGEX Matches!";
						}												
						// Загружаем список в массив
						newsEvents = (NewsEvent[])list.ToArray(typeof(NewsEvent));
						if (debug) Print("Added a total of " + list.Count  + " events to array.");
						// И не забываем уточнить новое время ПОСЛЕДНЕЙ ЗАГРУЗКИ НОВОСТЕЙ
						lastNewsUpdate = NOW;
						if (debug) Print("Last News Reloaded -> " + NOW);
					} else {
						// ловим нестандартные ситуации...
						if (newsResp == null) throw new Exception("Web response was null.");
						else throw new Exception("Web response status code = " + newsResp.StatusCode.ToString());
					}
				}

			} catch (Exception ex){
				Print(ex.ToString());
				lastLoadError = ex.Message;
			}
		}
		#endregion LOADNEWS
		
		// Основная работа внутри бара
		#region ONBARUPDATE
		/// <summary>
		/// Called on each bar update event (incoming tick)
		/// </summary>
		protected override void OnBarUpdate()			
		{
			
			newsItemPtr = -1;	// это мы пока сбрасываем указатель в списке новостей - пока не найдем правильный номер СЛЕДУЮЩЕЙ новости или НЕ НАЙЕМ ничего
			lastItemPtr = -1;	// также сбрасываем и указатель ПРЕДЫДУЩЕЙ новости
			int newsCounter = 0; // Счетчик еще не обработанных новостей
			int prevCounter = 0; // Счетчик новостей, уже обработанных, но еще находящихся в нашем списке
			NewsEvent e = null; // создаем переменную для возможной дальнейшей работы с событиями
			TimeSpan diff = TimeSpan.FromMinutes(0) ; // аналогично - переменная для работы с временными интервалами

			if(Bars.Count > BarsRequired) {
				// Начнем с получения текущего времени, ведь, ОКАЗЫВАЕТСЯ при МаркетРеплее оно не работает обычным образом
				
				// Автоматическое определение Маркет-Реплей-Мод, как оказалось, работает не всегда корректно - будем думать...
				/*
				if(Bars.MarketData != null) {
					// Это фактически обозначает, что мы не на живом подключении - то есть мы в МаркетРеплее сейчас
					NOW = (Bars.MarketData.Connection.Options.Provider == Cbi.Provider.Replay ? Bars.MarketData.Connection.Now : DateTime.Now);  // ПЕРВАЯ ИЗ ДВУХ единственных строчек, где нужна именно DateTime.Now!!!
					isMarketReplay = true;
					//и на маркет-реплее нет смысла рефрешить новости постоянно - достаточно раз в сутки... ну пусть будет раз в 12 часов
					newsRefeshInterval = 12*60;
				} else {
					//  в противном случае - скорее всего на живом коннекте в реал-тайме... так и будем считать!
					NOW = DateTime.Now; // ВТОРАЯи посленяя ИЗ ДВУХ единственных строчек, где нужна именно DateTime.Now!!!
					isMarketReplay = false;
				}
				*/
				
				// Поэтому пока определяем просто по настройкам индикатора (уже сделано) и тут теперь на эти настройки реагируем:
				if(isMarketReplay) {
					// Это фактически обозначает, что мы не на живом подключении - то есть мы в МаркетРеплее сейчас
					NOW = (Bars.MarketData.Connection.Options.Provider == Cbi.Provider.Replay ? Bars.MarketData.Connection.Now : DateTime.Now);  // ПЕРВАЯ ИЗ ДВУХ единственных строчек, где нужна именно DateTime.Now!!!
					//и на маркет-реплее нет смысла рефрешить новости постоянно - достаточно раз в сутки... ну пусть будет раз в 12 часов
					newsRefeshInterval = 12*60;
				} else {
					//  в противном случае - скорее всего на живом коннекте в реал-тайме... так и будем считать!
					NOW = DateTime.Now; // ВТОРАЯ и последняя ИЗ ДВУХ единственных строчек, где нужна именно DateTime.Now!!!
					//NOW = Time[0];
				}
				
				
				if(debug) Print("Now is: " + NOW);
				// и теперь надо ВЕЗДЕ ДАЛЬШЕ поменять вызов DateTime.Now на обращение к NOW!!!

				// Обновление списка новостей - один раз в заданное количество минут не смотря на таймфрейм графика!!!
				#region NEWSREFRESHER
				// Мы должны проверять новости, допустим, каждые 15 минут (5, 3, 1 - не важно, это настроено в переменной newsRefeshInterval)
				// Но свечка может быть на час, или на 1000ТИК - так что нам надо точно знать, что наш рефреш новостного списка сработает не на новой свечке
				// а именно раз в заданное количество минут - только сам индикатор должен иметь свойство CalculateOnBarClose = false;				
				if (Time[0] >= lastMinute.AddMinutes(1)) {
					if (debug) Print("ONE minute is up... Time[0] = " + Time[0] + " and LastMinute = " + lastMinute);
					lastMinute = Time[0];
					// вот и будем проверять не интервал свечек, а минутные интервалы					
					if (lastNewsUpdate.AddMinutes(newsRefeshInterval) < NOW){
						if (debug) Print("LastMinute = " + lastMinute + " and LastNewsUpdate = " + lastNewsUpdate);
						// Загрузка новостей - у нас в этом случае она отсутствует, так как мы просто обрабатываем
						// заданные в НАСТРОЙКАХ временные интервалы!!!
						//if(!FirstLoadNews) LoadNews(); 
						LoadNews(); 
					}
				}				
				#endregion NEWSREFRESHER
				
				// С первым тиком нового бара мы проверяем:
				// - есть ли у нас ПРЕДСТОЯЩИЕ новости
				// - есть ли у нас необходимость ЗВОНКА (алерта)
				// - есть ли у нас начало или продолжение КОЛОННЫ на графике
				// КАК мы это можем сделать?
				#region FIRSTTICKOFBAR
				if(FirstTickOfBar) {
					// свечка только началась
					//CanTrade[0] = 1; // статус новости пока еще не определен и не ясно можно ли торговать или нет
					if (newsEvents != null && newsEvents.Length > 0) { // Если у нас не пустой список новостей (список готовится при инициализации в функции LoadNews)
						// то устраиваем в цикле перебор всех новостей в этом списке
						for(int x = 0; x < newsEvents.Length ; x++){
							NewsEvent item = newsEvents[x];
							if (debug) Print(newsEvents[x].ToString());
							if (item.DateTimeLocal >= NOW ){
								// Нашли первое событие, которое еще НЕ произошло, но предстоит 
									newsItemPtr = x;	// выставили указатель в списке на СЛЕДУЮЩУЮ новость
									break; // дальше в цикле нам делать нечего - прерываемся
							} else {
								// Эта новость уже просрочена - значит ее номер нам и нужен для отрисовки ОКОНЧАНИЯ КОЛОННЫ!!!
								lastItemPtr = prevCounter;
								prevCounter += 1; // увеличим счетчик
							}
						}
						if (debug) Print(string.Format("Pointers: NEXT={0} | LAST={1} | COUNTER={2} | TOTAL={3}",newsItemPtr, lastItemPtr, prevCounter, newsEvents.Length));
						
					}
					
					if(showStat) {
						// Если в настройках задано выводить текстовые сообщения о статусе работы индикатора
						newsCounter = newsEvents.Length - prevCounter;
						string statusMsg = "";
						if(newsCounter > 0) {
							// если еще остались неотработавшие новости - выведем сообщение
							statusMsg = "Еще ожидаем новостей: " + newsCounter;
							string addMsg = "";
							int p = newsItemPtr;
							if(showStatNext) {		// Если в настройках задано выводить СЛЕДУЮЩИЕ ТРИ новости в статус
								// то минимум одну следующую новость мы отсюда точно можем вывести
								addMsg = "\nСледующая новость:\n " + newsEvents[p].ToStatus();
								if(newsCounter > 1) {
									// значит можем вывести и вторую новость
									addMsg = "\nСледующие 2 новости:\n " + newsEvents[p].ToStatus();
									p += 1;
									addMsg += "\n " + newsEvents[p].ToStatus();	
									if(newsCounter > 2) {
										// значит можем вывести и третью новость
										p = newsItemPtr;
										addMsg = "\nСледующие 3 новости:\n " + newsEvents[p].ToStatus();
										p += 1;
										addMsg += "\n " + newsEvents[p].ToStatus();										
										p += 1;
										addMsg += "\n " + newsEvents[p].ToStatus();										
									}
								}
								statusMsg += addMsg;
							} else {
								// Иначе просто выводим время следующей новости
								statusMsg += "\nСледующая новость в: " + newsEvents[newsItemPtr].DateTimeLocal.ToString("HH:mm");
							}
							
						} else {
							// Если больше нет новостей - тоже сообщим об этом
							statusMsg = statusTextLine1;
							statusMsg += "\n" + statusTextLine2;
						}
						// Ну и в любом случае выводим это СТАТУСНОЕ сообщение
						DrawTextFixed("tagStatusMsg", statusMsg, TextPosition.TopLeft, stColor, stFont, Color.Transparent, Color.Transparent, 0);
					}
				}
				#endregion FIRSTTICKOFBAR
								
				#region PREVNEWS
				if(lastItemPtr >= 0) {
					// У нас была ПРЕДЫДУЩАЯ новость
					// Причем, только на первом ТИКЕ бара мы тут можем оказаться, то есть все внутри этого блока будет проделано ОДИН раз за свечку
					// Надо проверить, сколько времени прошло ПОСЛЕ той новости и надо ли ЕЩЁ рисовать КООЛННУ
					e = newsEvents[lastItemPtr];
					diff = NOW - e.DateTimeLocal;
					int diffAfter = (int)diff.TotalMinutes;
					if (showEC && (diffAfter < minutesAfter)){ 
						// если ПОСЛЕ последней новости прошло еще МЕНЬШЕ чем minutesAfter минут
						// и в параметрах указано рисовать на графике событие - то продолжаем рисовать КОЛОННУ
						if (diffAfter == 0) {
							// У НАС ПРЯМО СЕЙЧАС ВРЕМЯ ВЫХОДА НОВОСТИ!!!
							if(debug) Print("NEWS TIME NOW - AFTER!" + NOW);
							BackColor = ecColor;
							//CanTrade[0] = -1; // Торговать точно НЕЛЬЗЯ!!!
							//PlotColors[0][0] = Color.Red;
						} else {
							if(debug) {
								Print("LINE in COLUMN after event!");
								Print(" _ _ _ NOW: " + NOW.Minute + " || EVENT: " +e.DateTimeLocal.Minute + " || So diff=" +  diff.TotalMinutes + " || MinAfter = " + minutesAfter);
								Print("---");
							}
							BackColor = ecColor;
							//CanTrade[0] = -1; // Торговать точно НЕЛЬЗЯ!!!
							//PlotColors[0][0] = Color.Red;
						}
						if(debug) Print("diff AFTER = " + diffAfter);
					}
					if (showPopUp && (diffAfter == minutesAfter)){
						// Если время уже прошло - гасим надпись
						RemoveDrawObject("tagMsgBox");
						if(debug) Print("POPUP HIDE after event!" + NOW + " | -->" +  diffAfter);
						// Мы будем показывать это окошко только один раз до события!!!
						//CanTrade[0] = 1; // Торговать вроде бы как МОЖНО...
						//PlotColors[0][0] = Color.Lime;
					} else {
						//CanTrade[0] = -1; // Торговать точно НЕЛЬЗЯ!!!
						//PlotColors[0][0] = Color.Red;
					}
				} else {
					//CanTrade[0] = 1; // Торговать вроде бы как МОЖНО...
					//PlotColors[0][0] = Color.Lime;
				}
				#endregion PREVNEWS
				
				#region NEXTNEWS
				if(newsItemPtr >= 0) {
					// У нас есть впереди СЛЕДУЮЩАЯ новость
					// Причем, только на первом ТИКЕ бара мы тут можем оказаться, то есть все внутри этого блока будет проделано ОДИН раз за свечку
					// Надо проверить, сколько времени осталось ДО этой новости и надо ли УЖЕ рисовать КООЛННУ и/или подавать сигнал-алерт				
					e = newsEvents[newsItemPtr];
					diff = e.DateTimeLocal - NOW;
					int diffBefore = (int)diff.TotalMinutes;
					if (diffBefore == 0){
						// У НАС ПРЯМО СЕЙЧАС ВРЕМЯ ВЫХОДА НОВОСТИ!!!
						if(debug) Print("NEWS TIME NOW!" + NOW);
						BackColor = ecColor; 
						//CanTrade[0] = -1; // Торговать точно НЕЛЬЗЯ!!!
						//PlotColors[0][0] = Color.Red;
						//DrawVerticalLine("News" + e.DateTimeLocal.ToString(), 0, Color.Yellow, DashStyle.Dash, 2);
						DrawVerticalLine("News" + e.DateTimeLocal.ToString(), e.DateTimeLocal, elColor, DashStyle.Dash, 2);
					} else {
						// До очередной новости еще есть время!
						if (sendAlerts && (diffBefore <= alertInterval)){
							// если ДО следующей новости осталось НЕ больше alertInterval минут
							// и в параметрах указано подавать сигнал - создаем алерт
							Alert("SavosNewsAlert"+e.ID + e.DateTimeLocal.ToString(), Priority.High, "Economic News Event!!!", alertWavFileName, Int32.MaxValue, Color.Blue, Color.White);
							if(debug) Print("ALERT before event!" + NOW + " Event will be at " + e.DateTimeLocal + " | -->" +  diffBefore);
							// Мы будем создавать новый АЛЕРТ каждую минуту теперь до наступления события!!!
							// Ведь возможно до события осталось меньше, чем задано в интервале на алерт, 
							// Но трейдер только запустил терминал - тогда он ПРОПУСТИТ алерт (я так полагаю)
							// Если мы его не пересоздадим повторно и повторно...
							// На самом деле настоящий алерт будет только один (в отличии от вывода в окне отладки)
							// Так как несколько алертов с одним и тем же ID просто нинзя игнорирует					
						} 
						//if (showPopUp && (diffBefore <= popupInterval) && (diffBefore > popupInterval - 2)){
						if (showPopUp && (diffBefore <= popupInterval)){
							// если ДО следующей новости осталось РОВНО popupInterval минут
							// и в параметрах указано выводить окно с сообщением - создаем ПопАп-окно
							string message = msgTextLine1;
							string message2 = "";
							if(showNewsTitle) {
								message = "       " + message + "\n" + "             | " + e.Time + " |\n";
								if(e.Priority == "  *") message2 = "               <low>\n";
								if(e.Priority == " **") message2 = "               <med>\n";
								if(e.Priority == "***") message2 = "              <HIGH!>\n";
								message += message2;
								if(e.Title.Length > 36) message2 = e.Title.Substring(0,33) + "...";
								message += message2;
							} else {
								message += "\n" + msgTextLine2;
							}
							DrawTextFixed("tagMsgBox", message, TextPosition.Center, Color.Red, new Font("Courier New", 14), Color.Red, Color.Firebrick, 4);
							if(debug) Print("POPUP SHOW before event!" + NOW + " | -->" +  diffBefore);
							// Мы будем показывать это окошко только один раз до события!!!
						}
						if (showEC && (diffBefore <= minutesBefore)){
							// если ДО следующей новости осталось НЕ больше minutesBefore минут
							// и в параметрах указано рисовать на графике событие - то начинаем рисовать КОЛОННУ
							//Print("LINE in COLUMN before event!" + NOW + " | -->" +  diff.TotalMinutes);
							if(debug) {
								Print("LINE in COLUMN before event!");
								Print(" _ _ _ NOW: " + NOW.Minute + " || EVENT: " +e.DateTimeLocal.Minute + " || So diff=" +  diffBefore + " || MinBefore = " + minutesBefore);
								Print("---");
							}
							BackColor = ecColor;
							//CanTrade[0] = -1; // Торговать точно НЕЛЬЗЯ!!!
							//PlotColors[0][0] = Color.Red;
						} else {
							//CanTrade[0] = 1; // Торговать вроде бы как МОЖНО...
							//PlotColors[0][0] = Color.Lime;
						}
					}
					if(debug) Print("diff BEFORE = " + diffBefore);
				}
				#endregion NEXTNEWS
			
				// РАБОТА ДЛЯ ВНЕШНЕГО СИГНАЛА НА СТРАТЕГИЮ
				//=======================================
				if(BackColor == ecColor)
				{
					CanTrade[0] = -1; // Торговать точно НЕЛЬЗЯ!!!
					PlotColors[0][0] = Color.Red;
				} else {
					CanTrade[0] = 1; // Торговать точно МОЖНО!!!
					PlotColors[0][0] = Color.Lime;
				}

			}			
			//DrawTextFixed("from", "from CL-Trade.Net\n", TextPosition.BottomLeft, Color.White, new Font("Arial",10, FontStyle.Regular), Color.Transparent, Color.Transparent, 0);
		}
		#endregion ONBARUPDATE
		
		/// <summary>
		/// Тут дальше пошло создание параметров для отображения в окне настроек индикатора
		/// </summary>		
		#region Properties
		[Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
		[XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public DataSeries CanTrade
		{
			get { return Values[0]; }
		}
		
		[Description("Time-Zone. For example, Moscow is '3' (three)...")]
		[Category(" 01. Settings")]
		[Gui.Design.DisplayNameAttribute("\t Time-Zone")]
		public int TimeZone
		{
			get { return timeZone; }
			set { timeZone = Math.Max(0,value); }
		}		
		
		[Description("Show USA News?")]
		[Category(" 01. Settings")]
		[Gui.Design.DisplayNameAttribute("\t\t USA News")]
		public bool ShowUSA
		{
			get { return showUS; }
			set { showUS = value; }
		}		
		[Description("Show EURO-Zone News?")]
		[Category(" 01. Settings")]
		[Gui.Design.DisplayNameAttribute("\t\t\t EURO-Zone News")]
		public bool ShowEUR
		{
			get { return showEU; }
			set { showEU = value; }
		}		
		[Description("Show Great Britain News?")]
		[Category(" 01. Settings")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t Great Britain News")]
		public bool ShowGB
		{
			get { return showGB; }
			set { showGB = value; }
		}		
		[Description("Show Germany News?")]
		[Category(" 01. Settings")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t\t Germany News")]
		public bool ShowDE
		{
			get { return showDE; }
			set { showDE = value; }
		}		

		[Description("Show LOW priority News?")]
		[Category(" 01. Settings")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t\t\t LOW priority News")]
		public bool ShowLOW
		{
			get { return showLow; }
			set { showLow = value; }
		}		
		[Description("Show MEDIUM priority News?")]
		[Category(" 01. Settings")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t\t\t\t MEDIUM priority News")]
		public bool ShowMED
		{
			get { return showMed; }
			set { showMed = value; }
		}		
		[Description("Show HIGH priority News?")]
		[Category(" 01. Settings")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t\t\t\t\t HIGH priority News")]
		public bool ShowHIGH
		{
			get { return showHigh; }
			set { showHigh = value; }
		}		


		[Description("Show Status Messages?")]
		[Category(" 02. StatusMessages")]
		[Gui.Design.DisplayNameAttribute("\t Status Messages")]
		public bool ShowStatus
		{
			get { return showStat; }
			set { showStat = value; }
		}		
		[Description("Enter Status NO-NEWS line#2")]
		[Category(" 02. StatusMessages")]
		[Gui.Design.DisplayNameAttribute("\t\t NO-NEWS line#2")]
		public string StatText2
		{
			get { return statusTextLine2; }
			set { statusTextLine2 = value; }
		}
		[Description("Show NEXT news at Status?")]
		[Category(" 02. StatusMessages")]
		[Gui.Design.DisplayNameAttribute("\t\t\t NEXT news at Status")]
		public bool ShowStatusNext
		{
			get { return showStatNext; }
			set { showStatNext = value; }
		}						
		[XmlIgnore()]
		[Description("Select Status Line Font")]
		[Category(" 02. StatusMessages")]
		[Gui.Design.DisplayName("\t\t\t\t STATUS Font")]
		public Font Stfont		
		{
			get { return stFont; }
			set { stFont = value; }
		}	
		[Browsable(false)]
		public string StFontSerialize
		{
    		get { return NinjaTrader.Gui.Design.SerializableFont.ToString(stFont); }
     		set { stFont = NinjaTrader.Gui.Design.SerializableFont.FromString(value); }
		}

		[XmlIgnore()]
        [Description("Color of STATUS text line")]
		[Category(" 02. StatusMessages")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t\t STATUS Color")]
        public Color StColor
        {
            get { return stColor; }
            set { stColor = value; }
        }
		[Browsable(false)]
		public string StColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(StColor); }
			set { StColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		
		
		[Description("Show MessageBox before Event?")]
		[Category(" 03. MessageBoxes")]
		[Gui.Design.DisplayNameAttribute("\t Message Box")]
		public bool ShowPP
		{
			get { return showPopUp; }
			set { showPopUp = value; }
		}		
		[Description("Minutes Before Event")]
		[Gui.Design.DisplayNameAttribute("\t\t Minutes BEFORE Event")]
		[Category(" 03. MessageBoxes")]
		public int MinPP
		{
			get { return popupInterval; }
			set { popupInterval = Math.Max(1, value); }
		}
		[Description("Text string #1")]
		[Gui.Design.DisplayNameAttribute("\t\t\t Text line #1")]
		[Category(" 03. MessageBoxes")]
		public string MsgLine1
		{
			get { return msgTextLine1; }
			set { msgTextLine1 = value; }
		}
		[Description("Text string #2")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t Text line #2")]
		[Category(" 03. MessageBoxes")]
		public string MsgLine2
		{
			get { return msgTextLine2; }
			set { msgTextLine2 = value; }
		}
		[Description("Show the current News Title into MessageBox?")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t\t Show News Title")]
		[Category(" 03. MessageBoxes")]
		public bool ShowNewsTitle
		{
			get { return showNewsTitle; }
			set { showNewsTitle = value; }
		}

		
		[Description("Send Alert with SOUND before Event?")]
		[Category(" 04. Alerts")]
		[Gui.Design.DisplayNameAttribute("\t Alert and Sound")]
		public bool SendAlertSound
		{
			get { return sendAlerts; }
			set { sendAlerts = value; }
		}		
		[Description("How many minutes Before Event Alert must be send?")]
		[Gui.Design.DisplayNameAttribute("\t\t Minutes BEFORE Event")]
		[Category(" 04. Alerts")]
		public int MinAS
		{
			get { return alertInterval; }
			set { alertInterval = Math.Max(1, value); }
		}

		[Description("Sound file name for alert (*.WAV)")]
		[Category(" 04. Alerts")]
		[Gui.Design.DisplayNameAttribute("\t\t\t Sound Alert File:")]
		//[Editor(typeof(System.Windows.Forms.Design.FileNameEditor),  typeof(System.Drawing.Design.UITypeEditor))]
        public string SoundFile
        {
            get{return alertWavFileName;}
            set{alertWavFileName = value;}
        }
		
		[Description("Show Vertical Line on Chart?")]
		[Category(" 05. ChartColumns")]
		[Gui.Design.DisplayNameAttribute("\t Draw Events")]
		public bool ShowEC
		{
			get { return showEC; }
			set { showEC = value; }
		}		
		[Description("How many minutes BEFORE Event need to Draw Column on the chart?")]
		[Category(" 05. ChartColumns")]
		[Gui.Design.DisplayNameAttribute("\t\t Minutes BEFORE Event")]
		public int MinBefore
		{
			get { return minutesBefore; }
			set { minutesBefore = Math.Max(1, value); }
		}
		[Description("How many minutes AFTER Event need to Draw Column on the chart?")]		
		[Category(" 05. ChartColumns")]
		[Gui.Design.DisplayNameAttribute("\t\t\t Minutes AFTER Event")]
		public int MinAfter
		{
			get { return minutesAfter; }
			set { minutesAfter = Math.Max(1, value); }
		}		
		[XmlIgnore()]
        [Description("Color of the EVENT's Column before and after news")]
        [Category(" 05. ChartColumns")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t Column Color")]
        public Color EcColor
        {
            get { return ecColor; }
            set { ecColor = value; }
        }
		[Browsable(false)]
		public string EcColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(EcColor); }
			set { EcColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}		
		[XmlIgnore()]
        [Description("Color of the EVENT's Line - news time only")]
        [Category(" 05. ChartColumns")]
		[Gui.Design.DisplayNameAttribute("\t\t\t\t\t EVENTS's Color")]
        public Color ElColor
        {
            get { return elColor; }
            set { elColor = value; }
        }
		[Browsable(false)]
		public string ElColorSerialize
		{
			get { return NinjaTrader.Gui.Design.SerializableColor.ToString(ElColor); }
			set { ElColor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
		}
		
		[Description("Use MarketReplay Mode NOW?")]
		[Category(" 06. MarketReplay")]
		[Gui.Design.DisplayNameAttribute("MarketReplay NOW")]
		public bool isMarketReplayMode
		{
			get { return isMarketReplay; }
			set { isMarketReplay = value; }
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
        private _Savos_News_For_Strategy[] cache_Savos_News_For_Strategy = null;

        private static _Savos_News_For_Strategy check_Savos_News_For_Strategy = new _Savos_News_For_Strategy();

        /// <summary>
        /// Simple News or any other events remainder.
        /// </summary>
        /// <returns></returns>
        public _Savos_News_For_Strategy _Savos_News_For_Strategy()
        {
            return _Savos_News_For_Strategy(Input);
        }

        /// <summary>
        /// Simple News or any other events remainder.
        /// </summary>
        /// <returns></returns>
        public _Savos_News_For_Strategy _Savos_News_For_Strategy(Data.IDataSeries input)
        {
            if (cache_Savos_News_For_Strategy != null)
                for (int idx = 0; idx < cache_Savos_News_For_Strategy.Length; idx++)
                    if (cache_Savos_News_For_Strategy[idx].EqualsInput(input))
                        return cache_Savos_News_For_Strategy[idx];

            lock (check_Savos_News_For_Strategy)
            {
                if (cache_Savos_News_For_Strategy != null)
                    for (int idx = 0; idx < cache_Savos_News_For_Strategy.Length; idx++)
                        if (cache_Savos_News_For_Strategy[idx].EqualsInput(input))
                            return cache_Savos_News_For_Strategy[idx];

                _Savos_News_For_Strategy indicator = new _Savos_News_For_Strategy();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                Indicators.Add(indicator);
                indicator.SetUp();

                _Savos_News_For_Strategy[] tmp = new _Savos_News_For_Strategy[cache_Savos_News_For_Strategy == null ? 1 : cache_Savos_News_For_Strategy.Length + 1];
                if (cache_Savos_News_For_Strategy != null)
                    cache_Savos_News_For_Strategy.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cache_Savos_News_For_Strategy = tmp;
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
        /// Simple News or any other events remainder.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator._Savos_News_For_Strategy _Savos_News_For_Strategy()
        {
            return _indicator._Savos_News_For_Strategy(Input);
        }

        /// <summary>
        /// Simple News or any other events remainder.
        /// </summary>
        /// <returns></returns>
        public Indicator._Savos_News_For_Strategy _Savos_News_For_Strategy(Data.IDataSeries input)
        {
            return _indicator._Savos_News_For_Strategy(input);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Simple News or any other events remainder.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator._Savos_News_For_Strategy _Savos_News_For_Strategy()
        {
            return _indicator._Savos_News_For_Strategy(Input);
        }

        /// <summary>
        /// Simple News or any other events remainder.
        /// </summary>
        /// <returns></returns>
        public Indicator._Savos_News_For_Strategy _Savos_News_For_Strategy(Data.IDataSeries input)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator._Savos_News_For_Strategy(input);
        }
    }
}
#endregion
