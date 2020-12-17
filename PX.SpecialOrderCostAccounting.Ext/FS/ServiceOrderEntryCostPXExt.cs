﻿using PX.Data;
using PX.Objects.FS;
using PX.Objects.IN;
using PX.Objects.SO;
using System;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class ServiceOrderEntryCostPXExt : PXGraphExtension<ServiceOrderEntry>
    {
        protected virtual void _(Events.FieldDefaulting<FSSODet, FSSODet.enablePO> e, PXFieldDefaulting BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            InventoryItem data = (InventoryItem)PXSelectorAttribute.Select<FSSODet.inventoryID>(e.Cache, e.Row);
            if (data != null)
            {
                InventoryItemCostPXExt dataExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(data);
                if (dataExt.UsrIsSpecialOrderItem.GetValueOrDefault(false))
                {
                    e.NewValue = dataExt.UsrIsSpecialOrderItem.GetValueOrDefault(false);
                    e.Cancel = true;
                }
            }
        }

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