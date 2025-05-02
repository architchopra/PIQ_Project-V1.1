using System. Collections. Concurrent;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class OrderBookDets
        {
        public string ParentInstrument { get; set; }
        public string Leg1 { get; set; }
        public string Leg2 { get; set; }
        public ConcurrentDictionary<string, Price> BuyOrders { get; set; } = new ConcurrentDictionary<string, Price> ( ); // Order -> Price
        public ConcurrentDictionary<string, Price> SellOrders { get; set; } = new ConcurrentDictionary<string, Price> ( ); // Order -> Price

        public OrderBookDets ( string parentInstrument, string leg1, string leg2, List<string> buyOrders, List<string> sellOrders )
            {
            ParentInstrument = parentInstrument;
            Leg1 = leg1;
            Leg2 = leg2;

            // Initialize buy and sell orders with empty price
            foreach ( var order in buyOrders )
                BuyOrders [ order ] = Price. Empty;

            foreach ( var order in sellOrders )
                SellOrders [ order ] = Price. Empty;
            }
        }
    }
