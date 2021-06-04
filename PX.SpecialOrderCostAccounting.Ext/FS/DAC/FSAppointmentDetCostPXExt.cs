using PX.Data;
using PX.Objects.FS;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public sealed class FSAppointmentDetCostPXExt : PXCacheExtension<FSAppointmentDet>
    {
        #region UsrIsSpecialOrderItem

        public abstract class usrIsSpecialOrderItem : PX.Data.BQL.BqlBool.Field<usrIsSpecialOrderItem> { }

        /// <summary>
        /// Un-bound field - Special Order Item identifier for sepecified inventory Item.
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(FSAppointmentDet.inventoryID))]
        [PXFormula(typeof(Selector<FSAppointmentDet.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>))]
        public bool? UsrIsSpecialOrderItem { get; set; }

        #endregion
    }
}