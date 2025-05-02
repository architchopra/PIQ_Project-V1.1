using System. Collections. Concurrent;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class InstrumentManager
        {
        private ConcurrentDictionary<Instrument, TradingLegs> parentinstrumentMap = new ConcurrentDictionary<Instrument, TradingLegs>();
        private ConcurrentDictionary<Instrument, TradingLegs> allinstrumentMap = new ConcurrentDictionary<Instrument, TradingLegs>();
        private InstrumentLookup? m_instrLookupRequest=null;
        PriceUpdateService priceupdate_service;
        Dispatcher disp=null;

        public InstrumentManager ( ConcurrentDictionary<Instrument, TradingLegs> instrumentMapping, ConcurrentDictionary<Instrument, TradingLegs> instrumentMappingall, PriceUpdateService update_service, Dispatcher disp2 )
            {
            this. parentinstrumentMap = instrumentMapping;
            this. allinstrumentMap = instrumentMappingall;
            this. priceupdate_service = update_service;
            this. disp = disp2;
            lookupandStoreInstr ( );
            }
        public void lookupandStoreInstr2 ( )
            {
            m_instrLookupRequest = new InstrumentLookup ( disp,
                       MarketId. CME, ProductType. Future, "SR3", "SR3 Dec25" );

            m_instrLookupRequest. OnData += m_instrLookupRequest_OnData2;
            m_instrLookupRequest. GetAsync ( );
            }
        public void lookupandStoreInstr ( )
            {
            m_instrLookupRequest = new InstrumentLookup ( disp,
                       MarketId. CME, ProductType. Future, "SR3", "SR3 Dec25" );

            m_instrLookupRequest. OnData += m_instrLookupRequest_OnData;
            m_instrLookupRequest. GetAsync ( );
            }
        void m_instrLookupRequest_OnData ( object sender, InstrumentLookupEventArgs e )
            {
            if ( e. Event == ProductDataEvent. Found )
                {
                // Instrument was found
                Instrument instrument = e.InstrumentLookup.Instrument;
                Console. WriteLine ( "Found: {0}", instrument );

                // Subscribe for market Data

                priceupdate_service. Subscribe2 ( instrument );


                }
            else if ( e. Event == ProductDataEvent. NotAllowed )
                {
                Console. WriteLine ( "Not Allowed : Please check your Token access" );
                }
            else
                {
                // Instrument was not found and TT API has given up looking for it
                Console. WriteLine ( "Cannot find instrument: {0}", e. Message );

                }
            }
        private async void m_instrLookupRequest_OnData2 ( object sender, InstrumentLookupEventArgs e )
            {
            if ( e. Event == ProductDataEvent. Found )
                {
                // Instrument was found
                Instrument instrument = e.InstrumentLookup.Instrument;
                Console. WriteLine ( "Found: {0}", instrument );

                priceupdate_service. Unsubscribe2 ( instrument );
                // Subscribe for market Data
                await Task. Delay ( 2000 );
                priceupdate_service. Subscribe2 ( instrument );


                }
            else if ( e. Event == ProductDataEvent. NotAllowed )
                {
                Console. WriteLine ( "Not Allowed : Please check your Token access" );
                }
            else
                {
                // Instrument was not found and TT API has given up looking for it
                Console. WriteLine ( "Cannot find instrument: {0}", e. Message );

                }
            }
        public void LookupAndStoreInstruments ( string parentSymbol, string leg1Symbol, string leg2Symbol, string product1, string product2, string product3, decimal mult1, decimal mult2, decimal ratio, decimal qt )
            {
            // Lookup Parent Instrument (i1)
            m_instrLookupRequest = new InstrumentLookup ( disp, MarketId. CME, ProductType. MultilegInstrument, product1, parentSymbol );
            m_instrLookupRequest. OnData += ( sender, args ) =>
            {
                if ( args. InstrumentLookup. Instrument != null )
                    {
                    Instrument parentInstrument = args.InstrumentLookup.Instrument;
                    LookupLegs ( parentInstrument, leg1Symbol, leg2Symbol, product2, product3, mult1, mult2, ratio, qt );
                    }
            };
            m_instrLookupRequest. GetAsync ( );
            }
        public void LookupAndStoreInstruments2 ( string parentSymbol, string leg1Symbol, string leg2Symbol, string product1, string product2, string product3 )
            {
            // Lookup Parent Instrument (i1)
            m_instrLookupRequest = new InstrumentLookup ( disp, MarketId. CME, ProductType. MultilegInstrument, product1, parentSymbol );
            m_instrLookupRequest. OnData += ( sender, args ) =>
            {
                if ( args. InstrumentLookup. Instrument != null )
                    {
                    Instrument parentInstrument = args.InstrumentLookup.Instrument;
                    LookupLegs ( parentInstrument, leg1Symbol, leg2Symbol, product2, product3 );
                    }
            };
            m_instrLookupRequest. GetAsync ( );
            }

        private void LookupLegs ( Instrument parentInstrument, string leg1Symbol, string leg2Symbol, string product2, string product3, decimal mult1, decimal mult2, decimal ratio, decimal qt )
            {
            InstrumentLookup leg1Lookup = new(disp, MarketId.CME, ProductType.MultilegInstrument, product2, leg1Symbol);
            InstrumentLookup leg2Lookup = new(disp, MarketId.CME, ProductType.MultilegInstrument, product3, leg2Symbol);

            Instrument leg1Instrument = null, leg2Instrument = null;

            leg1Lookup. OnData += ( sender, args ) => { leg1Instrument = args. InstrumentLookup. Instrument; CheckAndStore ( parentInstrument, leg1Instrument, leg2Instrument, mult1, mult2, ratio, qt ); };
            leg2Lookup. OnData += ( sender, args ) => { leg2Instrument = args. InstrumentLookup. Instrument; CheckAndStore ( parentInstrument, leg1Instrument, leg2Instrument, mult1, mult2, ratio, qt ); };

            leg1Lookup. GetAsync ( );
            leg2Lookup. GetAsync ( );
            }
        private void LookupLegs ( Instrument parentInstrument, string leg1Symbol, string leg2Symbol, string product2, string product3 )
            {
            InstrumentLookup leg1Lookup = new(disp, MarketId.CME, ProductType.MultilegInstrument, product2, leg1Symbol);
            InstrumentLookup leg2Lookup = new(disp, MarketId.CME, ProductType.MultilegInstrument, product3, leg2Symbol);

            Instrument leg1Instrument = null, leg2Instrument = null;

            leg1Lookup. OnData += ( sender, args ) => { leg1Instrument = args. InstrumentLookup. Instrument; Subsagain ( parentInstrument, leg1Instrument, leg2Instrument ); };
            leg2Lookup. OnData += ( sender, args ) => { leg2Instrument = args. InstrumentLookup. Instrument; Subsagain ( parentInstrument, leg1Instrument, leg2Instrument ); };

            leg1Lookup. GetAsync ( );
            leg2Lookup. GetAsync ( );
            }
        private void CheckAndStore ( Instrument parentInstrument, Instrument leg1, Instrument leg2, decimal mult1, decimal mult2, decimal ratio, decimal qt )
            {
            if ( leg1 != null && leg2 != null && !parentinstrumentMap. ContainsKey ( parentInstrument ) )
                {


                parentinstrumentMap [ parentInstrument ] = new TradingLegs
                    {
                    Leg1 = leg1,
                    Leg2 = leg2,
                    OrderLeg = parentInstrument,
                    multiplier1 = mult1,
                    multiplier2 = mult2,
                    ratio = ratio,
                    order_qty = qt,
                    max_lot_allowed = Math. Abs ( qt * 4 * mult2 )
                    };

                allinstrumentMap [ parentInstrument ] = parentinstrumentMap [ parentInstrument ];


                allinstrumentMap [ leg1 ] = parentinstrumentMap [ parentInstrument ];


                allinstrumentMap [ leg2 ] = parentinstrumentMap [ parentInstrument ];


                priceupdate_service. Subscribe ( leg1 );
                priceupdate_service. Subscribe ( leg2 );
                priceupdate_service. Subscribe ( parentInstrument );
                }
            }
        private async void Subsagain ( Instrument parentInstrument, Instrument leg1, Instrument leg2 )
            {
            if ( leg1 != null && leg2 != null )
                {
                priceupdate_service. Unsubscribe ( leg1 );
                priceupdate_service. Unsubscribe ( leg2 );
                priceupdate_service. Unsubscribe ( parentInstrument );
                await Task. Delay ( 2000 );
                //add delay of 2 seconds without stopping thread
                priceupdate_service. Subscribe ( leg1 );
                priceupdate_service. Subscribe ( leg2 );
                priceupdate_service. Subscribe ( parentInstrument );
                }
            }
        }
    }
