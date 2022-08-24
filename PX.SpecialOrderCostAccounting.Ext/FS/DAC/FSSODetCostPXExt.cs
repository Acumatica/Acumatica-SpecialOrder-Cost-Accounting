using PX.Data;
using PX.Objects.CS;
using PX.Objects.FS;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public sealed class FSSODetCostPXExt : PXCacheExtension<FSSODet>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();

        #region UsrIsSpecialOrderItem

        public abstract class usrIsSpecialOrderItem : PX.Data.BQL.BqlBool.Field<usrIsSpecialOrderItem> { }

        /// <summary>
        /// Un-bound field - Special Order Item identifier for sepecified inventory Item.
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(FSSODet.inventoryID))]
        [PXFormula(typeof(Selector<FSSODet.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>))]
        public bool? UsrIsSpecialOrderItem { get; set; }

        #endregion
    }
}