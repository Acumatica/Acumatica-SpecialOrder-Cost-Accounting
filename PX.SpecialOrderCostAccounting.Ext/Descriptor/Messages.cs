using PX.Common;

namespace PX.SpecialOrderCostAccounting.Ext
{
    [PXLocalizable(Prefix)]
    public static class Messages
    {
        public const string Prefix = "Special Order Cost Accounting";

        public const string ViewSalesOrServiceOrderFromPO = "Sales/Service Order";
        public const string Delink = "Delink Line";

        public const string ViewMultiple = "Multiple";

        public const string InvalidSpecialOrderItem = "Special Order Item can not be Lot/Serial Tracked and must have Average Valuation Method.";
        public const string ChangingQtyForLinkedRecord = "Updating quantity will update in the linked Sales Order as well. Do you want to continue?";
        public const string ChangingQtyForLinkedRecordFSO = "Updating quantity will update in the linked Service Order as well. Do you want to continue?";
        public const string ChangingQtyForLinkedRecordFSOAppt = "Updating quantity will update in the linked Service Order Appointment as well. Do you want to continue?";

        public const string ChangingCostForLinkedRecord = "Updating cost will update in the linked Sales Order as well. Do you want to continue?";
        public const string ChangingCostForLinkedRecordFSO = "Updating cost will update in the linked Service Order as well. Do you want to continue?";
        public const string ChangingCostForLinkedRecordFSOAppt = "Updating cost will update in the linked Service Order Appointment as well. Do you want to continue?";

        public const string POLineLinkedToServiceOrderLine = "Deletion of the purchase order line will unlink service order '{0}' from this purchase order. Do you want to continue?";
        public const string AddingNewPOLineToLinkedSO = "Adding a new line will not update the linked Sales Order. Do you want to continue?";
        public const string AddingNewPOLineToLinkedSrv = "Adding a new line will not update the linked Service Order. Do you want to continue?";

        public const string POOrderLinkedToServiceOrderLine = "Deletion of the purchase order line will unlink service order from this purchase order. Do you want to continue?";
        public const string POOrderLinkedToSalesOrderLine = "Deletion of the purchase order line will unlink sales order from this purchase order. Do you want to continue?";

        public const string POOrderLinkedToServiceOrder = "Deletion of the purchase order will unlink service order from this purchase order. Do you want to continue?";
        public const string POOrderLinkedToSalesOrder = "Deletion of the purchase order will unlink sales order from this purchase order. Do you want to continue?";

        public const string POReceiptQtyUpdateNotAllowed = "Line is linked with Sales/Service Order. Receipt quantity can not be more than ordered quantity for Special Order Item.";

        public const string POOrderQtyUpdateNotAllowedForRecvd = "Some quantities are received. Updating Order Quantity is not allowed.";
        public const string POOrderCostUpdateNotAllowedForRecvd = "Some quantities are received. Updating Cost is not allowed.";
        public const string POOrderQtyUpdateNotAllowed = "Appointment is scheduled. Updating Order Qty is not allowed.";
        public const string POOrderCostUpdateNotAllowed = "Appointment is scheduled. Updating Cost is not allowed.";

        public const string UnLinkPOOrderLineLinkedToServiceOrderLine = "Current purchase order line will be delinked from service order. Do you want to continue?";
        public const string UnLinkPOOrderLineLinkedToSalesOrderLine = "Current purchase order line will be delinked from sales order. Do you want to continue?";

        public const string AddFreight = "Add Freight";
        public const string InvalidFreight = "Invalid Freight Amount.";
        public const string FreightInventoryNotSpecified = "Freight Inventory Item is not specified in Purchase Orders Preferences.";
        public const string FreightInventoryAlreadyAdded = "Freight is already added.";

        public const string ValidationProcessReturnWithOrgCost = "Purchase Return includes Special Order Item. You must select Process Return with Original Cost";
    }
}