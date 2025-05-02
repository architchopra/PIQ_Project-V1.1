using System. Collections. Concurrent;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class Mapping_Prices
        {
        public ConcurrentDictionary<Instrument, TradingLegs> parentInstrumentMap = new ConcurrentDictionary<Instrument, TradingLegs>();
        public ConcurrentDictionary<Instrument, TradingLegs> allInstrumentMap = new ConcurrentDictionary<Instrument, TradingLegs>();
        public ConcurrentDictionary<string, Instrument> order_mapping = new ConcurrentDictionary<string, Instrument>();
        private readonly ConcurrentDictionary<Instrument, PriceUpdate> priceUpdates;
        Account acc=null;
        TS_Publisher ts_pub=null;
        DateTime buying_time=DateTime.Now;
        DateTime selling_time=DateTime.Now;
        object m_bo = new object();
        object m_bo_buy = new object();
        object m_bo_sell = new object();
        DateTime last_update_sent ;
        ConcurrentDictionary<string,OrderBookDets> Leg2_orders=new ConcurrentDictionary<string, OrderBookDets> ();
        public Mapping_Prices ( ConcurrentDictionary<Instrument, TradingLegs> Instr_map, ConcurrentDictionary<Instrument, TradingLegs> Instr_map1, ConcurrentDictionary<string, Instrument> order_mapping1, Account acc, TS_Publisher ts_publ, ConcurrentDictionary<Instrument, PriceUpdate> priceUpdates1, DateTime dt, ConcurrentDictionary<string, OrderBookDets> od1 )
            {
            this. parentInstrumentMap = Instr_map;
            this. allInstrumentMap = Instr_map1;
            this. acc = acc;
            this. ts_pub = ts_publ;
            this. order_mapping = order_mapping1;
            this. priceUpdates = priceUpdates1;
            this. last_update_sent = dt;
            this. Leg2_orders = od1;
            }
        public void Check_condition ( PriceUpdate PU )
            {
            priceUpdates. AddOrUpdate ( PU. Instr,
                     k => PU, // Factory function for creating new entry
                     ( k, existingValue ) =>
                     {
                         existingValue = PU;
                         return existingValue; // Return updated value
                     } );

            if ( allInstrumentMap. TryGetValue ( PU. Instr, out var tradingLegs ) )
                {
                ProcessConditions ( tradingLegs );
                }
            }
        private void ProcessConditions ( TradingLegs tradingLegs )
            {
            lock ( m_bo )
                {
                // Retrieve price updates for parent, leg1, and leg2
                var parentUpdate = GetPriceUpdate(tradingLegs.OrderLeg);
                var leg1Update = GetPriceUpdate(tradingLegs.Leg1);
                var leg2Update = GetPriceUpdate(tradingLegs.Leg2);

                // If any price update is missing, skip processing
                if ( parentUpdate. Instr == null || leg1Update. Instr == null || leg2Update. Instr == null )
                    return;

                Logger. DebugAsync ( "Price recieved for ,{0},{1}", tradingLegs. OrderLeg, DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );

                if ( Leg2_orders. TryGetValue ( leg2Update. Instr. InstrumentDetails. Alias, out OrderBookDets dets ) )
                    {

                    Price bestbid=leg2Update.BidPrice;
                    Price Secondbestbid=leg2Update.BidPrice-1;
                    Price Thirdbestbid=leg2Update.BidPrice-2;
                    Price Fourthbestbid=leg2Update.BidPrice-3;

                    Price bestask=leg2Update.AskPrice;
                    Price Secondbestask=leg2Update.AskPrice+1;
                    Price Thirdbestask=leg2Update.AskPrice +2;
                    Price Fourthbestask=leg2Update.AskPrice +3;


                    IDictionary<string,Order>orders_2= ts_pub. getLatestOrderlist ( );
                    foreach ( var d in dets. BuyOrders )
                        {
                        if ( d. Key != "" )
                            {
                            if ( orders_2. ContainsKey ( d. Key ) )
                                {
                                if ( orders_2 [ d. Key ]. LimitPrice. ToDecimal ( ) == bestbid. ToDecimal ( ) )
                                    {
                                    tradingLegs. BestBidOrder = d. Key;
                                    }

                                else if ( orders_2 [ d. Key ]. LimitPrice. ToDecimal ( ) == Secondbestbid. ToDecimal ( ) )
                                    {
                                    tradingLegs. SecondBestBidOrder = d. Key;
                                    }
                                else if ( orders_2 [ d. Key ]. LimitPrice. ToDecimal ( ) == Thirdbestbid. ToDecimal ( ) )
                                    {
                                    tradingLegs. ThirdBestBidOrder = d. Key;
                                    }
                                else if ( orders_2 [ d. Key ]. LimitPrice. ToDecimal ( ) == Fourthbestbid. ToDecimal ( ) )
                                    {
                                    tradingLegs. FourthBestBidOrder = d. Key;
                                    }
                                else
                                    {
                                    ts_pub. delete_order ( d. Key );
                                    }
                                dets. BuyOrders. TryRemove ( d. Key, out Price p );
                                }
                            }
                        }
                    if ( tradingLegs. BestBidOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. BestBidOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_b += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    if ( tradingLegs. SecondBestBidOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. SecondBestBidOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_b += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    if ( tradingLegs. ThirdBestBidOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. ThirdBestBidOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_b += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    if ( tradingLegs. FourthBestBidOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. FourthBestBidOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_b += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    foreach ( var d in dets. SellOrders )
                        {
                        if ( d. Key != "" )
                            {
                            if ( orders_2. ContainsKey ( d. Key ) )
                                {
                                if ( orders_2 [ d. Key ]. LimitPrice. ToDecimal ( ) == bestask. ToDecimal ( ) )
                                    {
                                    tradingLegs. BestAskOrder = d. Key;
                                    }

                                else if ( orders_2 [ d. Key ]. LimitPrice. ToDecimal ( ) == Secondbestask. ToDecimal ( ) )
                                    {
                                    tradingLegs. SecondBestAskOrder = d. Key;
                                    }
                                else if ( orders_2 [ d. Key ]. LimitPrice. ToDecimal ( ) == Thirdbestask. ToDecimal ( ) )
                                    {
                                    tradingLegs. ThirdBestAskOrder = d. Key;
                                    }
                                else if ( orders_2 [ d. Key ]. LimitPrice. ToDecimal ( ) == Fourthbestask. ToDecimal ( ) )
                                    {
                                    tradingLegs. FourthBestAskOrder = d. Key;
                                    }
                                else
                                    {
                                    ts_pub. delete_order ( d. Key );
                                    }
                                dets. SellOrders. TryRemove ( d. Key, out Price p );
                                }
                            }
                        }
                    if ( tradingLegs. BestAskOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. BestAskOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_s += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    if ( tradingLegs. SecondBestAskOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. SecondBestAskOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_s += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    if ( tradingLegs. ThirdBestAskOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. ThirdBestAskOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_s += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    if ( tradingLegs. FourthBestAskOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. FourthBestAskOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_s += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    Leg2_orders. TryRemove ( leg2Update. Instr. InstrumentDetails. Alias, out OrderBookDets od111 );
                    }
                // Efficiently evaluate conditions
                if ( tradingLegs. isallowed_s )
                    {
                    EvaluateBuyConditions ( parentUpdate, leg1Update, leg2Update, tradingLegs );
                    }
                if ( tradingLegs. isallowed_b )
                    {
                    EvaluateSellConditions ( parentUpdate, leg1Update, leg2Update, tradingLegs );
                    }
                }
            }
        private void EvaluateBuyConditions ( PriceUpdate parentPU, PriceUpdate leg1PU, PriceUpdate leg2PU, TradingLegs tradingLegs )
            {
            lock ( m_bo_buy )
                {
                decimal buy_pr = 0;
                if ( parentPU. BidQty. ToDecimal ( ) / parentPU. AskQty. ToDecimal ( ) > tradingLegs. ratio || parentPU. AskPrice. ToTicks ( ) - parentPU. BidPrice. ToTicks ( ) > 1 )
                    {
                    buy_pr = ( parentPU. BidPrice ). ToDecimal ( );
                    }
                else
                    {
                    buy_pr = ( parentPU. BidPrice - 1 ). ToDecimal ( );
                    }

                if ( tradingLegs. bestask != Price. Empty )
                    {
                    if ( leg2PU. AskPrice. ToDecimal ( ) != tradingLegs. bestask. ToDecimal ( ) )
                        {
                        if ( leg2PU. AskPrice. ToDecimal ( ) > tradingLegs. bestask. ToDecimal ( ) )
                            {

                            tradingLegs. BestAskOrder = tradingLegs. SecondBestAskOrder;
                            tradingLegs. SecondBestAskOrder = tradingLegs. ThirdBestAskOrder;
                            tradingLegs. ThirdBestAskOrder = tradingLegs. FourthBestAskOrder;
                            tradingLegs. FourthBestAskOrder = string. Empty;
                            Console. WriteLine ( "buying ,{0},{1},{2}", tradingLegs. FourthBestAskOrder, tradingLegs. FourthBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "buying,{0},{1},{2}", tradingLegs. ThirdBestAskOrder, tradingLegs. ThirdBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "buying,{0},{1},{2}", tradingLegs. SecondBestAskOrder, tradingLegs. SecondBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "buying,{0},{1},{2}", tradingLegs. BestAskOrder, tradingLegs. BestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "buying s{0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );

                            buying_time = DateTime. Now;

                            }
                        else
                            {
                            if ( tradingLegs. FourthBestAskOrder != string. Empty )
                                {
                                ts_pub. delete_order ( tradingLegs. FourthBestAskOrder );


                                }
                            tradingLegs. FourthBestAskOrder = tradingLegs. ThirdBestAskOrder;
                            tradingLegs. ThirdBestAskOrder = tradingLegs. SecondBestAskOrder;
                            tradingLegs. SecondBestAskOrder = tradingLegs. BestAskOrder;
                            tradingLegs. BestAskOrder = string. Empty;

                            Console. WriteLine ( "selling ,{0},{1},{2}", tradingLegs. FourthBestAskOrder, tradingLegs. FourthBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "selling,{0},{1},{2}", tradingLegs. ThirdBestAskOrder, tradingLegs. ThirdBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "selling,{0},{1},{2}", tradingLegs. SecondBestAskOrder, tradingLegs. SecondBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "selling,{0},{1},{2}", tradingLegs. BestAskOrder, tradingLegs. BestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "selling s{0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );
                            selling_time = DateTime. Now;

                            }
                        }
                    }
                tradingLegs. bestask = leg2PU. AskPrice;
                // Condition 1
                bool allowed=( DateTime. Now - buying_time ). TotalSeconds > 2 && ( DateTime. Now - selling_time ). TotalSeconds > 2;
                if ( ( tradingLegs. multiplier1 * leg1PU. AskPrice. ToDecimal ( ) + tradingLegs. multiplier2 * leg2PU. AskPrice. ToDecimal ( ) ) <= buy_pr )
                    {
                    string s1=null;
                    string s2=null;
                    string s3=null;
                    string s4=null;
                    if ( tradingLegs. BestAskOrder == string. Empty && allowed )
                        {
                        s1 = "best ask";
                        tradingLegs. BestAskOrder = "onway";
                        }
                    if ( tradingLegs. SecondBestAskOrder == string. Empty && allowed )
                        {
                        s2 = "best ask + 1";
                        tradingLegs. SecondBestAskOrder = "onway";
                        }
                    if ( tradingLegs. ThirdBestAskOrder == string. Empty && allowed )
                        {
                        s3 = "best ask+2";
                        tradingLegs. ThirdBestAskOrder = "onway";

                        }
                    if ( tradingLegs. FourthBestAskOrder == string. Empty && allowed )
                        {
                        s4 = "best ask+3";
                        Console. WriteLine ( "Adding sell order for {0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );

                        tradingLegs. FourthBestAskOrder = "onway";

                        }
                    if ( s1 != null || s2 != null || s3 != null || s4 != null )
                        {
                        PlaceSellOrders ( tradingLegs, leg2PU, s1, s2, s3, s4 );
                        }

                    }

                // Condition 2
                else if ( ( tradingLegs. multiplier1 * leg1PU. AskPrice. ToDecimal ( ) + tradingLegs. multiplier2 * ( leg2PU. AskPrice + 1 ). ToDecimal ( ) ) <= buy_pr )
                    {
                    string s2=null;
                    string s3=null;
                    string s4=null;
                    if ( tradingLegs. BestAskOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. BestAskOrder );


                        }

                    if ( tradingLegs. SecondBestAskOrder == string. Empty && allowed )
                        {
                        s2 = "best ask + 1";
                        tradingLegs. SecondBestAskOrder = "onway";

                        }
                    if ( tradingLegs. ThirdBestAskOrder == string. Empty && allowed )
                        {
                        s3 = "best ask+2";
                        tradingLegs. ThirdBestAskOrder = "onway";

                        }
                    if ( tradingLegs. FourthBestAskOrder == string. Empty && allowed )
                        {
                        s4 = "best ask+3";
                        Console. WriteLine ( "Adding sell order for {0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );

                        tradingLegs. FourthBestAskOrder = "onway";

                        }
                    if ( s2 != null || s3 != null || s4 != null )
                        {
                        PlaceSellOrders ( tradingLegs, leg2PU, null, s2, s3, s4 );
                        }

                    }

                // Condition 3
                else if ( ( tradingLegs. multiplier1 * leg1PU. AskPrice. ToDecimal ( ) + tradingLegs. multiplier2 * ( leg2PU. AskPrice + 2 ). ToDecimal ( ) ) <= buy_pr )
                    {
                    string s3=null;
                    string s4=null;
                    if ( tradingLegs. BestAskOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. BestAskOrder );

                        }
                    if ( tradingLegs. SecondBestAskOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. SecondBestAskOrder );

                        }
                    if ( tradingLegs. ThirdBestAskOrder == string. Empty && allowed )
                        {
                        s3 = "best ask+2";
                        tradingLegs. ThirdBestAskOrder = "onway";
                        }
                    if ( tradingLegs. FourthBestAskOrder == string. Empty && allowed )
                        {
                        s4 = "best ask+3";
                        Console. WriteLine ( "Adding sell order for {0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );

                        tradingLegs. FourthBestAskOrder = "onway";

                        }
                    if ( s3 != null || s4 != null )
                        {
                        PlaceSellOrders ( tradingLegs, leg2PU, null, null, s3, s4 );
                        }
                    }
                else if ( ( tradingLegs. multiplier1 * leg1PU. AskPrice. ToDecimal ( ) + tradingLegs. multiplier2 * ( leg2PU. AskPrice + 3 ). ToDecimal ( ) ) <= buy_pr )
                    {
                    if ( tradingLegs. BestAskOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. BestAskOrder );

                        }
                    if ( tradingLegs. SecondBestAskOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. SecondBestAskOrder );

                        }
                    if ( tradingLegs. ThirdBestAskOrder == string. Empty && allowed )
                        {
                        ts_pub. delete_order ( tradingLegs. ThirdBestAskOrder );
                        }
                    if ( tradingLegs. FourthBestAskOrder == string. Empty && allowed )
                        {
                        string s4 = "best ask+3";
                        tradingLegs. FourthBestAskOrder = "onway";
                        Console. WriteLine ( "Adding sell order for {0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );

                        PlaceSellOrders ( tradingLegs, leg2PU, null, null, null, s4 );
                        }

                    }
                else
                    {
                    if ( tradingLegs. BestAskOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. BestAskOrder );

                        }
                    if ( tradingLegs. SecondBestAskOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. SecondBestAskOrder );

                        }
                    if ( tradingLegs. ThirdBestAskOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. ThirdBestAskOrder );

                        }
                    if ( tradingLegs. FourthBestAskOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. FourthBestAskOrder );

                        }
                    }
                }
            }
        private void EvaluateSellConditions ( PriceUpdate parentPU, PriceUpdate leg1PU, PriceUpdate leg2PU, TradingLegs tradingLegs )
            {
            lock ( m_bo_sell )
                {
                decimal sell_pr = 0;
                if ( parentPU. AskQty. ToDecimal ( ) / parentPU. BidQty. ToDecimal ( ) > tradingLegs. ratio || parentPU. AskPrice. ToTicks ( ) - parentPU. BidPrice. ToTicks ( ) > 1 )
                    {
                    sell_pr = parentPU. AskPrice. ToDecimal ( );
                    }
                else
                    {
                    sell_pr = ( parentPU. AskPrice + 1 ). ToDecimal ( );
                    }
                if ( tradingLegs. bestbid != Price. Empty )
                    {
                    if ( leg2PU. BidPrice. ToDecimal ( ) != tradingLegs. bestbid. ToDecimal ( ) )
                        {
                        if ( leg2PU. BidPrice. ToDecimal ( ) > tradingLegs. bestbid. ToDecimal ( ) )
                            {
                            if ( tradingLegs. FourthBestBidOrder != string. Empty )
                                {
                                ts_pub. delete_order ( tradingLegs. FourthBestBidOrder );

                                }
                            tradingLegs. FourthBestBidOrder = tradingLegs. ThirdBestBidOrder;
                            tradingLegs. ThirdBestBidOrder = tradingLegs. SecondBestBidOrder;
                            tradingLegs. SecondBestBidOrder = tradingLegs. BestBidOrder;
                            tradingLegs. BestBidOrder = string. Empty;
                            Console. WriteLine ( "buying ,{0},{1},{2}", tradingLegs. FourthBestAskOrder, tradingLegs. FourthBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "buying,{0},{1},{2}", tradingLegs. ThirdBestAskOrder, tradingLegs. ThirdBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "buying,{0},{1},{2}", tradingLegs. SecondBestAskOrder, tradingLegs. SecondBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "buying,{0},{1},{2}", tradingLegs. BestAskOrder, tradingLegs. BestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "buying b{0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );

                            buying_time = DateTime. Now;

                            }
                        else
                            {

                            tradingLegs. BestBidOrder = tradingLegs. SecondBestBidOrder;
                            tradingLegs. SecondBestBidOrder = tradingLegs. ThirdBestBidOrder;
                            tradingLegs. ThirdBestBidOrder = tradingLegs. FourthBestBidOrder;
                            tradingLegs. FourthBestBidOrder = string. Empty;
                            Console. WriteLine ( "selling ,{0},{1},{2}", tradingLegs. FourthBestAskOrder, tradingLegs. FourthBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "selling,{0},{1},{2}", tradingLegs. ThirdBestAskOrder, tradingLegs. ThirdBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "selling,{0},{1},{2}", tradingLegs. SecondBestAskOrder, tradingLegs. SecondBestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "selling,{0},{1},{2}", tradingLegs. BestAskOrder, tradingLegs. BestBidOrder, tradingLegs. OrderLeg. InstrumentDetails. Alias );
                            Console. WriteLine ( "selling b{0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );
                            selling_time = DateTime. Now;

                            }
                        }
                    }
                tradingLegs. bestbid = leg2PU. BidPrice;
                bool allowed=( DateTime. Now - buying_time ). TotalSeconds > 2 && ( DateTime. Now - selling_time ). TotalSeconds > 2;

                // Condition 1
                if ( ( tradingLegs. multiplier1 * leg1PU. BidPrice. ToDecimal ( ) + tradingLegs. multiplier2 * leg2PU. BidPrice. ToDecimal ( ) ) >= sell_pr )
                    {
                    string s1=null;
                    string s2=null;
                    string s3=null;
                    string s4=null;
                    if ( tradingLegs. BestBidOrder == string. Empty && allowed )
                        {
                        s1 = "best bid";
                        tradingLegs. BestBidOrder = "onway";

                        }
                    if ( tradingLegs. SecondBestBidOrder == string. Empty && allowed )
                        {
                        s2 = "best bid-1";
                        tradingLegs. SecondBestBidOrder = "onway";

                        }
                    if ( tradingLegs. ThirdBestBidOrder == string. Empty && allowed )
                        {
                        s3 = "best bid-2";
                        tradingLegs. ThirdBestBidOrder = "onway";

                        }
                    if ( tradingLegs. FourthBestBidOrder == string. Empty && allowed )
                        {
                        s4 = "best bid-3";
                        tradingLegs. FourthBestBidOrder = "onway";

                        }
                    if ( ( s1 != null || s2 != null || s3 != null || s4 != null ) )
                        {
                        PlaceBuyOrders ( tradingLegs, leg2PU, s1, s2, s3, s4 );
                        }
                    }

                // Condition 2
                else if ( ( tradingLegs. multiplier1 * leg1PU. BidPrice. ToDecimal ( ) + tradingLegs. multiplier2 * ( leg2PU. BidPrice - 1 ). ToDecimal ( ) ) >= sell_pr )
                    {
                    string s2=null;
                    string s3=null;
                    string s4=null;
                    if ( tradingLegs. BestBidOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. BestBidOrder );

                        }

                    if ( tradingLegs. SecondBestBidOrder == string. Empty && allowed )
                        {
                        s2 = "best bid - 1";
                        tradingLegs. SecondBestBidOrder = "onway";

                        }
                    if ( tradingLegs. ThirdBestBidOrder == string. Empty && allowed )
                        {
                        s3 = "best bid - 2";
                        tradingLegs. ThirdBestBidOrder = "onway";

                        }
                    if ( tradingLegs. FourthBestBidOrder == string. Empty && allowed )
                        {
                        s4 = "best bid - 2";
                        tradingLegs. FourthBestBidOrder = "onway";

                        }

                    if ( ( s2 != null || s3 != null || s4 != null ) )
                        {
                        PlaceBuyOrders ( tradingLegs, leg2PU, null, s2, s3, s4 );
                        }
                    }

                // Condition 3
                else if ( ( tradingLegs. multiplier1 * leg1PU. BidPrice. ToDecimal ( ) + tradingLegs. multiplier2 * ( leg2PU. BidPrice - 2 ). ToDecimal ( ) ) >= sell_pr )
                    {
                    string s3=null;
                    string s4=null;
                    if ( tradingLegs. BestBidOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. BestBidOrder );


                        }

                    if ( tradingLegs. SecondBestBidOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. SecondBestBidOrder );


                        }

                    if ( tradingLegs. ThirdBestBidOrder == string. Empty && allowed )
                        {
                        s3 = "best bid-2";
                        tradingLegs. ThirdBestBidOrder = "onway";


                        }
                    if ( tradingLegs. FourthBestBidOrder == string. Empty && allowed )
                        {
                        s4 = "best bid-3";
                        tradingLegs. FourthBestBidOrder = "onway";


                        }
                    if ( ( s3 != null || s4 != null ) )
                        {
                        PlaceBuyOrders ( tradingLegs, leg2PU, null, null, s3, s4 );
                        }

                    }
                else if ( ( tradingLegs. multiplier1 * leg1PU. BidPrice. ToDecimal ( ) + tradingLegs. multiplier2 * ( leg2PU. BidPrice - 3 ). ToDecimal ( ) ) >= sell_pr )
                    {

                    string s4=null;
                    if ( tradingLegs. BestBidOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. BestBidOrder );


                        }

                    if ( tradingLegs. SecondBestBidOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. SecondBestBidOrder );


                        }

                    if ( tradingLegs. ThirdBestBidOrder == string. Empty && allowed )
                        {

                        ts_pub. delete_order ( tradingLegs. ThirdBestBidOrder );


                        }
                    if ( tradingLegs. FourthBestBidOrder == string. Empty && allowed )
                        {
                        s4 = "best bid-3";
                        tradingLegs. FourthBestBidOrder = "onway";


                        }
                    if ( s4 != null )
                        {
                        PlaceBuyOrders ( tradingLegs, leg2PU, null, null, null, s4 );
                        }

                    }
                else
                    {
                    if ( tradingLegs. BestBidOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. BestBidOrder );

                        }
                    if ( tradingLegs. SecondBestBidOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. SecondBestBidOrder );

                        }
                    if ( tradingLegs. ThirdBestBidOrder != string. Empty )
                        {
                        ts_pub. delete_order ( tradingLegs. ThirdBestBidOrder );

                        }
                    }
                }
            }
        private PriceUpdate GetPriceUpdate ( Instrument instr )
            {
            return priceUpdates. TryGetValue ( instr, out var update ) ? update : default;
            }
        public void DeleteAllLegOrders ( TradingLegs td, bool buy )
            {
            if ( buy )
                {
                if ( td. BestBidOrder != string. Empty )
                    {
                    ts_pub. delete_order ( td. BestBidOrder );

                    }
                if ( td. SecondBestBidOrder != string. Empty )
                    {
                    ts_pub. delete_order ( td. SecondBestBidOrder );

                    }
                if ( td. ThirdBestBidOrder != string. Empty )
                    {
                    ts_pub. delete_order ( td. ThirdBestBidOrder );

                    }
                if ( td. FourthBestBidOrder != string. Empty )
                    {
                    ts_pub. delete_order ( td. FourthBestBidOrder );

                    }
                }
            else
                {
                if ( td. BestAskOrder != string. Empty )
                    {
                    ts_pub. delete_order ( td. BestAskOrder );

                    }
                if ( td. SecondBestAskOrder != string. Empty )
                    {
                    ts_pub. delete_order ( td. SecondBestAskOrder );

                    }
                if ( td. ThirdBestAskOrder != string. Empty )
                    {
                    ts_pub. delete_order ( td. ThirdBestAskOrder );

                    }
                if ( td. FourthBestAskOrder != string. Empty )
                    {
                    ts_pub. delete_order ( td. FourthBestAskOrder );

                    }
                }
            }
        private void PlaceSellOrders ( TradingLegs tradingLegs, PriceUpdate PU, string order1, string order2, string order3, string order4 )
            {
            DateTime currentTime = DateTime.Now;
            DateTime startTime = DateTime.Today.Add(new TimeSpan(21, 48, 0)); // 9:30 PM today
            DateTime endTime = DateTime.Today.Add(new TimeSpan(23, 13, 0));   // 11:30 PM today
            Console. WriteLine ( "Adding sell order for2 {0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );
            if ( !string. IsNullOrEmpty ( order1 ) )
                {
                if ( tradingLegs. max_lot_s + Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 ) <= tradingLegs. max_lot_allowed && !( currentTime >= startTime && currentTime <= endTime ) )
                    {
                    OrderProfile op=Send_Order(PU.Instr,BuySell.Sell,PU.AskPrice,Math.Abs(tradingLegs.order_qty*tradingLegs.multiplier2),OrderType.Limit,TimeInForce.GoodTillCancel,acc, tradingLegs.OrderLeg.InstrumentDetails.Alias);

                    tradingLegs. BestAskOrder = ts_pub. Send_Orders_Series ( op );
                    last_update_sent = currentTime;
                    if ( tradingLegs. BestAskOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. BestAskOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_s += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }

                    }
                else
                    {
                    if ( tradingLegs. BestAskOrder == "onway" )
                        {
                        tradingLegs. BestAskOrder = string. Empty;
                        }
                    }
                }
            if ( !string. IsNullOrEmpty ( order2 ) )
                {
                if ( tradingLegs. max_lot_s + Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 ) <= tradingLegs. max_lot_allowed && !( currentTime >= startTime && currentTime <= endTime ) )
                    {
                    OrderProfile op1=Send_Order(PU.Instr,BuySell.Sell,PU.AskPrice+1,Math.Abs(tradingLegs.order_qty*tradingLegs.multiplier2),OrderType.Limit,TimeInForce.GoodTillCancel,acc,tradingLegs. OrderLeg.InstrumentDetails.Alias);
                    tradingLegs. SecondBestAskOrder = ts_pub. Send_Orders_Series ( op1 );
                    if ( tradingLegs. SecondBestAskOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. SecondBestAskOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_s += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    last_update_sent = currentTime;

                    }
                else
                    {
                    if ( tradingLegs. SecondBestAskOrder == "onway" )
                        {
                        tradingLegs. SecondBestAskOrder = string. Empty;
                        }
                    }

                }

            if ( !string. IsNullOrEmpty ( order3 ) )
                {
                if ( tradingLegs. max_lot_s + Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 ) <= tradingLegs. max_lot_allowed && !( currentTime >= startTime && currentTime <= endTime ) )
                    {
                    OrderProfile op2=Send_Order(PU.Instr,BuySell.Sell,PU.AskPrice+2,Math.Abs(tradingLegs.order_qty*tradingLegs.multiplier2),OrderType.Limit,TimeInForce.GoodTillCancel,acc, tradingLegs.OrderLeg.InstrumentDetails.Alias);
                    tradingLegs. ThirdBestAskOrder = ts_pub. Send_Orders_Series ( op2 );
                    if ( tradingLegs. ThirdBestAskOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. ThirdBestAskOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_s += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    last_update_sent = currentTime;

                    }
                else
                    {
                    if ( tradingLegs. ThirdBestAskOrder == "onway" )
                        {
                        tradingLegs. ThirdBestAskOrder = string. Empty;
                        }
                    }

                }
            if ( !string. IsNullOrEmpty ( order4 ) )
                {
                if ( tradingLegs. max_lot_s + Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 ) <= tradingLegs. max_lot_allowed && !( currentTime >= startTime && currentTime <= endTime ) )
                    {
                    Console. WriteLine ( "Adding sell order for 3{0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );

                    OrderProfile op3=Send_Order(PU.Instr,BuySell.Sell,PU.AskPrice+3,Math.Abs(tradingLegs.order_qty*tradingLegs.multiplier2),OrderType.Limit,TimeInForce.GoodTillCancel,acc, tradingLegs.OrderLeg.InstrumentDetails.Alias);
                    Console. WriteLine ( "Adding sell order for 4{0}", DateTime. Now. ToString ( "yyyy-MM-dd HH:mm:ss.fff" ) );

                    tradingLegs. FourthBestAskOrder = ts_pub. Send_Orders_Series ( op3 );
                    if ( tradingLegs. FourthBestAskOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. FourthBestAskOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_s += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }
                    last_update_sent = currentTime;

                    }
                else
                    {
                    if ( tradingLegs. FourthBestAskOrder == "onway" )
                        {
                        tradingLegs. FourthBestAskOrder = string. Empty;
                        }
                    }

                }
            }


        private void PlaceBuyOrders ( TradingLegs tradingLegs, PriceUpdate PU, string order1, string order2, string order3, string order4 )
            {
            DateTime currentTime = DateTime.Now;
            DateTime startTime = DateTime.Today.Add(new TimeSpan(21, 48, 0)); // 9:30 PM today
            DateTime endTime = DateTime.Today.Add(new TimeSpan(23, 13, 0));   // 11:30 PM today
            if ( !string. IsNullOrEmpty ( order1 ) )
                {
                if ( tradingLegs. max_lot_b + Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 ) <= tradingLegs. max_lot_allowed && !( currentTime >= startTime && currentTime <= endTime ) )
                    {
                    OrderProfile op=Send_Order(PU.Instr,BuySell.Buy,PU.BidPrice,Math.Abs(tradingLegs.order_qty*tradingLegs.multiplier2),OrderType.Limit,TimeInForce.GoodTillCancel,acc, tradingLegs.OrderLeg.InstrumentDetails.Alias);
                    tradingLegs. BestBidOrder = ts_pub. Send_Orders_Series ( op );
                    last_update_sent = currentTime;

                    if ( tradingLegs. BestBidOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. BestBidOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_b += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }

                    }
                else
                    {
                    if ( tradingLegs. BestBidOrder == "onway" )
                        {
                        tradingLegs. BestBidOrder = string. Empty;
                        }
                    }

                }
            if ( !string. IsNullOrEmpty ( order2 ) )
                {
                if ( tradingLegs. max_lot_b + Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 ) <= tradingLegs. max_lot_allowed && !( currentTime >= startTime && currentTime <= endTime ) )
                    {
                    OrderProfile op1=Send_Order(PU.Instr,BuySell.Buy,PU.BidPrice-1,Math.Abs(tradingLegs.order_qty*tradingLegs.multiplier2),OrderType.Limit,TimeInForce.GoodTillCancel,acc, tradingLegs.OrderLeg.InstrumentDetails.Alias);
                    tradingLegs. SecondBestBidOrder = ts_pub. Send_Orders_Series ( op1 );
                    last_update_sent = currentTime;

                    if ( tradingLegs. SecondBestBidOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. SecondBestBidOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_b += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }

                    }
                else
                    {
                    if ( tradingLegs. SecondBestBidOrder == "onway" )
                        {
                        tradingLegs. SecondBestBidOrder = string. Empty;
                        }
                    }

                }


            if ( !string. IsNullOrEmpty ( order3 ) )
                {
                if ( tradingLegs. max_lot_b + Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 ) <= tradingLegs. max_lot_allowed && !( currentTime >= startTime && currentTime <= endTime ) )
                    {
                    OrderProfile op2=Send_Order(PU.Instr,BuySell.Buy,PU.BidPrice-2,Math.Abs(tradingLegs.order_qty*tradingLegs.multiplier2),OrderType.Limit,TimeInForce.GoodTillCancel,acc, tradingLegs.OrderLeg.InstrumentDetails.Alias);
                    tradingLegs. ThirdBestBidOrder = ts_pub. Send_Orders_Series ( op2 );
                    last_update_sent = currentTime;

                    if ( tradingLegs. ThirdBestBidOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. ThirdBestBidOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_b += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }

                    }
                else
                    {
                    if ( tradingLegs. ThirdBestBidOrder == "onway" )
                        {
                        tradingLegs. ThirdBestBidOrder = string. Empty;
                        }
                    }

                }
            if ( !string. IsNullOrEmpty ( order4 ) )
                {
                if ( tradingLegs. max_lot_b + Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 ) <= tradingLegs. max_lot_allowed && !( currentTime >= startTime && currentTime <= endTime ) )
                    {
                    OrderProfile op3=Send_Order(PU.Instr,BuySell.Buy,PU.BidPrice-3,Math.Abs(tradingLegs.order_qty*tradingLegs.multiplier2),OrderType.Limit,TimeInForce.GoodTillCancel,acc,tradingLegs. OrderLeg.InstrumentDetails.Alias);
                    tradingLegs. FourthBestBidOrder = ts_pub. Send_Orders_Series ( op3 );
                    last_update_sent = currentTime;

                    if ( tradingLegs. FourthBestBidOrder != string. Empty )
                        {
                        order_mapping. TryAdd ( tradingLegs. FourthBestBidOrder, tradingLegs. OrderLeg );
                        tradingLegs. max_lot_b += Math. Abs ( tradingLegs. order_qty * tradingLegs. multiplier2 );
                        }

                    }
                else
                    {
                    if ( tradingLegs. FourthBestBidOrder == "onway" )
                        {
                        tradingLegs. FourthBestBidOrder = string. Empty;
                        }
                    }

                }
            }

        public OrderProfile Send_Order ( Instrument instrument, BuySell buysell, Price price, Decimal quantity, OrderType orderType, tt_net_sdk. TimeInForce TIF
     , Account acc, string text )
            {
            OrderProfile op = new OrderProfile(instrument)
                {
                BuySell = buysell,
                OrderType = orderType,
                TimeInForce = TIF,
                Account = acc,
                LimitPrice = price,
                OrderQuantity =Quantity.FromDecimal( instrument,quantity),
                TextB=text
                };

            return op;
            }



        }
    }
