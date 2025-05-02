using tt_net_sdk;

namespace PIQ_Project
    {
    public class TradingLegs
        {
        public Instrument Leg1 { get; set; }
        public Instrument Leg2 { get; set; }
        public Instrument OrderLeg { get; set; }
        public bool isallowed_s { get; set; }
        public bool isallowed_b { get; set; }
        public decimal order_qty { get; set; }
        public string BestBidOrder { get; set; }
        public string SecondBestBidOrder { get; set; }
        public string ThirdBestBidOrder { get; set; }
        public string FourthBestBidOrder { get; set; }
        public decimal multiplier1 { get; set; }
        public decimal multiplier2 { get; set; }
        public decimal ratio { get; set; }
        public decimal max_lot_allowed { get; set; }
        public decimal max_lot_b { get; set; }
        public decimal max_lot_s { get; set; }
        public string BestAskOrder { get; set; }
        public string SecondBestAskOrder { get; set; }
        public string ThirdBestAskOrder { get; set; }
        public string FourthBestAskOrder { get; set; }

        public Price bestbid { get; set; }
        public Price bestask { get; set; }
        public TradingLegs ( )
            {
            BestBidOrder = string. Empty;
            SecondBestBidOrder = string. Empty;
            ThirdBestBidOrder = string. Empty;
            FourthBestBidOrder = string. Empty;
            BestAskOrder = string. Empty;
            SecondBestAskOrder = string. Empty;
            ThirdBestAskOrder = string. Empty;
            FourthBestAskOrder = string. Empty;
            bestbid = Price. Empty;
            bestask = Price. Empty;
            isallowed_s = true;
            isallowed_b = true;
            }
        public bool order_keydel ( string order_key )
            {
            switch ( order_key )
                {
                case var _ when order_key == BestBidOrder:
                    BestBidOrder = string. Empty;
                    return true;
                case var _ when order_key == SecondBestBidOrder:
                    SecondBestBidOrder = string. Empty;
                    return true;

                case var _ when order_key == ThirdBestBidOrder:
                    ThirdBestBidOrder = string. Empty;
                    return true;
                case var _ when order_key == FourthBestBidOrder:
                    FourthBestBidOrder = string. Empty;
                    return true;

                case var _ when order_key == BestAskOrder:
                    BestAskOrder = string. Empty;
                    return true;


                case var _ when order_key == SecondBestAskOrder:
                    SecondBestAskOrder = string. Empty;
                    return true;

                case var _ when order_key == ThirdBestAskOrder:
                    ThirdBestAskOrder = string. Empty;
                    return true;
                case var _ when order_key == FourthBestAskOrder:
                    FourthBestAskOrder = string. Empty;
                    return true;
                default:
                    return false;
                }
            }
        }
    }
