using System. Collections. Concurrent;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class PriceUpdateService
        {
        private ConcurrentDictionary<Instrument, PricePublisher> publishers = new ConcurrentDictionary<Instrument, PricePublisher>();

        BlockingCollection<string>price_upd=new BlockingCollection<string>();
        ConcurrentDictionary<string,PriceUpdate>price_upd2=new ConcurrentDictionary<string, PriceUpdate>();
        private Dispatcher m_disp;

        public PriceUpdateService ( Dispatcher disp, BlockingCollection<string> price_updates, ConcurrentDictionary<string, PriceUpdate> price_update2 )
            {
            this. m_disp = disp;
            this. price_upd = price_updates;
            this. price_upd2 = price_update2;
            }


        // Method to subscribe to price updates for a symbol
        public void Subscribe ( Instrument symbol )
            {
            if ( !publishers. ContainsKey ( symbol ) )
                {

                PricePublisher publisher = new PricePublisher(symbol, m_disp,price_upd,price_upd2);
                publishers. TryAdd ( symbol, publisher );
                if ( symbol. InstrumentDetails. Alias. Contains ( "Double" ) )
                    {

                    Thread updateThread = new Thread(publisher.StartPublishingUpdates);
                    updateThread. Start ( );
                    }
                else
                    {
                    Thread updateThread = new Thread(publisher.StartPublishingUpdates1);
                    updateThread. Start ( );


                    }

                }
            }
        public void Subscribe2 ( Instrument symbol )
            {
            if ( !publishers. ContainsKey ( symbol ) )
                {

                PricePublisher publisher = new PricePublisher(symbol, m_disp,price_upd,price_upd2);
                publishers. TryAdd ( symbol, publisher );
                Thread updateThread = new Thread(publisher.StartPublishingUpdates);
                updateThread. Start ( );
                }
            }
        public void Unsubscribe ( Instrument symbol )
            {
            if ( publishers. TryGetValue ( symbol, out var publisher ) )
                {
                publisher. Dispose ( );
                publishers. TryRemove ( symbol, out _ ); // Remove the publisher as it is for a singleton subscriber

                }
            }
        public void Unsubscribe2 ( Instrument symbol )
            {
            if ( publishers. TryGetValue ( symbol, out var publisher ) )
                {
                publisher. Dispose2 ( );
                publishers. TryRemove ( symbol, out _ ); // Remove the publisher as it is for a singleton subscriber

                }
            }
        }
    }
