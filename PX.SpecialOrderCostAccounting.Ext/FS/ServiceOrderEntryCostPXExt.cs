using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.FS;
using PX.Objects.IN;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class ServiceOrderEntryCostPXExt : PXGraphExtension<ServiceOrderEntry>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDefault(typeof(Switch<Case<Where<Selector<FSSODet.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>, IsNotNull>,
                                            Selector<FSSODet.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>>, False>))]
        [PXFormula(typeof(Default<FSSODet.inventoryID>))]
        protected virtual void _(Events.CacheAttached<FSSODet.manualCost> e) { }

        protected virtual void _(Events.FieldDefaulting<FSSODet, FSSODet.enablePO> e, PXFieldDefaulting BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            InventoryItem data = (InventoryItem)PXSelectorAttribute.Select<FSSODet.inventoryID>(e.Cache, e.Row);
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

        protected virtual void _(Events.RowSelected<FSSODet> e, PXRowSelected BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            FSSODet fsoline = e.Row;
            if (fsoline == null) { return; }

            FSSODetCostPXExt fsolineExt = PXCache<FSSODet>.GetExtension<FSSODetCostPXExt>(fsoline);

            if (fsolineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false) && !String.IsNullOrEmpty(fsoline.POSource))
            {
                // Disable editing if PO is created or there's appointment
                bool bEnable = (String.IsNullOrEmpty(fsoline.PONbr) && (fsoline.ApptCntr.GetValueOrDefault(0) == 0));
                PXUIFieldAttribute.SetEnabled<FSSODet.curyUnitCost>(Base.ServiceOrderDetails.Cache, fsoline, bEnable);
                PXUIFieldAttribute.SetEnabled<FSSODet.estimatedQty>(Base.ServiceOrderDetails.Cache, fsoline, bEnable);
            }
        }
    }
}