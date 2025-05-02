using System. Collections. Concurrent;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class TS_Subscriber
        {
        public delegate void BookUpdatedEventHandler ( );
        public static event BookUpdatedEventHandler BookUpdated;
        private BlockingCollection<Order_enum> updateQueue = new BlockingCollection<Order_enum>();
        private BlockingCollection<OrderFilledEventArgs> updateQueue_fill = new BlockingCollection<OrderFilledEventArgs>();

        ConcurrentBag<Order> Bg = new ConcurrentBag<Order>();

        Trade_Sub_processor ts_processor=null;
        public TS_Subscriber ( BlockingCollection<Order_enum> updateQueue, BlockingCollection<OrderFilledEventArgs> update_fill, ConcurrentBag<Order> Bg, Trade_Sub_processor ts_processor1 )
            {
            this. updateQueue = updateQueue;
            this. Bg = Bg;
            this. ts_processor = ts_processor1;
            this. updateQueue_fill = update_fill;
            }
        public void EnqueueUpdate ( Order_enum update )
            {
            updateQueue. Add ( update );
            }
        public void EnqueueUpdate_fill ( OrderFilledEventArgs update )
            {
            updateQueue_fill. Add ( update );
            }

        public void BookSend ( ConcurrentBag<Order> bag )
            {
            foreach ( var el in bag )
                {
                Bg. Add ( el );
                }
            OnBookUpdated ( );
            if ( ts_processor != null )
                {
                ts_processor. StartProcessingUpdates ( );
                }
            }
        protected virtual void OnBookUpdated ( )
            {
            BookUpdated?.Invoke ( );
            }
        }
    }
