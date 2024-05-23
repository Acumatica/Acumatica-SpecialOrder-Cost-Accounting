using PX.Data;
using PX.Objects.IN;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Objects.CM;
using PX.Objects.IN.InventoryRelease;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class INReleaseProcessPXExt : PXGraphExtension<INReleaseProcess>
    {
        public static bool IsActive() => true;

        public delegate void BaseIssueCost(INCostStatus layer, INTran tran, INTranSplit split, InventoryItem item, ref decimal QtyUnCosted);

        [PXOverride]
        public virtual void IssueCost(INCostStatus layer, INTran tran, INTranSplit split, InventoryItem item, ref decimal QtyUnCosted,
                                      BaseIssueCost BaseInvoke)
        {
            //Modify layer unit cost if special order item and valuation is average
            //And Vendor Cost is used (UsrSpecialOrderCost)
            //Transaction is for Issue of invoice
            InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);
            if (item.ValMethod == INValMethod.Average && itemExt?.UsrIsSpecialOrderItem == true &&
                tran.DocType == INDocType.Issue && (tran.ARDocType == ARDocType.Invoice || tran.SOLineType == SOLineType.Inventory))
            {
                INTranCostPXExt tranExt = PXCache<INTran>.GetExtension<INTranCostPXExt>(tran);
                if (tranExt.UsrSpecialOrderCost.GetValueOrDefault(false))
                {
                    layer.UnitCost = tran.UnitCost;
                    if (PXCurrencyAttribute.IsNullOrEmpty(tran.UnitCost))
                    {
                        tran.TranCost = 0.00M;
                    }
                }
            }
            BaseInvoke(layer, tran, split, item, ref QtyUnCosted);

            //Reset unitcost and trancost if zero
            if ((PXCurrencyAttribute.IsNullOrEmpty(tran.UnitCost)) &&
                (item.ValMethod == INValMethod.Average && itemExt?.UsrIsSpecialOrderItem == true &&
                tran.DocType == INDocType.Issue && (tran.ARDocType == ARDocType.Invoice || tran.SOLineType == SOLineType.Inventory)))
            {
                INTranCostPXExt tranExt = PXCache<INTran>.GetExtension<INTranCostPXExt>(tran);
                if (tranExt.UsrSpecialOrderCost.GetValueOrDefault(false))
                {
                    layer.UnitCost = tran.UnitCost;
                    if (PXCurrencyAttribute.IsNullOrEmpty(tran.UnitCost))
                    {
                        tran.TranCost = 0.00M;
                    }
                }
            }
        }

        public delegate void BaseTransferCost(INTran tran, INTranSplit split, InventoryItem item, INCostStatus issueCost, INTranCost issueTranCost, decimal issuedQty, decimal issuedCost);

        [PXOverride]
        public virtual void TransferCost(INTran tran, INTranSplit split, InventoryItem item, INCostStatus issueCost, 
                                         INTranCost issueTranCost, decimal issuedQty, decimal issuedCost,
                                         BaseTransferCost BaseInvoke)
        {
            InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);
            if ((PXCurrencyAttribute.IsNullOrEmpty(tran.UnitCost)) &&
               (item.ValMethod == INValMethod.Average && itemExt?.UsrIsSpecialOrderItem == true &&
               tran.DocType == INDocType.Issue && (tran.ARDocType == ARDocType.Invoice || tran.SOLineType == SOLineType.Inventory)))
            {
                INTranCostPXExt tranExt = PXCache<INTran>.GetExtension<INTranCostPXExt>(tran);
                if (tranExt.UsrSpecialOrderCost.GetValueOrDefault(false))
                {
                    //Reverse accumulation
                    issueCost.TotalCost += (issuedCost * -1);
                    tran.TranCost = 0.00M;
                    issueTranCost.TranCost = 0.00M;
                }
            }

            BaseInvoke(tran, split, item, issueCost, issueTranCost, issuedQty, issuedCost);
        }
    }
}