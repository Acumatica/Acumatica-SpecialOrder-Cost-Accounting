using PX.Data;
using PX.Objects.PO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class POReceiptLineCostPXExt : PXCacheExtension<POReceiptLine>
    {
        #region UsrIsSpecialOrderItem

        public abstract class usrIsSpecialOrderItem : PX.Data.BQL.BqlBool.Field<usrIsSpecialOrderItem> { }

        /// <summary>
        /// Un-bound field - Special Order Item identifier for sepecified inventory Item.
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(POReceiptLine.inventoryID))]
        [PXFormula(typeof(Selector<POReceiptLine.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>))]
        public bool? UsrIsSpecialOrderItem { get; set; }

        #endregion
    }
}
