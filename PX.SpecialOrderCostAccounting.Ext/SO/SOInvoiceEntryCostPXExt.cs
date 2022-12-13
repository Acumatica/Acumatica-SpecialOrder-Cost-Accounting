using System;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.IN;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.PO;
using PX.Objects.FS;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class SOInvoiceEntryCostPXExt : PXGraphExtension<SOInvoiceEntry>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

        public delegate void BasePostInvoiceDirectLines(INIssueEntry docgraph, ARInvoice invoice, DocumentList<INRegister> list);

        [PXOverride]
        public virtual void PostInvoiceDirectLines(INIssueEntry docgraph, ARInvoice invoice, DocumentList<INRegister> list,
                                                    BasePostInvoiceDirectLines BaseInvoke)
        {
            //ServiceManagement Billing via ServiceOrder/Appointment OR direct Credit Memo for Invoice
            docgraph.RowInserting.AddHandler<INTran>((sender, e) =>
            {
                INTran data = (INTran)e.Row;

                InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<INTran.inventoryID>(docgraph.transactions.Cache, data);
                InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);

                if (item.ValMethod == INValMethod.Average && itemExt.UsrIsSpecialOrderItem.GetValueOrDefault(false) &&
                    data.DocType == INDocType.Issue && (data.ARDocType == ARDocType.Invoice || data.ARDocType == ARDocType.CreditMemo))
                {
                    ARTran arData = PXSelect<ARTran, Where<ARTran.tranType, Equal<Required<ARTran.tranType>>,
                                                        And<ARTran.refNbr, Equal<Required<ARTran.refNbr>>,
                                                        And<ARTran.lineNbr, Equal<Required<ARTran.lineNbr>>>>>>.
                                                        Select(Base, data.ARDocType, data.ARRefNbr, data.ARLineNbr);
                    
                    if (data?.ARDocType == ARDocType.CreditMemo)
                    {
                        arData = ARTran.PK.Find(Base, arData?.OrigInvoiceType, arData?.OrigInvoiceNbr, arData?.OrigInvoiceLineNbr);
                    }

                    if (arData != null)
                    {
                        FSARTran arDataExt = FSARTran.PK.Find(Base, arData.TranType, arData.RefNbr, arData.LineNbr);

                        if (arDataExt != null)
                        {
                            PXResult<POLine, FSSODet> datainfo = (PXResult<POLine, FSSODet>)
                                                                     PXSelectJoin<POLine, InnerJoin<FSSODet, On<FSSODet.poLineNbr, Equal<POLine.lineNbr>,
                                                                                            And<FSSODet.poType, Equal<POLine.orderType>,
                                                                                            And<FSSODet.poNbr, Equal<POLine.orderNbr>>>>>,
                                                                                          Where<FSSODet.lineNbr, Equal<Required<FSSODet.lineNbr>>,
                                                                                            And<FSSODet.srvOrdType, Equal<Required<FSSODet.srvOrdType>>,
                                                                                            And<FSSODet.refNbr, Equal<Required<FSSODet.refNbr>>>>>>.
                                                                                            Select(Base, arDataExt.ServiceOrderLineNbr, arDataExt.SrvOrdType,
                                                                                            arDataExt.ServiceOrderRefNbr);

                            // Direct Credit Memo for Invoice with sales order
                            if (datainfo == null && data?.ARDocType == ARDocType.CreditMemo) 
                            {
                                INTranCostPXExt indataExt = PXCache<INTran>.GetExtension<INTranCostPXExt>(data);
                                indataExt.UsrSpecialOrderCost = true;
                                data.UnitCost = arData.UnitCost;

                                return; 
                            }

                            POLine poData = datainfo;
                            FSSODet fssoData = datainfo;

                            // If Special Order Item is not linked to PO
                            if (datainfo == null) { return; }

                            INTranCostPXExt dataExt = PXCache<INTran>.GetExtension<INTranCostPXExt>(data);
                            dataExt.UsrSpecialOrderCost = true;
                            data.UnitCost = poData.UnitCost;

                            FSServiceOrder srvOrder = FSSODet.FK.ServiceOrder.FindParent(Base, fssoData);

                            // Billing is via Service Order 
                            if (srvOrder.BillingBy == ID.Billing_By.SERVICE_ORDER)
                            {
                                //Copy Line service order line notes to issue document line
                                PXNoteAttribute.CopyNoteAndFiles(docgraph.Caches[typeof(FSSODet)], fssoData, sender, data, true, false);
                            }
                            // Billing is via Appointment
                            else if (srvOrder.BillingBy == ID.Billing_By.APPOINTMENT)
                            {
                                FSAppointmentDet fssoAptData = PXSelect<FSAppointmentDet,
                                                                    Where<FSAppointmentDet.refNbr, Equal<Required<FSAppointmentDet.refNbr>>,
                                                                        And<FSAppointmentDet.lineNbr, Equal<Required<FSAppointmentDet.lineNbr>>>>>.
                                                                        SelectWindowed(Base, 0, 1, arDataExt.AppointmentRefNbr, arDataExt.AppointmentLineNbr);
                                if (fssoAptData != null)
                                {
                                    //Copy Appointment line notes to issue document line
                                    PXNoteAttribute.CopyNoteAndFiles(docgraph.Caches[typeof(FSAppointmentDet)], fssoAptData, sender, data, true, false);
                                }
                            }
                        }
                    }
                }
            });
            BaseInvoke(docgraph, invoice, list);
        }
    }
}