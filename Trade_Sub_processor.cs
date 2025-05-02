using System. Collections. Concurrent;
using System. Text;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class Trade_Sub_processor
        {
        ConcurrentBag<Order> book = new ConcurrentBag<Order>();
        private BlockingCollection<Order_enum> updateQueue = new BlockingCollection<Order_enum>();
        private BlockingCollection<OrderFilledEventArgs> updateQueue_fill = new BlockingCollection<OrderFilledEventArgs>();

        public ConcurrentDictionary<string, Instrument> ordermapping = new ConcurrentDictionary<string, Instrument>();
        public ConcurrentDictionary<Tuple<Instrument, Instrument>, Hedge_Dets> legmapping = new ConcurrentDictionary<Tuple<Instrument, Instrument>, Hedge_Dets>();
        public ConcurrentDictionary<string,Tuple<Instrument, Instrument>> hede_map = new ConcurrentDictionary<string,Tuple<Instrument, Instrument>>();
        private readonly ConcurrentDictionary<Instrument, PriceUpdate> priceUpdates;
        public ConcurrentDictionary<Instrument, TradingLegs> parentInstrumentMap = new ConcurrentDictionary<Instrument, TradingLegs>();
        public ConcurrentDictionary<Tuple<Instrument,bool>, decimal> total_fill_count= new ConcurrentDictionary<Tuple<Instrument,bool>, decimal>();
        public ConcurrentDictionary<Tuple<Instrument,bool>,DateTime>parent_inst_last_fill=new ConcurrentDictionary<Tuple<Instrument,bool>, DateTime>();

        TS_Publisher ts_publisher = null;

        int fill_counter=0;
        int max_fill=60;

        private object m_obj = new object();
        private object m_obj2 = new object();
        private object m_obj1 = new object();
        private object m_obj3 = new object();
        private object m_obj4 = new object();

        string filepath= "C:\\tt\\order_details\\PIQ_Orders.csv";

        TTNetApiFunctions form1=null;
        private string webhookUrl_new = "https://hertshtengroup.webhook.office.com/webhookb2/04c9ca00-ec47-4281-8936-6f2d36918585@0753c1a4-2be6-4a86-8763-32ae847e1186/IncomingWebhook/13f981b4223b4a4a8426ecc629e05b6d/60ad9ba1-7d39-4c4f-a2bb-ba6ceb541d26/V2rSl5dEKcYyhfgMOtEvpDeVK5KSOLODtqEQjY9X2Hdek1";

        Account acc=null;
        public Trade_Sub_processor ( )
            {

            }
        public Trade_Sub_processor ( ConcurrentBag<Order> book1, BlockingCollection<Order_enum> updateQueu, BlockingCollection<OrderFilledEventArgs> updateQueue_fil, ConcurrentDictionary<Instrument, PriceUpdate> priceUpdates, Dispatcher disp, string account_name, TS_Publisher ts_p, Account acc, ConcurrentDictionary<Instrument, TradingLegs> parentInstrumentMaps, TTNetApiFunctions form, ConcurrentDictionary<string, Instrument> ordermap, ConcurrentDictionary<Instrument, PriceUpdate> priceUpdate, ConcurrentDictionary<string, Tuple<Instrument, Instrument>> Hede_map, ConcurrentDictionary<Tuple<Instrument, Instrument>, Hedge_Dets> legmap )
            {
            this. book = book1;
            this. updateQueue = updateQueu;
            this. updateQueue_fill = updateQueue_fil;
            this. ts_publisher = ts_p;
            this. parentInstrumentMap = parentInstrumentMaps;
            this. acc = acc;
            this. form1 = form;
            this. ordermapping = ordermap;
            this. legmapping = legmap;
            this. hede_map = Hede_map;
            this. priceUpdates = priceUpdate;


            }
        public void StartProcessingUpdates ( )
            {

            ProcessUpdates ( );
            }
        void ProcessUpdates ( )
            {

            ProcessUpdates_book ( );
            Task. Run ( ( ) =>
            {
                foreach ( var update in updateQueue. GetConsumingEnumerable ( ) )
                    {
                    switch ( update. Type )
                        {


                        case Order_enum. UpdateType. OrderAdded:

                            ProcessUpdate_Add ( update. OrderAdded. Order );

                            break;

                        case Order_enum. UpdateType. OrderUpdated:
                            ProcessUpdate_update ( update. OrderUpdated );
                            break;

                        case Order_enum. UpdateType. OrderDeleted:
                            ProcessUpdate_delete ( update. OrderDeleted );
                            break;

                        case Order_enum. UpdateType. OrderRejected:
                            ProcessUpdate_reject ( update. OrderRejected );
                            break;
                        }



                    }
            } );
            Task. Run ( ( ) =>
            {
                foreach ( var update in updateQueue_fill. GetConsumingEnumerable ( ) )
                    {
                    ProcessUpdate_fill ( update );
                    }
            } );

            }
        private void ProcessUpdate_reject ( OrderRejectedEventArgs e )
            {
            if ( e != null )
                {



                }
            }
        // Method to continuously process updates
        private void ProcessUpdates_book ( )
            {
            lock ( m_obj )
                {
                foreach ( var update in book )
                    {
                    Console. WriteLine ( update. Instrument. ToString ( ) );
                    Console. WriteLine ( update. Instrument. Name );

                    }
                Task. Run ( ( ) =>
                {
                    form1. Process_Updates ( );
                } );
                }
            }


        public void DeleteAllLegOrders ( TradingLegs td, bool buy )
            {
            if ( buy )
                {
                if ( td. BestBidOrder != string. Empty )
                    {
                    ts_publisher. delete_order ( td. BestBidOrder );

                    }
                if ( td. SecondBestBidOrder != string. Empty )
                    {
                    ts_publisher. delete_order ( td. SecondBestBidOrder );

                    }
                if ( td. ThirdBestBidOrder != string. Empty )
                    {
                    ts_publisher. delete_order ( td. ThirdBestBidOrder );

                    }
                if ( td. FourthBestBidOrder != string. Empty )
                    {
                    ts_publisher. delete_order ( td. FourthBestBidOrder );

                    }
                }
            else
                {
                if ( td. BestAskOrder != string. Empty )
                    {
                    ts_publisher. delete_order ( td. BestAskOrder );

                    }
                if ( td. SecondBestAskOrder != string. Empty )
                    {
                    ts_publisher. delete_order ( td. SecondBestAskOrder );

                    }
                if ( td. ThirdBestAskOrder != string. Empty )
                    {
                    ts_publisher. delete_order ( td. ThirdBestAskOrder );

                    }
                if ( td. FourthBestAskOrder != string. Empty )
                    {
                    ts_publisher. delete_order ( td. FourthBestAskOrder );

                    }
                }
            }
        private void ProcessUpdate_fill ( OrderFilledEventArgs e )
            {
            lock ( m_obj2 )
                {
                if ( e. OldOrder != null )
                    {
                    if ( DateTime. Now. Hour == 23 || DateTime. Now. Hour == 21 || DateTime. Now. Hour == 22 )
                        {
                        Logger. InformationAsync ( "\nOrderFilled [{0}]: {1}@{2}  {3} , {4}", e. Fill. SiteOrderKey, e. Fill. Quantity, e. Fill. MatchPrice, e. Fill. Instrument. InstrumentDetails. Alias, DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );
                        }

                    try
                        {
                        if ( e. FillType == tt_net_sdk. FillType. Full )
                            {
                            Console. WriteLine ( "\nOrderFullyFilled [{0}]: {1}@{2}", e. Fill. SiteOrderKey, e. Fill. Quantity, e. Fill. MatchPrice );


                            }
                        else
                            {
                            Console. WriteLine ( "\nOrderPartiallyFilled [{0}]: {1}@{2}", e. Fill. SiteOrderKey, e. Fill. Quantity, e. Fill. MatchPrice );
                            if ( e. NewOrder != null )
                                {

                                }
                            }
                        if ( e. Fill != null )
                            {


                            if ( ordermapping. ContainsKey ( e. Fill. SiteOrderKey ) && !e. Fill. IsExchangeSpreadLegFill )
                                {
                                if ( legmapping. ContainsKey ( Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ) ) )
                                    {
                                    Hedge_Dets h_d=legmapping[Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument )];
                                    if ( e. Fill. BuySell == BuySell. Buy )
                                        {
                                        int hedge_qt=h_d. Add_Buyhedge ( Convert. ToInt32 ( e. Fill. Quantity ) ,e.Fill.MatchPrice.ToDecimal());

                                        if ( hedge_qt > 0 )
                                            {
                                            OrderProfile op =Send_Order(h_d.hedge_instrument,BuySell.Buy,priceUpdates[h_d.hedge_instrument].BidPrice,hedge_qt,OrderType.Limit,TimeInForce.GoodTillCancel,acc,"Hedge_ReOrder(1,-1)",ordermapping [ e. Fill. SiteOrderKey ].InstrumentDetails.Alias );
                                            string order_key=ts_publisher.Send_Orders_Series(op);
                                            if ( order_key != string. Empty )
                                                {
                                                hede_map. TryAdd ( order_key, Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ) );
                                                h_d. buyquote_hedge. TryAdd ( order_key, true );
                                                }
                                            else
                                                {
                                                TeamsPOST m_teamsPost = new TeamsPOST ( );
                                                Task. Run ( ( ) =>
                                                {
                                                    _ = m_teamsPost. SendMessageAndCsvFile ( webhookUrl_new, $"Unable to send hedge order for instrument {h_d. hedge_instrument} for quantity {hedge_qt} buy " );
                                                    ;

                                                } );
                                                }
                                            }
                                        else
                                            {
                                            OrderProfile op =Send_Order(h_d.hedge_instrument,BuySell.Sell,priceUpdates[h_d.hedge_instrument].AskPrice,hedge_qt*-1,OrderType.Limit,TimeInForce.GoodTillCancel,acc,"Hedge_ReOrder(1,-1)" ,ordermapping [ e. Fill. SiteOrderKey ].InstrumentDetails.Alias);
                                            string order_key=ts_publisher.Send_Orders_Series(op);
                                            if ( order_key != string. Empty )
                                                {
                                                hede_map. TryAdd ( order_key, Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ) );
                                                h_d. buyquote_hedge. TryAdd ( order_key, true );

                                                }
                                            else
                                                {
                                                TeamsPOST m_teamsPost = new TeamsPOST ( );
                                                Task. Run ( ( ) =>
                                                {
                                                    _ = m_teamsPost. SendMessageAndCsvFile ( webhookUrl_new, $"Unable to send hedge order for instrument {h_d. hedge_instrument} for quantity {hedge_qt * -1} sell " );
                                                    ;

                                                } );
                                                }
                                            }
                                        }
                                    else
                                        {
                                        int hedge_qt=h_d. Add_Sellhedge ( Convert. ToInt32 ( e. Fill. Quantity ),e.Fill.MatchPrice.ToDecimal() );
                                        if ( hedge_qt < 0 )
                                            {
                                            OrderProfile op =Send_Order(h_d.hedge_instrument,BuySell.Buy,priceUpdates[h_d.hedge_instrument].BidPrice,hedge_qt*-1,OrderType.Limit,TimeInForce.GoodTillCancel,acc,"Hedge_ReOrder(1,-1)", ordermapping[e.Fill.SiteOrderKey].InstrumentDetails.Alias);
                                            string order_key=ts_publisher.Send_Orders_Series(op);
                                            if ( order_key != null )
                                                {
                                                hede_map. TryAdd ( order_key, Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ) );
                                                h_d. sellquote_hedge. TryAdd ( order_key, true );

                                                }
                                            else
                                                {
                                                TeamsPOST m_teamsPost = new TeamsPOST ( );
                                                Task. Run ( ( ) =>
                                                {
                                                    _ = m_teamsPost. SendMessageAndCsvFile ( webhookUrl_new, $"Unable to send hedge order for instrument {h_d. hedge_instrument} for quantity {hedge_qt * -1} buy" );
                                                    ;

                                                } );
                                                }
                                            }
                                        else if ( hedge_qt > 0 )
                                            {
                                            OrderProfile op =Send_Order(h_d.hedge_instrument,BuySell.Sell,priceUpdates[h_d.hedge_instrument].AskPrice,hedge_qt,OrderType.Limit,TimeInForce.GoodTillCancel,acc,"Hedge_ReOrder(1,-1)",ordermapping [ e. Fill. SiteOrderKey ].InstrumentDetails.Alias);
                                            string order_key=ts_publisher.Send_Orders_Series(op);
                                            if ( order_key != null )
                                                {
                                                hede_map. TryAdd ( order_key, Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ) );
                                                h_d. sellquote_hedge. TryAdd ( order_key, true );

                                                }
                                            else
                                                {
                                                TeamsPOST m_teamsPost = new TeamsPOST ( );
                                                Task. Run ( ( ) =>
                                                {
                                                    _ = m_teamsPost. SendMessageAndCsvFile ( webhookUrl_new, $"Unable to send hedge order for instrument {h_d. hedge_instrument} for quantity {hedge_qt} sell " );
                                                    ;

                                                } );
                                                }
                                            }
                                        }
                                    }
                                else
                                    {
                                    Hedge_Dets h_d =  new Hedge_Dets ( parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ], e. Fill. Instrument );
                                    legmapping. TryAdd ( Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ), h_d );
                                    if ( e. Fill. BuySell == BuySell. Buy )
                                        {
                                        int hedge_qt=h_d. Add_Buyhedge ( Convert. ToInt32 ( e. Fill. Quantity ),e.Fill.MatchPrice.ToDecimal() );

                                        if ( hedge_qt > 0 )
                                            {
                                            OrderProfile op =Send_Order(h_d.hedge_instrument,BuySell.Buy,priceUpdates[h_d.hedge_instrument].BidPrice,hedge_qt,OrderType.Limit,TimeInForce.GoodTillCancel,acc,"Hedge_ReOrder(1,-1)" ,ordermapping [ e. Fill. SiteOrderKey ].InstrumentDetails.Alias);
                                            string order_key=ts_publisher.Send_Orders_Series(op);
                                            if ( order_key != null )
                                                {
                                                hede_map. TryAdd ( order_key, Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ) );
                                                h_d. buyquote_hedge. TryAdd ( order_key, true );

                                                }
                                            else
                                                {
                                                TeamsPOST m_teamsPost = new TeamsPOST ( );
                                                Task. Run ( ( ) =>
                                                {
                                                    _ = m_teamsPost. SendMessageAndCsvFile ( webhookUrl_new, $"Unable to send hedge order for instrument {h_d. hedge_instrument} for quantity {hedge_qt} buy " );
                                                    ;

                                                } );

                                                }
                                            }
                                        else
                                            {
                                            OrderProfile op =Send_Order(h_d.hedge_instrument,BuySell.Sell,priceUpdates[h_d.hedge_instrument].AskPrice,hedge_qt*-1,OrderType.Limit,TimeInForce.GoodTillCancel,acc,"Hedge_ReOrder(1,-1)" ,ordermapping [ e. Fill. SiteOrderKey ].InstrumentDetails.Alias);
                                            string order_key=ts_publisher.Send_Orders_Series(op);
                                            if ( order_key != null )
                                                {
                                                hede_map. TryAdd ( order_key, Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ) );
                                                h_d. buyquote_hedge. TryAdd ( order_key, true );

                                                }
                                            else
                                                {
                                                TeamsPOST m_teamsPost = new TeamsPOST ( );
                                                Task. Run ( ( ) =>
                                                {
                                                    _ = m_teamsPost. SendMessageAndCsvFile ( webhookUrl_new, $"Unable to send hedge order for instrument {h_d. hedge_instrument} for quantity {hedge_qt * -1} sell" );
                                                    ;

                                                } );
                                                }
                                            }
                                        }
                                    else
                                        {
                                        int hedge_qt=h_d. Add_Sellhedge ( Convert. ToInt32 ( e. Fill. Quantity ) ,e.Fill.MatchPrice.ToDecimal());
                                        if ( hedge_qt < 0 )
                                            {
                                            OrderProfile op =Send_Order(h_d.hedge_instrument,BuySell.Buy,priceUpdates[h_d.hedge_instrument].BidPrice,hedge_qt*-1,OrderType.Limit,TimeInForce.GoodTillCancel,acc,"Hedge_ReOrder(1,-1)",ordermapping [ e. Fill. SiteOrderKey ].InstrumentDetails.Alias );
                                            string order_key=ts_publisher.Send_Orders_Series(op);
                                            if ( order_key != null )
                                                {
                                                hede_map. TryAdd ( order_key, Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ) );
                                                h_d. sellquote_hedge. TryAdd ( order_key, true );

                                                }
                                            else
                                                {
                                                TeamsPOST m_teamsPost = new TeamsPOST ( );
                                                Task. Run ( ( ) =>
                                                {
                                                    _ = m_teamsPost. SendMessageAndCsvFile ( webhookUrl_new, $"Unable to send hedge order for instrument {h_d. hedge_instrument} for quantity {hedge_qt * -1} buy" );
                                                    ;

                                                } );
                                                }
                                            }
                                        else if ( hedge_qt > 0 )
                                            {
                                            OrderProfile op =Send_Order(h_d.hedge_instrument,BuySell.Sell,priceUpdates[h_d.hedge_instrument].AskPrice,hedge_qt,OrderType.Limit,TimeInForce.GoodTillCancel,acc,"Hedge_ReOrder(1,-1)",ordermapping [ e. Fill. SiteOrderKey ].InstrumentDetails.Alias);
                                            string order_key=ts_publisher.Send_Orders_Series(op);
                                            if ( order_key != null )
                                                {
                                                hede_map. TryAdd ( order_key, Tuple. Create ( ordermapping [ e. Fill. SiteOrderKey ], e. Fill. Instrument ) );
                                                h_d. sellquote_hedge. TryAdd ( order_key, true );

                                                }
                                            else
                                                {
                                                TeamsPOST m_teamsPost = new TeamsPOST ( );
                                                Task. Run ( ( ) =>
                                                {
                                                    _ = m_teamsPost. SendMessageAndCsvFile ( webhookUrl_new, $"Unable to send hedge order for instrument {h_d. hedge_instrument} for quantity {hedge_qt} sell " );
                                                    ;

                                                } );
                                                }
                                            }
                                        }
                                    }

                                if ( e. Fill. BuySell == BuySell. Buy )
                                    {

                                    Tuple <Instrument,bool>temp=Tuple.Create(ordermapping [ e. Fill. SiteOrderKey ] ,true);
                                    if ( e. FillType == tt_net_sdk. FillType. Full )
                                        {
                                        if ( parent_inst_last_fill. ContainsKey ( temp ) )
                                            {
                                            if ( ( e. Fill. TransactionDateTime - parent_inst_last_fill [ temp ] ). TotalSeconds < 2 )
                                                {
                                                parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ]. isallowed_b = false;
                                                //delete all orders for that side for this deepfly
                                                DeleteAllLegOrders ( parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ], true );

                                                }
                                            parent_inst_last_fill [ temp ] = e. Fill. TransactionDateTime;
                                            }
                                        else
                                            {
                                            parent_inst_last_fill. TryAdd ( temp, e. Fill. TransactionDateTime );
                                            }
                                        }
                                    if ( total_fill_count. ContainsKey ( temp ) )
                                        {
                                        total_fill_count [ temp ] += e. Fill. Quantity. Value;
                                        if ( total_fill_count [ temp ] >= 60 )
                                            {
                                            parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ]. isallowed_b = false;
                                            //delete all orders for that side for this deepfly
                                            DeleteAllLegOrders ( parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ], true );
                                            }
                                        }
                                    else
                                        {
                                        total_fill_count. TryAdd ( temp, e. Fill. Quantity. Value );
                                        }
                                    }
                                else
                                    {

                                    Tuple <Instrument,bool>temp=Tuple.Create(ordermapping [ e. Fill. SiteOrderKey ] ,false);
                                    if ( e. FillType == tt_net_sdk. FillType. Full )
                                        {
                                        if ( parent_inst_last_fill. ContainsKey ( temp ) )
                                            {
                                            if ( ( e. Fill. TransactionDateTime - parent_inst_last_fill [ temp ] ). TotalSeconds < 2 )
                                                {
                                                parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ]. isallowed_s = false;
                                                //delete all orders for that side for this deepfly
                                                DeleteAllLegOrders ( parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ], true );

                                                }
                                            parent_inst_last_fill [ temp ] = e. Fill. TransactionDateTime;
                                            }
                                        else
                                            {
                                            parent_inst_last_fill. TryAdd ( temp, e. Fill. TransactionDateTime );
                                            }
                                        }
                                    if ( total_fill_count. ContainsKey ( temp ) )
                                        {
                                        total_fill_count [ temp ] += e. Fill. Quantity. Value;
                                        if ( total_fill_count [ temp ] >= 60 )
                                            {
                                            parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ]. isallowed_s = false;
                                            //delete all orders for that side for this deepfly
                                            DeleteAllLegOrders ( parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ], false );

                                            }
                                        }
                                    else
                                        {
                                        total_fill_count. TryAdd ( temp, e. Fill. Quantity. Value );
                                        }
                                    }
                                }
                            else if ( hede_map. ContainsKey ( e. Fill. SiteOrderKey ) && !e. Fill. IsExchangeSpreadLegFill )
                                {
                                Hedge_Dets h_d= legmapping [ hede_map [ e. Fill. SiteOrderKey ] ];
                                h_d. AddHedgeFill ( ( int ) e. Fill. Quantity. Value, e. Fill. MatchPrice. Value, e. Fill. SiteOrderKey );
                                }
                            if ( e. FillType == tt_net_sdk. FillType. Full )
                                {
                                if ( ordermapping. ContainsKey ( e. Fill. SiteOrderKey ) && !e. Fill. IsExchangeSpreadLegFill )
                                    {
                                    if ( parentInstrumentMap. ContainsKey ( ordermapping [ e. Fill. SiteOrderKey ] ) )
                                        {
                                        parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ]. order_keydel ( e. Fill. SiteOrderKey );
                                        if ( e. Fill. BuySell == BuySell. Buy )
                                            {
                                            parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ]. max_lot_b -= e. Fill. Quantity;
                                            }
                                        else
                                            {
                                            parentInstrumentMap [ ordermapping [ e. Fill. SiteOrderKey ] ]. max_lot_s -= e. Fill. Quantity;

                                            }
                                        }


                                    }

                                }

                            }
                        }
                    catch ( Exception ex )
                        {

                        }
                    }
                }

            }

        private void ProcessUpdate_Add ( Order update )
            {
            lock ( m_obj1 )
                {
                if ( update != null )
                    {
                    if ( DateTime. Now. Hour == 23 || DateTime. Now. Hour == 21 || DateTime. Now. Hour == 22 )
                        {
                        Logger. InformationAsync ( "\nOrderAdded [{0}] {1}: {2}  {3} , {4}", update. SiteOrderKey, update. BuySell, update. ToString ( ), update. Instrument. InstrumentDetails. Alias, DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );
                        }
                    Console. WriteLine ( "\nOrderAdded [{0}] {1}: {2}", update. SiteOrderKey, update. BuySell, update. ToString ( ) );
                    if ( ordermapping. ContainsKey ( update. SiteOrderKey ) )
                        {
                        if ( parentInstrumentMap. ContainsKey ( ordermapping [ update. SiteOrderKey ] ) )
                            {
                            CheckOrder ( update. BuySell == BuySell. Buy, update. SiteOrderKey, parentInstrumentMap [ ordermapping [ update. SiteOrderKey ] ] );
                            }
                        else
                            {
                            ts_publisher. delete_order ( update. SiteOrderKey );
                            }
                        }

                    }
                }
            }
        public void CheckOrder ( bool buy, string orderkey, TradingLegs td )
            {
            if ( buy )
                {
                if ( td. BestBidOrder != orderkey && td. SecondBestBidOrder != orderkey && td. ThirdBestBidOrder != orderkey && td. FourthBestBidOrder != orderkey )
                    {
                    ts_publisher. delete_order ( orderkey );
                    }
                }
            else
                {
                if ( td. BestAskOrder != orderkey && td. SecondBestAskOrder != orderkey && td. ThirdBestAskOrder != orderkey && td. FourthBestAskOrder != orderkey )
                    {
                    ts_publisher. delete_order ( orderkey );
                    }
                }
            }
        private void ProcessUpdate_update ( OrderUpdatedEventArgs update )
            {
            lock ( m_obj3 )
                {
                if ( update. NewOrder != null && update. OldOrder != null )
                    {
                    if ( DateTime. Now. Hour == 23 || DateTime. Now. Hour == 21 || DateTime. Now. Hour == 22 )
                        {
                        Logger. InformationAsync ( "\nOrderupdated [{0}] {1}: {2}  {3} , {4}", update. NewOrder. SiteOrderKey, update. NewOrder. BuySell, update. NewOrder. ToString ( ), update. NewOrder. Instrument. InstrumentDetails. Alias, DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );
                        }
                    }
                }
            }
        private void ProcessUpdate_delete ( OrderDeletedEventArgs e )
            {
            lock ( m_obj4 )
                {
                Order update = e.OldOrder;

                if ( update != null )
                    {
                    if ( DateTime. Now. Hour == 23 || DateTime. Now. Hour == 21 || DateTime. Now. Hour == 22 )
                        {
                        Logger. InformationAsync ( "\nOrderDeleted[{0}] {1}: {2}  {3} , {4}", update. SiteOrderKey, update. BuySell, update. ToString ( ), update. Instrument. InstrumentDetails. Alias, DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );
                        }
                    if ( ordermapping. ContainsKey ( update. SiteOrderKey ) && e. DeletedUpdate. OrderSource == OrderSource. DotnetApiClt )
                        {
                        ordermapping. TryRemove ( update. SiteOrderKey, out Instrument p_instr );
                        if ( p_instr != null )
                            {
                            if ( parentInstrumentMap. ContainsKey ( p_instr ) )
                                {

                                parentInstrumentMap [ p_instr ]. order_keydel ( update. SiteOrderKey );
                                if ( update. BuySell == BuySell. Buy )
                                    {
                                    parentInstrumentMap [ p_instr ]. max_lot_b -= update. OrderQuantity;
                                    }
                                else
                                    {
                                    parentInstrumentMap [ p_instr ]. max_lot_s -= update. OrderQuantity;

                                    }
                                }
                            }
                        }
                    else if ( ordermapping. ContainsKey ( update. SiteOrderKey ) && e. DeletedUpdate. OrderSource != OrderSource. DotnetApiClt )
                        {
                        StoreOrders ( parentInstrumentMap );
                        foreach ( var x in ordermapping )
                            {
                            ts_publisher. delete_order ( x. Key );

                            }
                        TeamsPOST m_teamsPost = new TeamsPOST ( );
                        Task. Run ( ( ) =>
                        {
                            _ = m_teamsPost. SendMessageAndCsvFile ( webhookUrl_new, $"Manual intervention detected " );
                            ;

                        } );

                        Thread. Sleep ( 1000 );
                        Environment. Exit ( 001 );

                        }
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
        public OrderProfile Send_Order ( Instrument instrument, BuySell buysell, Price price, Decimal quantity, OrderType orderType, tt_net_sdk. TimeInForce TIF
  , Account acc, string text, string text2 )
            {
            OrderProfile op = new OrderProfile(instrument)
                {
                BuySell = buysell,
                OrderType = orderType,
                TimeInForce = TIF,
                Account = acc,
                LimitPrice = price,
                OrderQuantity =Quantity.FromDecimal( instrument,quantity),
                TextA=text,
                TextB=text2
                };


            return op;
            }

        }
    }
