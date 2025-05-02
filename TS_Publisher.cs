using System. Collections. Concurrent;
using tt_net_sdk;

namespace PIQ_Project
    {
    public class TS_Publisher
        {
        private static Thread TradeDispatcherThread = new Thread(AttachTradeDispatcherToThisThread);
        private static tt_net_sdk.WorkerDispatcher TradeDispatcher = null;
        private Dictionary<string, BlockingCollection<Order>> dataQueues = new Dictionary<string, BlockingCollection<Order>>();
        private static readonly AutoResetEvent DispatcherInitializedEvent = new AutoResetEvent(false);
        ConcurrentBag<Order> initial_order_List = new ConcurrentBag<Order>();
        private BlockingCollection<Order_enum> updatesQueue = new BlockingCollection<Order_enum>();
        private BlockingCollection<OrderFilledEventArgs> updatesQueue_fill = new BlockingCollection<OrderFilledEventArgs>();
        private ConcurrentDictionary<TS_Subscriber, List<string>> subscriberSubscriptions = new ConcurrentDictionary<TS_Subscriber, List<string>>();
        private Dispatcher m_disp;
        public TradeSubscription m_instrumentTradeSubscription = null;
        private string account_name = "XGJRE";
        TradeSubscriptionTTAccountFilter tsiAF;
        private ConcurrentDictionary<TS_Subscriber, bool> subscribers_fill = new ConcurrentDictionary<TS_Subscriber, bool>();
        private ConcurrentDictionary<TS_Subscriber, bool> subscribers_add = new ConcurrentDictionary<TS_Subscriber, bool>();
        private ConcurrentDictionary<TS_Subscriber, bool> subscribers_update = new ConcurrentDictionary<TS_Subscriber, bool>();
        private ConcurrentDictionary<TS_Subscriber, bool> subscribers_delete = new ConcurrentDictionary<TS_Subscriber, bool>();
        private ConcurrentDictionary<TS_Subscriber, bool> subscribers_reject = new ConcurrentDictionary<TS_Subscriber, bool>();
        private static bool IsDispatcherThreadStarted = false;
        List<TS_Subscriber> s_list = new List<TS_Subscriber>();
        private readonly object sendOrdersLock = new object();
        bool m_orderbook_download = false;
        SlidingWindow sw=null;
        public TS_Publisher ( Dispatcher disp, String acc_name )
            {
            this. m_disp = disp;
            this. account_name = acc_name;
            // Initialize data queues for different types of updates
            InitializeDataQueues ( );
            }
        private static void AttachTradeDispatcherToThisThread ( )
            {
            TradeDispatcher = tt_net_sdk. Dispatcher. AttachWorkerDispatcher ( );
            DispatcherInitializedEvent. Set ( ); // Signal that the dispatcher is initialized
            TradeDispatcher. Run ( );
            }
        // Method to initialize data queues for different types of updates
        private void InitializeDataQueues ( )
            {
            // Initialize a data queue for each type of update


            dataQueues [ "OrderAdded" ] = new BlockingCollection<Order> ( );

            dataQueues [ "OrderRejected" ] = new BlockingCollection<Order> ( );

            sw = new SlidingWindow ( );

            m_instrumentTradeSubscription = new TradeSubscription ( m_disp );
            tsiAF = new TradeSubscriptionTTAccountFilter ( account_name, false, "Acct Filter" );
            m_instrumentTradeSubscription. SetFilter ( tsiAF );

            m_instrumentTradeSubscription. OrderUpdated += new EventHandler<OrderUpdatedEventArgs> ( m_instrumentTradeSubscription_OrderUpdated );
            m_instrumentTradeSubscription. OrderDeleted += new EventHandler<OrderDeletedEventArgs> ( m_instrumentTradeSubscription_OrderDeleted );

            m_instrumentTradeSubscription. OrderAdded += new EventHandler<OrderAddedEventArgs> ( m_instrumentTradeSubscription_OrderAdded );

            m_instrumentTradeSubscription. OrderFilled += new EventHandler<OrderFilledEventArgs> ( m_instrumentTradeSubscription_OrderFilled );
            m_instrumentTradeSubscription. OrderRejected += new EventHandler<OrderRejectedEventArgs> ( m_instrumentTradeSubscription_OrderRejected );
            m_instrumentTradeSubscription. OrderBookDownload += new EventHandler<OrderBookDownloadEventArgs> ( m_instrumentTradeSubscription_OrderBookDownload );
            m_instrumentTradeSubscription. Start ( );
            // Add more queues for other types of updates as needed
            }
        void m_instrumentTradeSubscription_OrderBookDownload ( object sender, OrderBookDownloadEventArgs e )
            {
            foreach ( var order in e. Orders. ToList ( ) )
                {
                initial_order_List. Add ( order );
                }
            Console. WriteLine ( "Orderbook downloaded..." );

            foreach ( var s in s_list )
                {
                s. BookSend ( initial_order_List );
                }
            m_orderbook_download = true;
            if ( !IsDispatcherThreadStarted )
                {
                IsDispatcherThreadStarted = true;
                TradeDispatcherThread. Start ( );
                DispatcherInitializedEvent. WaitOne ( ); // Wait until the dispatcher is initialized
                }
            DistributeUpdates ( );
            DistributeUpdates2 ( );
            }
        void m_instrumentTradeSubscription_OrderRejected ( object sender, OrderRejectedEventArgs e )
            {
            Console. WriteLine ( "\nOrderRejected [{0}]", e. Order. SiteOrderKey );
            updatesQueue. Add ( new Order_enum ( e ) );



            }
        void m_instrumentTradeSubscription_OrderFilled ( object sender, OrderFilledEventArgs e )
            {
            Console. WriteLine ( Thread. CurrentThread. ManagedThreadId );
            updatesQueue_fill. Add ( e );
            }
        void m_instrumentTradeSubscription_OrderDeleted ( object sender, OrderDeletedEventArgs e )
            {


            updatesQueue. Add ( new Order_enum ( e ) );
            }
        void m_instrumentTradeSubscription_OrderAdded ( object sender, OrderAddedEventArgs e )
            {
            Console. WriteLine ( "order added THREAD ,{0}", Thread. CurrentThread. ManagedThreadId );

            updatesQueue. Add ( new Order_enum ( e ) );

            }
        void m_instrumentTradeSubscription_OrderUpdated ( object sender, OrderUpdatedEventArgs e )
            {
            updatesQueue. Add ( new Order_enum ( e ) );


            }
        public void AddSubscriber ( TS_Subscriber subscriber, List<string> subscriptionTypes )
            {
            subscriberSubscriptions. TryAdd ( subscriber, subscriptionTypes );

            // Start a background task to continuously distribute updates to this subscriber
            if ( subscriberSubscriptions [ subscriber ]. Contains ( "order_book" ) )
                {
                if ( m_orderbook_download )
                    {
                    subscriber. BookSend ( initial_order_List );
                    }
                else
                    {
                    s_list. Add ( subscriber );
                    }


                }
            if ( subscriberSubscriptions [ subscriber ]. Contains ( "order_filled" ) )
                {
                subscribers_fill. TryAdd ( subscriber, true );
                }
            if ( subscriberSubscriptions [ subscriber ]. Contains ( "order_add" ) )
                {

                subscribers_add. TryAdd ( subscriber, true );
                }
            if ( subscriberSubscriptions [ subscriber ]. Contains ( "order_update" ) )
                {
                subscribers_update. TryAdd ( subscriber, true );
                }
            if ( subscriberSubscriptions [ subscriber ]. Contains ( "order_delete" ) )
                {
                subscribers_delete. TryAdd ( subscriber, true );
                }
            if ( subscriberSubscriptions [ subscriber ]. Contains ( "order_reject" ) )
                {
                subscribers_reject. TryAdd ( subscriber, true );
                }

            }
        private void DistributeUpdates ( )
            {

            TradeDispatcher. DispatchAction ( ( ) =>
            {
                foreach ( var update in updatesQueue. GetConsumingEnumerable ( ) )
                    {
                    switch ( update. Type )
                        {
                        case Order_enum. UpdateType. OrderAdded:
                            foreach ( var subscriber in subscribers_add )
                                {
                                subscriber. Key. EnqueueUpdate ( update );
                                }
                            break;

                        case Order_enum. UpdateType. OrderUpdated:
                            foreach ( var subscriber in subscribers_update )
                                {
                                subscriber. Key. EnqueueUpdate ( update );
                                }
                            break;

                        case Order_enum. UpdateType. OrderDeleted:
                            foreach ( var subscriber in subscribers_delete )
                                {
                                subscriber. Key. EnqueueUpdate ( update );
                                }
                            break;

                        case Order_enum. UpdateType. OrderRejected:
                            foreach ( var subscriber in subscribers_reject )
                                {
                                subscriber. Key. EnqueueUpdate ( update );
                                }
                            break;
                        }
                    }

            } );

            }
        private void DistributeUpdates2 ( )
            {
            Task. Run ( ( ) =>
            {
                foreach ( var update in updatesQueue_fill. GetConsumingEnumerable ( ) )
                    {
                    // Distribute the update to each subscriber
                    foreach ( var subscribe in subscribers_fill )
                        {
                        subscribe. Key. EnqueueUpdate_fill ( update );
                        }
                    }

            } );
            }

        public void delete_order ( string order_key )
            {
            if ( m_instrumentTradeSubscription. Orders. ContainsKey ( order_key ) )
                {
                OrderProfile op = m_instrumentTradeSubscription.Orders[order_key].GetOrderProfile();
                op. Action = OrderAction. Delete;
                op. TextTT = "deleting";
                if ( !m_instrumentTradeSubscription. SendOrder ( op ) )
                    {
                    Console. WriteLine ( "order paused failed{0}", order_key );

                    }
                else
                    {
                    Console. WriteLine ( "order paused {0}", order_key );
                    }
                }
            }

        public string Send_Orders_Series ( OrderProfile op )
            {
            if ( op. Action == OrderAction. Add )
                {
                sw. Add ( op. InstrumentDetails. Alias, Convert. ToInt32 ( op. OrderQuantity ) );
                int sum_qty= sw. GetSumLast30Minutes ( op. InstrumentDetails. Alias );
                int x=sw.AddEvent(op.Instrument.InstrumentDetails.Alias,DateTime.Now);
                if ( sum_qty < 50000 && x <= 8 )
                    {
                    if ( op. OrderQuantity. IsValid && op. OrderQuantity != Quantity. Empty && op. LimitPrice. IsValid && op. LimitPrice. IsTradable && op. LimitPrice != null )
                        {

                        lock ( sendOrdersLock )
                            {
                            if ( !m_instrumentTradeSubscription. SendOrder ( op ) )
                                {

                                Console. WriteLine ( "send new order failed for {0},at price{1},orderprofile:{2}", op. Instrument, op. LimitPrice, op. ToString ( ) );
                                return string. Empty;
                                }
                            else
                                {

                                // Display the elapsed time

                                Console. WriteLine ( "\nSent new order: " + op. Instrument. Name + " " + op. BuySell + " " + op. OrderQuantity. ToString ( ) + "@" + op. LimitPrice. ToString ( ) + " SOK=" + op. SiteOrderKey );
                                return op. SiteOrderKey;

                                }

                            }
                        }
                    else
                        {
                        return string. Empty;
                        }
                    }
                else
                    {
                    return string. Empty;

                    }

                }
            else
                {
                if ( op. OrderQuantity. IsValid && op. OrderQuantity != Quantity. Empty && op. LimitPrice. IsValid && op. LimitPrice. IsTradable && op. LimitPrice != null )
                    {

                    lock ( sendOrdersLock )
                        {
                        if ( !m_instrumentTradeSubscription. SendOrder ( op ) )
                            {

                            Console. WriteLine ( "send new order failed for {0},at price{1},orderprofile:{2}", op. Instrument, op. LimitPrice, op. ToString ( ) );
                            return string. Empty;
                            }
                        else
                            {

                            // Display the elapsed time

                            Console. WriteLine ( "\nSent new order: " + op. Instrument. Name + " " + op. BuySell + " " + op. OrderQuantity. ToString ( ) + "@" + op. LimitPrice. ToString ( ) + " SOK=" + op. SiteOrderKey );
                            return op. SiteOrderKey;

                            }

                        }
                    }
                else
                    {
                    return string. Empty;
                    }
                }
            return "";

            }


        public IDictionary<string, Order> getLatestOrderlist ( )
            {
            return m_instrumentTradeSubscription. Orders;
            }
        }
    }
