using System. Collections. Concurrent;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class Hedge_Dets
        {

        public Instrument parent_instrumnet { get; set; }
        public Instrument hedge_instrument { get; set; }
        public Instrument quote_instrument { get; set; }
        public decimal hedge_factor { get; set; }
        public decimal hedgemult { get; set; }
        public decimal quotemult { get; set; }
        public decimal remaining_hedge_qty_buy { get; set; } = 0;
        public decimal remaining_hedge_qty_sell { get; set; } = 0;
        public decimal quote_fill_avg_sell { get; set; } = 0;
        public decimal quote_fill_avg_buy { get; set; } = 0;
        public decimal quote_fill_qty_buy { get; set; } = 0;
        public decimal quote_fill_qty_sell { get; set; } = 0;

        public decimal hedge_fill_avg_sell { get; set; } = 0;
        public decimal hedge_fill_avg_buy { get; set; } = 0;
        public decimal hedge_fill_qty_buy { get; set; } = 0;
        public decimal hedge_fill_qty_sell { get; set; } = 0;

        public ConcurrentDictionary<string, bool> buyquote_hedge { get; set; } = new ConcurrentDictionary<string, bool> ( );
        public ConcurrentDictionary<string, bool> sellquote_hedge { get; set; } = new ConcurrentDictionary<string, bool> ( );

        public Hedge_Dets ( )
            {

            }
        public Hedge_Dets ( TradingLegs td, Instrument inst )
            {
            parent_instrumnet = td. OrderLeg;
            if ( td. Leg1. InstrumentDetails. Alias == inst. InstrumentDetails. Alias )
                {
                hedge_instrument = td. Leg2;
                hedge_factor = td. multiplier2 / td. multiplier1;
                quotemult = td. multiplier1;
                hedgemult = td. multiplier2;
                quote_instrument = td. Leg1;
                }
            else
                {
                hedge_instrument = td. Leg1;
                hedge_factor = td. multiplier1 / td. multiplier2;
                quotemult = td. multiplier2;
                hedgemult = td. multiplier1;
                quote_instrument = td. Leg2;

                }
            }
        public int Add_Buyhedge ( int qt, decimal buy_p )
            {
            quote_fill_avg_buy = quote_fill_avg_buy * quote_fill_qty_buy + qt * buy_p;
            quote_fill_avg_buy /= ( quote_fill_qty_buy + qt );
            quote_fill_qty_buy += qt;
            decimal hedge_qt=qt*hedge_factor;
            hedge_qt += remaining_hedge_qty_buy;
            int integral_part=(int)hedge_qt;
            remaining_hedge_qty_buy = hedge_qt - ( int ) hedge_qt;
            if ( remaining_hedge_qty_buy >= 0.99m )
                {
                integral_part += 1;
                remaining_hedge_qty_buy = 0;
                }
            else if ( remaining_hedge_qty_buy <= -0.99m )
                {
                integral_part -= 1;
                remaining_hedge_qty_buy = 0;
                }
            return integral_part;

            }
        public int Add_Sellhedge ( int qt, decimal sell_p )
            {
            quote_fill_avg_sell = quote_fill_avg_sell * quote_fill_qty_sell + qt * sell_p;
            quote_fill_avg_sell /= ( quote_fill_qty_sell + qt );
            quote_fill_qty_sell += qt;
            decimal hedge_qt=qt*hedge_factor;
            hedge_qt += remaining_hedge_qty_sell;
            int integral_part=(int)hedge_qt;
            remaining_hedge_qty_sell = hedge_qt - ( int ) hedge_qt;
            if ( remaining_hedge_qty_sell >= 0.99m )
                {
                integral_part += 1;
                remaining_hedge_qty_sell = 0;
                }
            else if ( remaining_hedge_qty_sell <= -0.99m )
                {
                integral_part -= 1;
                remaining_hedge_qty_sell = 0;
                }
            return integral_part;
            }
        public void AddHedgeFill ( int qt, decimal sell_p, string order_key )
            {
            if ( buyquote_hedge. ContainsKey ( order_key ) )
                {
                decimal struct_fill=(qt+hedge_fill_qty_buy)/hedgemult;
                if ( struct_fill - ( int ) struct_fill >= 0.99m )
                    {
                    struct_fill = ( int ) struct_fill + 1;
                    }
                else if ( struct_fill - ( int ) struct_fill <= -0.99m )
                    {
                    struct_fill = ( int ) struct_fill - 1;
                    }
                if ( Math. Abs ( ( int ) struct_fill ) > 0 )
                    {
                    decimal quantity_left_hedge=(hedge_fill_qty_buy+qt) % Math.Abs(hedgemult);
                    hedge_fill_avg_buy = hedge_fill_avg_buy * hedge_fill_qty_buy + ( qt - quantity_left_hedge ) * sell_p;
                    hedge_fill_avg_buy /= ( hedge_fill_qty_buy + ( qt - quantity_left_hedge ) );
                    decimal struct_price=quote_fill_avg_buy*quotemult+ hedge_fill_avg_buy*hedgemult;
                    hedge_fill_qty_buy = quantity_left_hedge;
                    hedge_fill_avg_buy = sell_p;
                    quote_fill_qty_buy -= Math. Abs ( quotemult * ( int ) struct_fill );
                    if ( quote_fill_qty_buy == 0 )
                        {
                        quote_fill_avg_buy = 0;
                        }
                    Logger. InformationAsync ( "Fill for {0} Sell {1}@{2} for legs {3},{4}", parent_instrumnet, ( int ) struct_fill, struct_price, quote_instrument. InstrumentDetails. Alias, hedge_instrument. InstrumentDetails. Alias );
                    }
                else
                    {
                    hedge_fill_avg_buy = hedge_fill_avg_buy * hedge_fill_qty_buy + qt * sell_p;
                    hedge_fill_avg_buy /= ( hedge_fill_qty_buy + qt );
                    hedge_fill_qty_buy = hedge_fill_qty_buy + qt;


                    }


                }
            else if ( sellquote_hedge. ContainsKey ( order_key ) )
                {
                decimal struct_fill=(qt+hedge_fill_qty_sell)/hedgemult;
                if ( struct_fill - ( int ) struct_fill >= 0.99m )
                    {
                    struct_fill = ( int ) struct_fill + 1;
                    }
                else if ( struct_fill - ( int ) struct_fill <= -0.99m )
                    {
                    struct_fill = ( int ) struct_fill - 1;
                    }
                if ( Math. Abs ( ( int ) struct_fill ) > 0 )
                    {
                    decimal quantity_left_hedge=(hedge_fill_qty_sell+qt) % Math.Abs(hedgemult);
                    hedge_fill_avg_sell = hedge_fill_avg_sell * hedge_fill_qty_sell + ( qt - quantity_left_hedge ) * sell_p;
                    hedge_fill_avg_sell /= ( hedge_fill_qty_sell + ( qt - quantity_left_hedge ) );
                    decimal struct_price=quote_fill_avg_sell*quotemult+ hedge_fill_avg_sell*hedgemult;
                    hedge_fill_qty_sell = quantity_left_hedge;
                    hedge_fill_avg_sell = sell_p;
                    quote_fill_qty_sell -= Math. Abs ( quotemult * ( int ) struct_fill );
                    if ( quote_fill_qty_sell == 0 )
                        {
                        quote_fill_avg_sell = 0;
                        }
                    Logger. InformationAsync ( "Fill for {0} Buy {1}@{2} for legs {3},{4}", parent_instrumnet, ( int ) struct_fill, struct_price, quote_instrument. InstrumentDetails. Alias, hedge_instrument. InstrumentDetails. Alias );
                    }
                else
                    {
                    hedge_fill_avg_sell = hedge_fill_avg_sell * hedge_fill_qty_sell + qt * sell_p;
                    hedge_fill_avg_sell /= ( hedge_fill_qty_sell + qt );
                    hedge_fill_qty_sell = hedge_fill_qty_sell + qt;


                    }
                }
            return;
            }
        }
    }
