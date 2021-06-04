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
        public delegate void BasePostInvoiceDirectLines(INIssueEntry docgraph, ARInvoice invoice, DocumentList<INRegister> list);

        [PXOverride]
        public virtual void PostInvoiceDirectLines(INIssueEntry docgraph, ARInvoice invoice, DocumentList<INRegister> list,
                                                    BasePostInvoiceDirectLines BaseInvoke)
        {
            //ServiceManagement Billing via ServiceOrder/Appointment
            docgraph.RowInserting.AddHandler<INTran>((sender, e) =>
            {
                INTran data = (INTran)e.Row;

                InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<INTran.inventoryID>(docgraph.transactions.Cache, data);
                InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);

                if (item.ValMethod == INValMethod.Average && itemExt.UsrIsSpecialOrderItem.GetValueOrDefault(false) &&
                    data.DocType == INDocType.Issue && data.ARDocType == ARDocType.Invoice)
                {
                    ARTran arData = PXSelect<ARTran, Where<ARTran.tranType, Equal<Required<ARTran.tranType>>,
                                                        And<ARTran.refNbr, Equal<Required<ARTran.refNbr>>,
                                                        And<ARTran.lineNbr, Equal<Required<ARTran.lineNbr>>>>>>.
                                                        Select(Base, data.ARDocType, data.ARRefNbr, data.ARLineNbr);
                    if (arData != null)
                    {
                        FSxARTran arDataExt = PXCache<ARTran>.GetExtension<FSxARTran>(arData);

                        PXResult<POLine, FSSODet> datainfo = (PXResult<POLine, FSSODet>)
                                                                 PXSelectJoin<POLine, InnerJoin<FSSODet, On<FSSODet.poLineNbr, Equal<POLine.lineNbr>,
                                                                                        And<FSSODet.poType, Equal<POLine.orderType>,
                                                                                        And<FSSODet.poNbr, Equal<POLine.orderNbr>>>>>,
                                                                                      Where<FSSODet.sODetID, Equal<Required<FSSODet.sODetID>>,
                                                                                        And<FSSODet.sOID, Equal<Required<FSSODet.sOID>>>>>.
                                                                                        Select(Base, arDataExt.SODetID, arDataExt.SOID);
                        if (datainfo == null) { return; }

                        POLine poData = datainfo;
                        FSSODet fssoData = datainfo;

                        INTranCostPXExt dataExt = PXCache<INTran>.GetExtension<INTranCostPXExt>(data);
                        dataExt.UsrSpecialOrderCost = true;
                        data.UnitCost = poData.UnitCost;

                        // PO Source is Service Order 
                        if (fssoData.POSource == ListField_FSPOSource.PurchaseToServiceOrder)
                        {
                            //Copy Line service order line notes to issue document line
                            PXNoteAttribute.CopyNoteAndFiles(docgraph.Caches[typeof(FSSODet)], fssoData, sender, data, true, false);
                        }
                        else if (fssoData.POSource == ListField_FSPOSource.PurchaseToAppointment)
                        {
                            FSAppointmentDet fssoAptData = PXSelect<FSAppointmentDet,
                                                                Where<FSAppointmentDet.appDetID, Equal<Required<FSAppointmentDet.appDetID>>,
                                                                    And<FSAppointmentDet.appointmentID, Equal<Required<FSAppointmentDet.appointmentID>>>>>.
                                                                    SelectWindowed(Base, 0, 1, arDataExt.AppDetID, arDataExt.AppointmentID);
                            if (fssoAptData != null)
                            {
                                //Copy Appointment line notes to issue document line
                                PXNoteAttribute.CopyNoteAndFiles(docgraph.Caches[typeof(FSAppointmentDet)], fssoAptData, sender, data, true, false);
                            }
                        }
                    }
                }
            });
            BaseInvoke(docgraph, invoice, list);
        }
    }
}