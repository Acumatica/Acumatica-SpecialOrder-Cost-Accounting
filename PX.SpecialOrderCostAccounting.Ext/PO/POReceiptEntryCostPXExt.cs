using PX.Data;
using PX.Objects.PO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class POReceiptEntryCostPXExt : PXGraphExtension<POReceiptEntry>
    {
        protected virtual void _(Events.FieldVerifying<POReceiptLine, POReceiptLine.receiptQty> e, PXFieldVerifying BaseInvoke) 
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            POReceiptLine data = e.Row;
            if (data == null) { return; }

            decimal? dRctQty = (decimal?)e.NewValue;
            decimal? dRctOldQty = (decimal?)e.OldValue;

            if (dRctOldQty != dRctQty)
            { 
                POLine poLinedata = PXSelectReadonly<POLine,
                                    Where<POLine.orderType, Equal<Required<POLine.orderType>>,
                                        And<POLine.orderNbr, Equal<Required<POLine.orderNbr>>,
                                        And<POLine.lineNbr, Equal<Required<POLine.lineNbr>>>>>>.
                                        Select(Base, data.POType, data.PONbr, data.POLineNbr);
            
                if ((poLinedata?.LineType == POLineType.GoodsForDropShip ||
                         poLinedata?.LineType == POLineType.GoodsForSalesOrder ||
                         poLinedata?.LineType == POLineType.GoodsForServiceOrder) && (poLinedata?.OrderQty != dRctQty))
                {
                    throw new PXSetPropertyException<POReceiptLine.receiptQty>(Messages.POReceiptQtyUpdateNotAllowed, PXErrorLevel.Error);
                }
            }
        }
    }
}
