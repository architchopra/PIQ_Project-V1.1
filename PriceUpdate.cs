using tt_net_sdk;

namespace PIQ_Project
    {
    public struct PriceUpdate
        {
        public Price BidPrice { get; } = Price. Empty;
        public Price AskPrice { get; } = Price. Empty;
        public Quantity BidQty { get; } = Quantity. Empty;
        public Quantity AskQty { get; } = Quantity. Empty;
        public Instrument Instr { get; } = null;

        public PriceUpdate ( Price bidPrice, Price askPrice, Instrument instr, Quantity bidQty, Quantity askQty )
            {
            BidPrice = bidPrice;
            AskPrice = askPrice;
            Instr = instr;
            BidQty = bidQty;
            AskQty = askQty;
            }
        }
    }
