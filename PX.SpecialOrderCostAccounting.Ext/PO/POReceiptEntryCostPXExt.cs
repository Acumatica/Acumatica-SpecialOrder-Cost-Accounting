using System;
using System.Linq;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
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

        public delegate void BaseReleaseReturn(INIssueEntry docgraph, PX.Objects.AP.APInvoiceEntry invoiceGraph, POReceipt aDoc, DocumentList<INRegister> aCreated, DocumentList<PX.Objects.AP.APInvoice> aInvoiceCreated, bool aIsMassProcess);

        [PXOverride]
        public virtual void ReleaseReturn(INIssueEntry docgraph, PX.Objects.AP.APInvoiceEntry invoiceGraph, POReceipt aDoc, DocumentList<INRegister> aCreated, DocumentList<PX.Objects.AP.APInvoice> aInvoiceCreated, bool aIsMassProcess,
                                          BaseReleaseReturn BaseInvoke)
        {
            if (aDoc.ReturnOrigCost != true)
            {
                bool? bSpecialOrder = PXSelectReadonly<POReceiptLine,
                                        Where<POReceiptLine.receiptType, Equal<Required<POReceiptLine.receiptType>>,
                                            And<POReceiptLine.receiptNbr, Equal<Required<POReceiptLine.receiptNbr>>>>>.
                                            Select(Base, aDoc.ReceiptType, aDoc.ReceiptNbr).
                                            RowCast<POReceiptLine>()?.Any(x => x.GetExtension<POReceiptLineCostPXExt>().UsrIsSpecialOrderItem == true);
                if (bSpecialOrder == true)
                {
                    throw new PXException(Messages.ValidationProcessReturnWithOrgCost);
                }
            }
            BaseInvoke(docgraph, invoiceGraph, aDoc, aCreated, aInvoiceCreated, aIsMassProcess);
        }
    }
}