using PX.Data;
using PX.Objects.IN;
using PX.Objects.AR;
using PX.Objects.SO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class INReleaseProcessPXExt : PXGraphExtension<INReleaseProcess>
    {
        public delegate void BaseIssueCost(INCostStatus layer, INTran tran, INTranSplit split, InventoryItem item, ref decimal QtyUnCosted);

        [PXOverride]
        public virtual void IssueCost(INCostStatus layer, INTran tran, INTranSplit split, InventoryItem item, ref decimal QtyUnCosted,
                                      BaseIssueCost BaseInvoke)
        {
            //Modify layer unit cost if special order item and valuation is average
            //And Vendor Cost is used (UsrSpecialOrderCost)
            //Transaction is for Issue of invoice
            InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);
            if (item.ValMethod == INValMethod.Average && itemExt.UsrIsSpecialOrderItem.GetValueOrDefault(false) &&
                tran.DocType == INDocType.Issue && (tran.ARDocType == ARDocType.Invoice || tran.SOLineType == SOLineType.Inventory))
            {
                INTranCostPXExt tranExt = PXCache<INTran>.GetExtension<INTranCostPXExt>(tran);
                if (tranExt.UsrSpecialOrderCost.GetValueOrDefault(false))
                {
                    layer.UnitCost = tran.UnitCost;
                }
            }
            BaseInvoke(layer, tran, split, item, ref QtyUnCosted);
        }
    }
}