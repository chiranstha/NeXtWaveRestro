namespace NextWave.Erp.Enums
{
    public enum RestaurantTableStatus
    {
        Available = 0,
        Occupied = 1,
        Reserved = 2,
        Cleaning = 3,
        Inactive = 4
    }

    public enum RestaurantOrderType
    {
        DineIn = 0,
        TakeAway = 1,
        Delivery = 2,
        Mobile = 3,
        Qr = 4
    }

    public enum RestaurantOrderStatus
    {
        Draft = 0,
        SentToKitchen = 1,
        InProgress = 2,
        Ready = 3,
        Served = 4,
        Billed = 5,
        Closed = 6,
        Cancelled = 7
    }

    public enum RestaurantOrderItemStatus
    {
        Draft = 0,
        Sent = 1,
        Preparing = 2,
        Ready = 3,
        Served = 4,
        Cancelled = 5
    }

    public enum RestaurantTicketType
    {
        KOT = 0,
        BOT = 1
    }

    public enum RestaurantTicketPurpose
    {
        NewOrder = 0,
        Cancellation = 1,
        AddOn = 2
    }

    public enum RestaurantTicketStatus
    {
        Pending = 0,
        InProgress = 1,
        Ready = 2,
        Served = 3,
        Cancelled = 4
    }

    public enum RestaurantStationType
    {
        Kitchen = 0,
        Bar = 1,
        Counter = 2,
        Other = 3
    }

    public enum RestaurantDeviceStatus
    {
        Active = 0,
        Blocked = 1
    }

    public enum RestaurantStockAdjustmentType
    {
        Increase = 0,
        Decrease = 1,
        PhysicalCount = 2,
        Wastage = 3
    }

    public enum RestaurantChannelType
    {
        DineIn = 0,
        TakeAway = 1,
        OwnOnline = 2,
        Aggregator = 3
    }

    public enum RestaurantChannelProvider
    {
        Internal = 0,
        OwnOnline = 1,
        Foodmandu = 2,
        Pathao = 3,
        Bhojdeals = 4
    }

    public enum RestaurantChannelSyncStatus
    {
        PendingPush = 0,
        Synced = 1,
        Failed = 2,
        Disabled = 3
    }

    public enum RestaurantExternalOrderStatus
    {
        Received = 0,
        Accepted = 1,
        Rejected = 2,
        Cancelled = 3,
        Completed = 4
    }

    public enum RestaurantPayoutMatchStatus
    {
        Pending = 0,
        Matched = 1,
        Unmatched = 2,
        Discrepancy = 3
    }

    public enum RestaurantCustomerPaymentMode
    {
        CashOnDelivery = 0,
        CounterSettlement = 1
    }

    public enum RestaurantGuestOrderApprovalStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public enum RestaurantReservationStatus
    {
        Requested = 0,
        Confirmed = 1,
        Declined = 2,
        Waitlisted = 3,
        Seated = 4,
        Cancelled = 5,
        Completed = 6,
        NoShow = 7
    }

    public enum RestaurantPrintJobType
    {
        KitchenTicket = 0,
        BillReceipt = 1
    }

    public enum RestaurantPrintJobStatus
    {
        Pending = 0,
        Leased = 1,
        Printed = 2,
        Failed = 3,
        Cancelled = 4
    }

    public enum RestaurantSmsOutboxStatus
    {
        Pending = 0,
        Sent = 1,
        Failed = 2
    }

    public enum RestaurantSyncEntityType
    {
        MenuCategory = 0,
        MenuItem = 1,
        MenuVariant = 2,
        ModifierGroup = 3,
        Modifier = 4,
        DiningTable = 5,
        Order = 6,
        OrderItem = 7,
        Ticket = 8,
        TicketItem = 9,
        Review = 10,
        Channel = 11,
        Device = 12
    }

    public enum RestaurantSyncOperation
    {
        Upsert = 0,
        Delete = 1
    }

    public enum RestaurantSyncUploadStatus
    {
        Pending = 0,
        Applied = 1,
        Conflict = 2,
        Failed = 3
    }
}
