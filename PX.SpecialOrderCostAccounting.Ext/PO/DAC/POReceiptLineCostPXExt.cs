using PX.Data;
using PX.Objects.CS;
using PX.Objects.PO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public sealed class POReceiptLineCostPXExt : PXCacheExtension<POReceiptLine>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

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
