using System;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CR.Extensions.CRCreateSalesOrder;
using PX.Objects.IN;
using PX.Objects.SO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class OpportunityMaintCostPXExt : PXGraphExtension<OpportunityMaint.CRCreateSalesOrderExt, OpportunityMaint>
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

        public delegate void BaseDoCreateSalesOrder();

        [PXOverride]
        public virtual void DoCreateSalesOrder(BaseDoCreateSalesOrder BaseInvoke)
        {
            PXGraph.InstanceCreated.AddHandler<SOOrderEntry>((graph) =>
            {
                graph.RowUpdated.AddHandler<SOLine>((cache, eArgs) =>
                {
                    var soLine = (SOLine)eArgs.Row;
                    InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<SOLine.inventoryID>(cache, soLine);
                    if (item != null)
                    {
                        InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);
                        if (itemExt.UsrIsSpecialOrderItem.GetValueOrDefault())
                        {
                            CROpportunityProducts oppLine = PXResult<CROpportunityProducts>.Current;
                            cache.SetValueExt<SOLine.curyUnitCost>(soLine, oppLine.CuryUnitCost);
                        }
                    }
                });
            });

            BaseInvoke();
        }
    }
}