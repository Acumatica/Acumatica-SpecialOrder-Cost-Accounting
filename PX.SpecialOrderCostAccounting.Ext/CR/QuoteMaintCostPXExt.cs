using System;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.IN;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class QuoteMaintCostPXExt : PXGraphExtension<QuoteMaint>
    {
        protected virtual void _(Events.FieldUpdated<CROpportunityProducts.inventoryID> e, PXFieldUpdated BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }
            if (e.Row == null) { return; }

            InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<CROpportunityProducts.inventoryID>(Base.Products.Cache, e.Row);
            if (item == null) { return; }
            InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);

            if (itemExt.UsrIsSpecialOrderItem.GetValueOrDefault(false))
            {
                e.Cache.SetValueExt<CROpportunityProducts.pOCreate>(e.Row, itemExt.UsrIsSpecialOrderItem);
            }
        }
    }
}