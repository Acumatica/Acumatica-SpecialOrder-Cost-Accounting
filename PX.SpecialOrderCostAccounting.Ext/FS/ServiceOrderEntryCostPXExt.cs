using PX.Data;
using PX.Objects.FS;
using PX.Objects.IN;
using System;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class ServiceOrderEntryCostPXExt : PXGraphExtension<ServiceOrderEntry>
    {
        /// <summary>
        /// Assign Value from UsrIsSpecialOrderItem.
        /// </summary>
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDefault(typeof(Switch<Case<Where<Selector<FSSODet.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>, IsNotNull>,
                                            Selector<FSSODet.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>>, False>))]
        [PXFormula(typeof(Default<FSSODet.inventoryID>))]
        protected virtual void _(Events.CacheAttached<FSSODet.enablePO> e) { }

        protected virtual void _(Events.RowSelected<FSSODet> e, PXRowSelected BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            FSSODet fsoline = e.Row;
            if (fsoline == null) { return; }

            FSSODetCostPXExt fsolineExt = PXCache<FSSODet>.GetExtension<FSSODetCostPXExt>(fsoline);

            if (fsolineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false) && (fsoline.POSource == INReplenishmentSource.PurchaseToOrder))
            {
                // Disable editing if PO is created and is Purchase to Order
                PXUIFieldAttribute.SetEnabled<FSSODet.curyUnitCost>(Base.ServiceOrderDetails.Cache, fsoline, (String.IsNullOrEmpty(fsoline.PONbr)));
                PXUIFieldAttribute.SetEnabled<FSSODet.estimatedQty>(Base.ServiceOrderDetails.Cache, fsoline, (String.IsNullOrEmpty(fsoline.PONbr)));
            }
        }
    }
}