using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class POReceiptEntryCostPXExt : PXGraphExtension<POReceiptEntry>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

        protected virtual void _(Events.FieldVerifying<POReceiptLine, POReceiptLine.receiptQty> e, PXFieldVerifying BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            POReceiptLine data = e.Row;
            if (data == null) { return; }

            POReceiptLineCostPXExt dataExt = PXCache<POReceiptLine>.GetExtension<POReceiptLineCostPXExt>(data);

            if (dataExt?.UsrIsSpecialOrderItem != true) { return; }

            decimal? dRctQty = (decimal?)e.NewValue;
            decimal? dRctOldQty = (decimal?)e.OldValue;
            bool isNewRec = (e.Cache.GetStatus(data).IsIn(PXEntryStatus.Inserted, PXEntryStatus.InsertedDeleted));
            decimal? dOrigRctQty = (!isNewRec) ? POReceiptLine.PK.Find(Base, data)?.ReceiptQty.GetValueOrDefault(0) : 0m;

            if (dRctOldQty != dRctQty && data.ReceiptType == POReceiptType.POReceipt)
            {
                POLine poLinedata = PXSelectReadonly<POLine,
                                    Where<POLine.orderType, Equal<Required<POLine.orderType>>,
                                        And<POLine.orderNbr, Equal<Required<POLine.orderNbr>>,
                                        And<POLine.lineNbr, Equal<Required<POLine.lineNbr>>>>>>.
                                        Select(Base, data.POType, data.PONbr, data.POLineNbr);

                // Restrict receiving more than ordered
                if ((poLinedata?.LineType == POLineType.GoodsForDropShip ||
                     poLinedata?.LineType == POLineType.GoodsForSalesOrder ||
                     poLinedata?.LineType == POLineType.GoodsForServiceOrder))
                {
                    if ((dRctQty.GetValueOrDefault(0) + (poLinedata.ReceivedQty.GetValueOrDefault(0) - dOrigRctQty)) > 
                        (poLinedata.OrderQty.GetValueOrDefault(0)))
                    {
                        throw new PXSetPropertyException<POReceiptLine.receiptQty>(Messages.POReceiptQtyUpdateNotAllowed, PXErrorLevel.Error);
                    }
                }
            }
        }

        protected virtual void _(Events.RowSelected<POReceiptLine> e, PXRowSelected BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            POReceiptLine prline = e.Row;
            if (prline == null) { return; }

            POReceiptLineCostPXExt polineExt = PXCache<POReceiptLine>.GetExtension<POReceiptLineCostPXExt>(prline);

            if (polineExt.UsrIsSpecialOrderItem == true)
            {
                PXUIFieldAttribute.SetEnabled<POReceiptLine.curyUnitCost>(e.Cache, prline, false);
                PXUIFieldAttribute.SetEnabled<POReceiptLineSplit.qty>(Base.splits.Cache, null, false);
            }
        }

        public delegate void BaseReleaseReturn(INIssueEntry docgraph, PX.Objects.AP.APInvoiceEntry invoiceGraph, POReceipt aDoc, DocumentList<INRegister> aCreated, DocumentList<PX.Objects.AP.APInvoice> aInvoiceCreated, bool aIsMassProcess);

        [PXOverride]
        public virtual void ReleaseReturn(INIssueEntry docgraph, PX.Objects.AP.APInvoiceEntry invoiceGraph, POReceipt aDoc, DocumentList<INRegister> aCreated, DocumentList<PX.Objects.AP.APInvoice> aInvoiceCreated, bool aIsMassProcess,
                                          BaseReleaseReturn BaseInvoke)
        {
            if (aDoc.ReturnInventoryCostMode != ReturnCostMode.OriginalCost)
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