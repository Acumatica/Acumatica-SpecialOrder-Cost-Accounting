using System;
using System.Linq;
using PX.Data;
using PX.Objects.FS;
using PX.Objects.IN;
using PX.Objects.SO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class AppointmentEntryCostPXExt : PXGraphExtension<AppointmentEntry>
    {
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDefault(typeof(Switch<Case<Where<Selector<FSAppointmentDet.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>, IsNotNull>,
                                            Selector<FSAppointmentDet.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>>, False>))]
        [PXFormula(typeof(Default<FSAppointmentDet.inventoryID>))]
        protected virtual void _(Events.CacheAttached<FSAppointmentDet.manualCost> e) { }

        protected virtual void _(Events.FieldDefaulting<FSAppointmentDet, FSAppointmentDet.enablePO> e, PXFieldDefaulting BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            InventoryItem data = (InventoryItem)PXSelectorAttribute.Select<FSAppointmentDet.inventoryID>(e.Cache, e.Row);
            if (data != null)
            {
                InventoryItemCostPXExt dataExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(data);
                if (dataExt.UsrIsSpecialOrderItem.GetValueOrDefault(false))
                {
                    e.NewValue = dataExt.UsrIsSpecialOrderItem.GetValueOrDefault(false);
                    e.Cancel = true;
                }
            }
        }

        protected virtual void _(Events.RowSelected<FSAppointmentDet> e, PXRowSelected BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            FSAppointmentDet fsoline = e.Row;
            if (fsoline == null) { return; }

            FSAppointmentDetCostPXExt fsolineExt = PXCache<FSAppointmentDet>.GetExtension<FSAppointmentDetCostPXExt>(fsoline);

            if (fsolineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false))
            {
                // Disable editing if PO is created
                PXUIFieldAttribute.SetEnabled<FSAppointmentDet.curyUnitCost>(Base.AppointmentDetails.Cache, fsoline, (String.IsNullOrEmpty(fsoline.PONbr)));
                PXUIFieldAttribute.SetEnabled<FSAppointmentDet.estimatedQty>(Base.AppointmentDetails.Cache, fsoline, (String.IsNullOrEmpty(fsoline.PONbr)));
                PXUIFieldAttribute.SetEnabled<FSAppointmentDet.actualQty>(Base.AppointmentDetails.Cache, fsoline, (String.IsNullOrEmpty(fsoline.PONbr)));
            }
        }

        public delegate void BaseVerifySrvOrdLineQty(PXCache cache, FSAppointmentDet apptLine, object newValue, Type QtyField, bool runningFieldVerifying);

        [PXOverride]
        public virtual void VerifySrvOrdLineQty(PXCache cache, FSAppointmentDet apptLine, object newValue, Type QtyField, bool runningFieldVerifying, BaseVerifySrvOrdLineQty BaseInvoke)
        {
            if (apptLine != null)
            {
                FSAppointmentDetCostPXExt fsolineExt = PXCache<FSAppointmentDet>.GetExtension<FSAppointmentDetCostPXExt>(apptLine);
                if (fsolineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false) && (apptLine.POSource == ListField_FSPOSource.PurchaseToAppointment))
                {
                    return;
                }
            }
            BaseInvoke(cache, apptLine, newValue, QtyField, runningFieldVerifying);
        }

        public delegate void BaseInsertUpdateSODet(PXCache cacheAppointmentDet, FSAppointmentDet fsAppointmentDetRow, PXSelectBase<FSSODet> viewSODet, FSAppointment apptRow);

        [PXOverride]
        public virtual void InsertUpdateSODet(PXCache cacheAppointmentDet, FSAppointmentDet fsAppointmentDetRow, PXSelectBase<FSSODet> viewSODet, FSAppointment apptRow, BaseInsertUpdateSODet BaseInvoke)
        {
            PXEntryStatus lineStatus = cacheAppointmentDet.GetStatus(fsAppointmentDetRow);
            if ((lineStatus == PXEntryStatus.Inserted || lineStatus == PXEntryStatus.Updated) && 
                fsAppointmentDetRow?.SODetID != null && (fsAppointmentDetRow.POSource == ListField_FSPOSource.PurchaseToAppointment))
            {
                FSSODet fsSODetRow = FSSODet.UK.Find(viewSODet.Cache.Graph, fsAppointmentDetRow.SODetID);

                if (fsAppointmentDetRow.EstimatedQty != fsSODetRow?.EstimatedQty)
                {
                    FSAppointmentDetCostPXExt fsolineExt = PXCache<FSAppointmentDet>.GetExtension<FSAppointmentDetCostPXExt>(fsAppointmentDetRow);
                    if (fsolineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false))
                    {
                        ServiceOrderEntry srvGraph = (ServiceOrderEntry)viewSODet.Cache.Graph;

                        if (srvGraph != null)
                        {
                            srvGraph.ServiceOrderDetails.Current = fsSODetRow;
                            var fsSplits = srvGraph.Splits.Select().RowCast<FSSODetSplit>().Where(x => x.LineNbr == fsSODetRow.LineNbr &&
                                                                                                       x.SrvOrdType == fsSODetRow.SrvOrdType &&
                                                                                                       x.RefNbr == fsSODetRow.RefNbr);
                            foreach (FSSODetSplit fssplit in fsSplits)
                            {
                                srvGraph.Splits.Current = fssplit;
                                srvGraph.Splits.Current.Qty = fsAppointmentDetRow.EstimatedQty;
                                srvGraph.Splits.Current = srvGraph.Splits.Update(srvGraph.Splits.Current);
                            }
                        }
                    }
                }
            }

            BaseInvoke(cacheAppointmentDet, fsAppointmentDetRow, viewSODet, apptRow);
        }
    }
}