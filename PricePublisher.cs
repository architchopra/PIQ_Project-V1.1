using System. Collections. Concurrent;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class PricePublisher
        {
        public ManualResetEvent priceChangedEvent = new ManualResetEvent(false);
        private readonly Instrument symbol=null;
        private PriceUpdate latestPriceUpdate;
        private Dispatcher m_disp=null;
        private PriceSubscription m_priceSubscription = null;
        private PriceSubscription m_priceSubscription1 = null;
        BlockingCollection<string> price_upd = new BlockingCollection<string>();
        ConcurrentDictionary<string, PriceUpdate> price_upd2 = new ConcurrentDictionary<string, PriceUpdate>();
        DateTime dt_check;
        bool first=true;
        public PricePublisher ( )
            {

            }
        public PricePublisher ( Instrument symbol, Dispatcher disp, BlockingCollection<string> price_upds, ConcurrentDictionary<string, PriceUpdate> price_updates2 )
            {
            this. m_disp = disp;
            this. symbol = symbol;
            this. price_upd = price_upds;
            this. price_upd2 = price_updates2;
            }

        // Method to start publishing updates
        public void StartPublishingUpdates ( )
            {
            m_priceSubscription = new PriceSubscription ( symbol, m_disp );
            m_priceSubscription. Settings = new PriceSubscriptionSettings ( PriceSubscriptionType. MarketDepth );
            m_priceSubscription. FieldsUpdated += m_priceSubscription_FieldsUpdated;
            m_priceSubscription. Start ( );

            }
        public void StartPublishingUpdates1 ( )
            {
            m_priceSubscription1 = new PriceSubscription ( symbol, m_disp );
            m_priceSubscription1. Settings = new PriceSubscriptionSettings ( PriceSubscriptionType. MarketDepth );
            m_priceSubscription1. FieldsUpdated += m_priceSubscription_FieldsUpdated1;
            m_priceSubscription1. Start ( );

            }
        void m_priceSubscription_FieldsUpdated ( object sender, FieldsUpdatedEventArgs e )
            {

            if ( e. Error == null )
                {

                Price Bid=e. Fields. GetBestBidPriceField ( 0 ). Value;
                Price Ask=e. Fields. GetBestAskPriceField ( 0 ). Value;
                Quantity Bid_qty=e. Fields. GetBestBidQuantityField ( ). Value;
                Quantity Ask_qty=e. Fields. GetBestAskQuantityField ( ). Value;
                string alias=e. Fields. Instrument. InstrumentDetails. Alias;
                if ( Bid. IsValid && Bid. IsTradable && Ask. IsValid && Ask. IsTradable &&
                    Bid_qty != 0 && Ask_qty != 0 &&
                    Bid_qty. IsValid && Ask_qty. IsValid &&
                    Bid_qty. Value != Quantity. Empty && Ask_qty != Quantity. Empty &&
                    Bid != Price. Empty && Ask != Price. Empty )
                    {
                    try
                        {
                        if ( price_upd2. ContainsKey ( alias ) )
                            {
                            price_upd2 [ alias ] = new PriceUpdate ( Bid, Ask, e. Fields. Instrument, Bid_qty, Ask_qty );
                            price_upd. Add ( alias );
                            }
                        else
                            {
                        label:
                            bool flag = price_upd2. TryAdd ( alias, new PriceUpdate ( Bid, Ask, e. Fields. Instrument, Bid_qty, Ask_qty ));
                            if ( !price_upd2. ContainsKey ( alias ) )
                                {
                                goto label;
                                }
                            price_upd. Add ( alias );

                            }
                        }
                    catch ( Exception ex )
                        {

                        }
                    }
                }
            else
                {
                if ( e. Error != null )
                    {
                    Logger. FatalAsync ( "Unrecoverable price subscription error: {0},{1}", e. Error. Message, e. Error );
                    Dispose ( );// function to dispose the current price sub  after error 
                    Dispose2 ( );// function to dispose the current price sub  after error 
                    StartPublishingUpdates ( );//Function to resubscribe the price for this product

                    }
                }
            }

        void m_priceSubscription_FieldsUpdated1 ( object sender, FieldsUpdatedEventArgs e )
            {

            if ( e. Error == null )
                {

                Price Bid=e. Fields. GetBestBidPriceField ( 0 ). Value;
                Price Ask=e. Fields. GetBestAskPriceField ( 0 ). Value;
                Quantity Bid_qty=e. Fields. GetBestBidQuantityField ( ). Value;
                Quantity Ask_qty=e. Fields. GetBestAskQuantityField ( ). Value;
                string alias=e. Fields. Instrument. InstrumentDetails. Alias;
                if ( Bid. IsValid && Bid. IsTradable && Ask. IsValid && Ask. IsTradable &&
                    Bid_qty != 0 && Ask_qty != 0 &&
                    Bid_qty. IsValid && Ask_qty. IsValid &&
                    Bid_qty. Value != Quantity. Empty && Ask_qty != Quantity. Empty &&
                    Bid != Price. Empty && Ask != Price. Empty )
                    {
                    try
                        {
                        if ( price_upd2. ContainsKey ( alias ) )
                            {
                            if ( ( price_upd2 [ alias ]. AskPrice. ToDecimal ( ) != Ask. ToDecimal ( ) ) || ( price_upd2 [ alias ]. BidPrice. ToDecimal ( ) != Bid. ToDecimal ( ) ) )
                                {
                                price_upd2 [ alias ] = new PriceUpdate ( Bid, Ask, e. Fields. Instrument, Bid_qty, Ask_qty );
                                price_upd. Add ( alias );

                                }
                            }
                        else
                            {
                        label:
                            bool flag = price_upd2. TryAdd ( alias, new PriceUpdate ( Bid, Ask, e. Fields. Instrument, Bid_qty,Ask_qty ) );
                            if ( !price_upd2. ContainsKey ( alias ) )
                                {
                                goto label;
                                }
                            price_upd. Add ( alias );

                            }
                        }
                    catch ( Exception ex )
                        {

                        }

                    }
                }
            else
                {
                if ( e. Error != null )
                    {
                    Logger. FatalAsync ( "Unrecoverable price subscription error: {0},{1}", e. Error. Message, e. Error );
                    Dispose2 ( );// function to dispose the current price sub  after error 
                    Dispose ( );// function to dispose the current price sub  after error 
                    StartPublishingUpdates1 ( );//Function to resubscribe the price for this product

                    }
                }
            }

        // Method to get the latest price update

        public void Dispose ( )
            {
            try
                {
                if ( m_priceSubscription1 != null )
                    {
                    price_upd2. TryRemove ( symbol. InstrumentDetails. Alias, out PriceUpdate pu );

                    m_priceSubscription1. FieldsUpdated -= m_priceSubscription_FieldsUpdated1;
                    m_priceSubscription1. Dispose ( );
                    m_priceSubscription1 = null;
                    }



                }
            catch ( Exception ex )
                {

                }


            }
        public void Dispose2 ( )
            {
            try
                {
                if ( m_priceSubscription != null )
                    {
                    price_upd2. TryRemove ( symbol. InstrumentDetails. Alias, out PriceUpdate pu );

                    m_priceSubscription. FieldsUpdated -= m_priceSubscription_FieldsUpdated;
                    m_priceSubscription. Dispose ( );
                    m_priceSubscription = null;

                    }
                }
            catch ( Exception ex )
                {

                }
            }

        }

    }
