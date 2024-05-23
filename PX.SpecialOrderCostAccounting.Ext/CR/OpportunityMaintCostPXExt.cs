using PX.Data;
using PX.Objects.CR;
using PX.Objects.IN;
using PX.Objects.SO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class OpportunityMaintCostPXExt : PXGraphExtension<OpportunityMaint.CRCreateSalesOrderExt, OpportunityMaint>
    {
        public static bool IsActive() => OpportunityMaint.CRCreateSalesOrderExt.IsActive();

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
                graph.FieldUpdated.AddHandler<SOLine.orderQty>((cache, eArgs) =>
                {
                    var soLine = (SOLine)eArgs.Row;
                    InventoryItem item = InventoryItem.PK.Find(graph, soLine.InventoryID);
                    if (item != null)
                    {
                        InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);
                        if (itemExt.UsrIsSpecialOrderItem.GetValueOrDefault())
                        {
                            CROpportunityProducts oppLine = PXResult<CROpportunityProducts>.Current;
                            cache.SetValueExt<SOLine.unitCost>(soLine, oppLine.UnitCost);
                        }
                    }
                });
            });

            BaseInvoke();
        }
    }
}