using System;
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

            if (fsolineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false) && (fsoline.POSource == ListField_FSPOSource.PurchaseToAppointment))
            {
                // Disable editing if PO is created and is Purchase to Order
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
    }
}