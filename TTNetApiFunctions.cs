using System. Collections. Concurrent;
using System. Diagnostics;
using System. Text;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class TTNetApiFunctions
        {
        // Declare the API objects
        private TTAPI m_api = null;
        private tt_net_sdk.WorkerDispatcher m_disp = null;
        public Dispatcher disp;
        private IReadOnlyCollection<Account> m_accounts = null;
        private readonly ConcurrentDictionary<Instrument, PriceUpdate> priceUpdates=new ConcurrentDictionary<Instrument, PriceUpdate>();
        public bool first=true;

        bool price_error_1=false;
        bool price_error_2=false;
        bool order_update_error_1=false;
        bool order_update_error_2=false;
        private object m_Lock = new object();
        private bool m_isDisposed = false;
        Timer m_timer_3=null;
        TS_Subscriber ts_subscriber = null;
        string filepath= "C:\\tt\\order_details\\PIQ_Orders.csv";
        string filepath_secondary= "C:\\tt\\order_details\\PIQ_Orders_copy.csv";

        private readonly string account_name = "XGJRE"; // Enter your Account In
        private readonly int account_idx = 0;
        /* private readonly string account_name = "JRathore-SIM"; // Enter your Account In
         private readonly int account_idx = 0;*/

        ConcurrentBag<Order> book = new ConcurrentBag<Order>();
        private BlockingCollection<Order_enum> updateQueue = new BlockingCollection<Order_enum>();
        private BlockingCollection<OrderFilledEventArgs> updateQueue_fill = new BlockingCollection<OrderFilledEventArgs>();

        PriceUpdateService updateService = null;
        Mapping_Prices mp_prices = null;
        DateTime last_order_update = DateTime.MinValue;
        InstrumentManager instr_manager= null;
        Trade_Sub_processor ts_processor=null;
        TS_Publisher ts_publisher =null;
        ConcurrentDictionary<string,PriceUpdate>price_upd2=new ConcurrentDictionary<string, PriceUpdate>();
        BlockingCollection<string> price_upd = new BlockingCollection<string>();

        ConcurrentDictionary<string, OrderBookDets>orderBookDictionary=new  ConcurrentDictionary<string, OrderBookDets>();
        DateTime last_update_time = DateTime.MinValue;
        public ConcurrentDictionary<Instrument, TradingLegs> parentInstrumentMap = new ConcurrentDictionary<Instrument, TradingLegs>();
        public ConcurrentDictionary<Instrument, TradingLegs> allInstrumentMap = new ConcurrentDictionary<Instrument, TradingLegs>();
        public ConcurrentDictionary<string, Instrument> ordermapping = new ConcurrentDictionary<string, Instrument>();
        public ConcurrentDictionary<Tuple<Instrument, Instrument>, Hedge_Dets> legmapping = new ConcurrentDictionary<Tuple<Instrument, Instrument>, Hedge_Dets>();
        public ConcurrentDictionary<string,Tuple<Instrument, Instrument>> hede_map = new ConcurrentDictionary<string,Tuple<Instrument, Instrument>>();

        private string webhookurl="https://prod-173.westeurope.logic.azure.com:443/workflows/9850082edcef43b6a45ee6c5a23322cd/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=oT0_nkmd09xv-63z0Wg7ppb1uuCvQTbmTTgpFouBSoQ";

        public void Start ( tt_net_sdk. TTAPIOptions apiConfig )
            {
            m_disp = tt_net_sdk. Dispatcher. AttachWorkerDispatcher ( );
            m_disp. DispatchAction ( ( ) =>
            {
                Init ( apiConfig );
            } );

            m_disp. Run ( );
            }

        public void Init ( tt_net_sdk. TTAPIOptions apiConfig )
            {
            ApiInitializeHandler apiInitializeHandler = new ApiInitializeHandler(ttNetApiInitHandler);
            TTAPI. ShutdownCompleted += TTAPI_ShutdownCompleted;
            TTAPI. CreateTTAPI ( tt_net_sdk. Dispatcher. Current, apiConfig, apiInitializeHandler );
            }

        public void ttNetApiInitHandler ( TTAPI api, ApiCreationException ex )
            {
            if ( ex == null )
                {
                Console. WriteLine ( "TT.NET SDK INITIALIZED" );

                // Authenticate your credentials
                m_api = api;
                m_api. TTAPIStatusUpdate += new EventHandler<TTAPIStatusUpdateEventArgs> ( m_api_TTAPIStatusUpdate );
                m_api. Start ( );
                }
            else if ( ex. IsRecoverable )
                {
                // this is in informational update from the SDK
                Console. WriteLine ( "TT.NET SDK Initialization Message: {0}", ex. Message );
                if ( ex. Code == ApiCreationException. ApiCreationError. NewAPIVersionAvailable )
                    {
                    // a newer version of the SDK is available - notify someone to upgrade
                    }
                }
            else
                {
                Console. WriteLine ( "TT.NET SDK Initialization Failed: {0}", ex. Message );
                if ( ex. Code == ApiCreationException. ApiCreationError. NewAPIVersionRequired )
                    {
                    // do something to upgrade the SDK package since it will not start until it is upgraded 
                    // to the minimum version noted in the exception message
                    }
                Dispose ( );
                }
            }


        public void m_api_TTAPIStatusUpdate ( object sender, TTAPIStatusUpdateEventArgs e )
            {
            Console. WriteLine ( "TTAPIStatusUpdate: {0}", e );
            if ( DateTime. Now. Hour == 21 || DateTime. Now. Hour == 22 || DateTime. Now. Hour == 23 )
                {
                Logger. InformationAsync ( "ttapi,{0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );
                }
            if ( e. IsReady == false )
                {
                // TODO: Do any connection lost processing here
                return;
                }
            if ( first )
                {
                disp = Dispatcher. Current;
                // Get the accounts
                m_accounts = m_api. Accounts;
                ts_publisher = new TS_Publisher ( disp, account_name );
                ts_processor = new Trade_Sub_processor ( book, updateQueue, updateQueue_fill, priceUpdates, Dispatcher. Current, account_name, ts_publisher, m_accounts. ElementAt ( account_idx ), parentInstrumentMap, this, ordermapping, priceUpdates, hede_map, legmapping );

                m_timer_3 = new System. Threading. Timer (
                callback: new TimerCallback ( m_ase_updater ),  // The method to call
                state: null,  // No state to pass, set to null
                dueTime: 0,  // Start immediately
                period: 120000  // Fire every 1000ms (60 second)
            );

                ts_subscriber = new TS_Subscriber ( updateQueue, updateQueue_fill, book, ts_processor );
                List<string> subscriptionTypes = new List<string> { "order_update", "order_delete", "order_filled", "order_add", "order_book" ,"order_reject"};
                ts_publisher. AddSubscriber ( ts_subscriber, subscriptionTypes );
                updateService = new PriceUpdateService ( Dispatcher. Current, price_upd, price_upd2 );
                ReadOrderBookFromCSV ( );

                instr_manager = new InstrumentManager ( parentInstrumentMap, allInstrumentMap, updateService, Dispatcher. Current );


                instr_manager. LookupAndStoreInstruments ( "SR3 Mar26 3mo Double Butterfly", "SR3 Mar26-Dec26 Calendar", "SR3 Jun26-Sep26 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Jun26 3mo Double Butterfly", "SR3 Jun26-Mar27 Calendar", "SR3 Sep26-Dec26 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Sep26 3mo Double Butterfly", "SR3 Sep26-Jun27 Calendar", "SR3 Dec26-Mar27 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Dec26 3mo Double Butterfly", "SR3 Dec26-Sep27 Calendar", "SR3 Mar27-Jun27 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Mar27 3mo Double Butterfly", "SR3 Mar27-Dec27 Calendar", "SR3 Jun27-Sep27 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Jun27 3mo Double Butterfly", "SR3 Jun27-Mar28 Calendar", "SR3 Sep27-Dec27 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Sep27 3mo Double Butterfly", "SR3 Sep27-Jun28 Calendar", "SR3 Dec27-Mar28 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Dec27 3mo Double Butterfly", "SR3 Dec27-Sep28 Calendar", "SR3 Mar28-Jun28 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Mar28 3mo Double Butterfly", "SR3 Mar28-Dec28 Calendar", "SR3 Jun28-Sep28 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Jun28 3mo Double Butterfly", "SR3 Jun28-Mar29 Calendar", "SR3 Sep28-Dec28 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Sep28 3mo Double Butterfly", "SR3 Sep28-Jun29 Calendar", "SR3 Dec28-Mar29 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );
                instr_manager. LookupAndStoreInstruments ( "SR3 Dec28 3mo Double Butterfly", "SR3 Dec28-Sep29 Calendar", "SR3 Mar29-Jun29 Calendar", "SR3", "SR3", "SR3", 1, -3, 10, 3 );

                mp_prices = new Mapping_Prices ( parentInstrumentMap, allInstrumentMap, ordermapping, m_accounts. ElementAt ( account_idx ), ts_publisher, priceUpdates, last_order_update, orderBookDictionary );
                }
            }
        public void ReadOrderBookFromCSV ( )
            {

            try
                {
                using ( var reader = new StreamReader ( filepath ) )
                    {
                    string headerLine = reader.ReadLine(); // Skip first header row


                    while ( !reader. EndOfStream )
                        {
                        string line = reader.ReadLine();
                        var values = line.Split(',');

                        // Ensure there are enough columns
                        if ( values. Length < 11 )
                            continue;

                        string parentInstr = values[0].Trim();
                        string leg1 = values[1].Trim();
                        string leg2 = values[2].Trim();

                        // Extract Ask (Sell) Orders
                        List<string> sellOrders = new List<string>
                        {
                            values[3].Trim().Trim('"'),
                            values[4].Trim().Trim('"'),
                            values[5].Trim().Trim('"'),
                            values[6].Trim().Trim('"')
                        };

                        // Extract Bid (Buy) Orders
                        List<string> buyOrders = new List<string>
                        {
                            values[7].Trim().Trim('"'),
                            values[8].Trim().Trim('"'),
                            values[9].Trim().Trim('"'),
                            values[10].Trim().Trim('"')
                        };


                        // Store in dictionary using Leg2 as the key
                        orderBookDictionary [ leg2 ] = new OrderBookDets ( parentInstr, leg1, leg2, buyOrders, sellOrders );
                        }
                    }

                if ( !string. IsNullOrEmpty ( filepath_secondary ) )
                    {
                    File. Copy ( filepath, filepath_secondary, overwrite: true );
                    File. Delete ( filepath );
                    }
                }
            catch ( Exception ex )
                {
                Console. WriteLine ( ex. Message );
                }
            }
        private void m_ase_updater ( object state )
            {

            DateTime startTime = DateTime.Today.Add(new TimeSpan(21, 59, 0)); // 9:59 PM today
            DateTime endTime = DateTime.Today.Add(new TimeSpan(23, 01, 0));   // 11:01 PM today

            DateTime currenttime= DateTime.Now;
            if ( currenttime. DayOfWeek == DayOfWeek. Friday &&
                    currenttime. TimeOfDay >= new TimeSpan ( 23, 02, 0 ) )
                {
                StoreOrders ( parentInstrumentMap );

                Environment. Exit ( 0 );
                }
            if ( ( currenttime > startTime && currenttime < endTime ) || currenttime. Hour == 22 )
                {

                }
            else
                {
                if ( ( currenttime - last_update_time ). Minutes >= 3 && last_update_time != DateTime. MinValue && !price_error_1 && !order_update_error_1 )
                    {
                    price_error_1 = true;
                    TeamsPOST m_teamsPost = new TeamsPOST ( );
                    Task. Run ( ( ) =>
                    {
                        _ = m_teamsPost. SendMessageAndCsvFile ( webhookurl, $"PRICE issue " );
                        ;

                    } );
                    //no price has been recieved for past 30 minutes lets try to resubscibe the prices
                    instr_manager. lookupandStoreInstr2 ( );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Dec25 3mo Double Butterfly", "SR3 Dec25-Sep26 Calendar", "SR3 Mar26-Jun26 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Mar26 3mo Double Butterfly", "SR3 Mar26-Dec26 Calendar", "SR3 Jun26-Sep26 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Jun26 3mo Double Butterfly", "SR3 Jun26-Mar27 Calendar", "SR3 Sep26-Dec26 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Sep26 3mo Double Butterfly", "SR3 Sep26-Jun27 Calendar", "SR3 Dec26-Mar27 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Dec26 3mo Double Butterfly", "SR3 Dec26-Sep27 Calendar", "SR3 Mar27-Jun27 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Mar27 3mo Double Butterfly", "SR3 Mar27-Dec27 Calendar", "SR3 Jun27-Sep27 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Jun27 3mo Double Butterfly", "SR3 Jun27-Mar28 Calendar", "SR3 Sep27-Dec27 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Sep27 3mo Double Butterfly", "SR3 Sep27-Jun28 Calendar", "SR3 Dec27-Mar28 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Dec27 3mo Double Butterfly", "SR3 Dec27-Sep28 Calendar", "SR3 Mar28-Jun28 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Mar28 3mo Double Butterfly", "SR3 Mar28-Dec28 Calendar", "SR3 Jun28-Sep28 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Jun28 3mo Double Butterfly", "SR3 Jun28-Mar29 Calendar", "SR3 Sep28-Dec28 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Sep28 3mo Double Butterfly", "SR3 Sep28-Jun29 Calendar", "SR3 Dec28-Mar29 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Dec28 3mo Double Butterfly", "SR3 Dec28-Sep29 Calendar", "SR3 Mar29-Jun29 Calendar", "SR3", "SR3", "SR3" );
                    }
                else if ( ( currenttime - last_update_time ). Minutes < 3 && last_update_time != DateTime. MinValue )
                    {
                    price_error_1 = false;

                    }
                else if ( ( currenttime - last_update_time ). Minutes >= 5 && last_update_time != DateTime. MinValue && !price_error_2 )
                    {
                    TeamsPOST m_teamsPost = new TeamsPOST ( );
                    Task. Run ( ( ) =>
                    {
                        _ = m_teamsPost. SendMessageAndCsvFile ( webhookurl, $"price issue restarting application " );
                        ;

                    } );
                    StoreOrders ( parentInstrumentMap );
                    Process. Start ( "restart_helper.bat" );
                    Environment. Exit ( 4 );
                    }
                if ( ( currenttime - last_order_update ). Minutes >= 3 && last_order_update != DateTime. MinValue && !order_update_error_1 && !price_error_1 )
                    {
                    order_update_error_1 = true;
                    TeamsPOST m_teamsPost = new TeamsPOST ( );
                    Task. Run ( ( ) =>
                    {
                        _ = m_teamsPost. SendMessageAndCsvFile ( webhookurl, $"order not updated issue " );
                        ;

                    } );
                    //no price has been recieved for past 30 minutes lets try to resubscibe the prices
                    instr_manager. lookupandStoreInstr2 ( );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Dec25 3mo Double Butterfly", "SR3 Dec25-Sep26 Calendar", "SR3 Mar26-Jun26 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Mar26 3mo Double Butterfly", "SR3 Mar26-Dec26 Calendar", "SR3 Jun26-Sep26 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Jun26 3mo Double Butterfly", "SR3 Jun26-Mar27 Calendar", "SR3 Sep26-Dec26 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Sep26 3mo Double Butterfly", "SR3 Sep26-Jun27 Calendar", "SR3 Dec26-Mar27 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Dec26 3mo Double Butterfly", "SR3 Dec26-Sep27 Calendar", "SR3 Mar27-Jun27 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Mar27 3mo Double Butterfly", "SR3 Mar27-Dec27 Calendar", "SR3 Jun27-Sep27 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Jun27 3mo Double Butterfly", "SR3 Jun27-Mar28 Calendar", "SR3 Sep27-Dec27 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Sep27 3mo Double Butterfly", "SR3 Sep27-Jun28 Calendar", "SR3 Dec27-Mar28 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Dec27 3mo Double Butterfly", "SR3 Dec27-Sep28 Calendar", "SR3 Mar28-Jun28 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Mar28 3mo Double Butterfly", "SR3 Mar28-Dec28 Calendar", "SR3 Jun28-Sep28 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Jun28 3mo Double Butterfly", "SR3 Jun28-Mar29 Calendar", "SR3 Sep28-Dec28 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Sep28 3mo Double Butterfly", "SR3 Sep28-Jun29 Calendar", "SR3 Dec28-Mar29 Calendar", "SR3", "SR3", "SR3" );
                    instr_manager. LookupAndStoreInstruments2 ( "SR3 Dec28 3mo Double Butterfly", "SR3 Dec28-Sep29 Calendar", "SR3 Mar29-Jun29 Calendar", "SR3", "SR3", "SR3" );
                    }
                else if ( ( currenttime - last_order_update ). Minutes < 3 && last_order_update != DateTime. MinValue )
                    {
                    order_update_error_1 = false;

                    }
                else if ( ( currenttime - last_order_update ). Minutes >= 5 && last_order_update != DateTime. MinValue && !order_update_error_2 )
                    {
                    TeamsPOST m_teamsPost = new TeamsPOST ( );
                    Task. Run ( ( ) =>
                    {
                        _ = m_teamsPost. SendMessageAndCsvFile ( webhookurl, $"order not updated issue restarting application " );
                        ;

                    } );
                    StoreOrders ( parentInstrumentMap );
                    Process. Start ( "restart_helper.bat" );
                    Environment. Exit ( 5 );
                    }

                }
            }
        public void Process_Updates ( )
            {
            foreach ( var update in price_upd. GetConsumingEnumerable ( ) )
                {
                if ( price_upd2. ContainsKey ( update ) )
                    {
                    mp_prices. Check_condition ( price_upd2 [ update ] );
                    }
                }
            }
        public void StoreOrders ( ConcurrentDictionary<Instrument, TradingLegs> parentInstrumentMap )
            {
            using ( var writer = new StreamWriter ( filepath, false, Encoding. UTF8 ) )
                {
                // Write headers
                writer. WriteLine ( "Parent_Instr,Leg1,Leg2,Ask Order1,Ask Order2,Ask Order3,Ask Order4,Bid Orders1,Bid Orders2,Bid Orders3,Bid Orders4," );

                foreach ( var kvp in parentInstrumentMap )
                    {
                    string parentInstr = kvp.Key.InstrumentDetails.Alias;
                    var details = kvp.Value;

                    // Combine ask orders into one string with quotes
                    string askOrders = $"\"{details.BestAskOrder}\",\"{details.SecondBestAskOrder}\",\"{details.ThirdBestAskOrder}\",\"{details.FourthBestAskOrder}\"";

                    // Combine bid orders
                    string bidOrders = $"\"{details.BestBidOrder}\",\"{details.SecondBestBidOrder}\",\"{details.ThirdBestBidOrder}\",\"{details.FourthBestBidOrder}\"";

                    // Write row
                    writer. WriteLine ( $"{parentInstr},{details. Leg1. InstrumentDetails. Alias},{details. Leg2. InstrumentDetails. Alias},{askOrders},{bidOrders}" );
                    }
                }
            }
        public void Dispose ( )
            {
            lock ( m_Lock )
                {
                if ( !m_isDisposed )
                    {
                    m_isDisposed = true;
                    }


                TTAPI. Shutdown ( );
                }
            }

        public void TTAPI_ShutdownCompleted ( object sender, EventArgs e )
            {
            // Dispose of any other objects / resources
            Console. WriteLine ( "TTAPI shutdown completed" );
            }
        }
    }
