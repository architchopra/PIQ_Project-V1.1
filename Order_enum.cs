using tt_net_sdk;

namespace PIQ_Project
    {
    public class Order_enum
        {
        public enum UpdateType
            {
            OrderFilled,
            OrderAdded,
            OrderUpdated,
            OrderDeleted,
            OrderRejected
            }

        public UpdateType Type { get; set; }
        public OrderFilledEventArgs OrderFilled { get; set; }
        public OrderAddedEventArgs OrderAdded { get; set; }
        public OrderUpdatedEventArgs OrderUpdated { get; set; }
        public OrderDeletedEventArgs OrderDeleted { get; set; }
        public OrderRejectedEventArgs OrderRejected { get; set; }

        public Order_enum ( OrderFilledEventArgs orderFilled )
            {
            Type = UpdateType. OrderFilled;
            OrderFilled = orderFilled;
            }

        public Order_enum ( OrderAddedEventArgs orderAdded )
            {
            Type = UpdateType. OrderAdded;
            OrderAdded = orderAdded;
            }

        public Order_enum ( OrderUpdatedEventArgs orderUpdated )
            {
            Type = UpdateType. OrderUpdated;
            OrderUpdated = orderUpdated;
            }

        public Order_enum ( OrderDeletedEventArgs orderDeleted )
            {
            Type = UpdateType. OrderDeleted;
            OrderDeleted = orderDeleted;
            }

        public Order_enum ( OrderRejectedEventArgs orderRejected )
            {
            Type = UpdateType. OrderRejected;
            OrderRejected = orderRejected;
            }
        }
    }
