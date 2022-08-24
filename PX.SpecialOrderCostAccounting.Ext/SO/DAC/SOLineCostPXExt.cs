using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public sealed class SOLineCostPXExt : PXCacheExtension<SOLine>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

        #region UsrIsSpecialOrderItem

        public abstract class usrIsSpecialOrderItem : PX.Data.BQL.BqlBool.Field<usrIsSpecialOrderItem> { }

        /// <summary>
        /// Un-bound field - Special Order Item identifier for sepecified inventory Item.
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(SOLine.inventoryID))]
        [PXFormula(typeof(Selector<SOLine.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>))]
        public bool? UsrIsSpecialOrderItem { get; set; }

        #endregion

        #region UsrPOLinkRef
        public abstract class usrPOLinkRef : PX.Data.BQL.BqlString.Field<usrPOLinkRef> { }

        [PXString]
        [PXUIField(DisplayName = "Purchase Order #", Enabled = false)]
        public string UsrPOLinkRef { get; set; }
        #endregion
    }
}