using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO;
using System;
using System.Collections;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class SOOrderEntryCostPXExt : PXGraphExtension<SOOrderEntry>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

        public override void Initialize()
        {
            PXUIFieldAttribute.SetVisible<SOLine.curyUnitCost>(Base.Transactions.Cache, null, true);
        }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDefault(typeof(Switch<Case<Where<Selector<SOLine.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>, IsNotNull>,
                                             Selector<SOLine.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>>, False>))]
        [PXFormula(typeof(Default<SOLine.inventoryID>))]
        protected virtual void _(Events.CacheAttached<SOLine.pOCreate> e) { }

        protected virtual void _(Events.FieldDefaulting<SOLine.pOCreate> e, PXFieldDefaulting BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            SOLine row = (SOLine)e.Row;
            if (row == null) { return; }
            bool bCanApply = (Base.soordertype.Current.RequireShipping == true && row.TranType != INDocType.Undefined && 
                              row.Operation == SOOperation.Issue);
            if (!bCanApply) 
            {
                e.NewValue = false;
                e.Cancel = true; 
            }
        }

        protected virtual void _(Events.RowSelected<SOLine> e, PXRowSelected BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            SOLine soline = e.Row;
            if (soline == null) { return; }

            SOLineCostPXExt solineExt = PXCache<SOLine>.GetExtension<SOLineCostPXExt>(soline);

            bool isPOCreateAndSpecialOrder = (soline.POCreate.GetValueOrDefault(false) && solineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false));
            bool isPOCreated = soline.POCreated.GetValueOrDefault(false);

            if (solineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false))
            {
                PXUIFieldAttribute.SetEnabled<SOLine.curyUnitCost>(Base.Transactions.Cache, soline, isPOCreateAndSpecialOrder && !isPOCreated);
            }

            PXUIFieldAttribute.SetEnabled<SOLine.orderQty>(Base.Transactions.Cache, soline, !isPOCreated);
        }

        #region Ref Link to PO

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [PXFormula(typeof(PXExtPOLinkFromSO<SOLine.pOCreated, SOLine.lineNbr, SOLine.orderType, SOLine.orderNbr>))]
        public void _(Events.CacheAttached<SOLineCostPXExt.usrPOLinkRef> e) { }

        public PXAction<SOOrder> ViewLinkedPORef;

        [PXUIField(DisplayName = PX.Objects.PO.Messages.ViewDemand,
                   MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXLookupButton]
        public virtual IEnumerable viewLinkedPORef(PXAdapter adapter)
        {
            SOLine row = Base.Transactions.Current;
            if (row == null) { return adapter.Get(); }

            if (String.IsNullOrEmpty(row.LineType)) { return adapter.Get(); }

            SOLineCostPXExt rowExt = row.GetExtension<SOLineCostPXExt>();
            if (String.IsNullOrEmpty(rowExt.UsrPOLinkRef)) { return adapter.Get(); }
            var linkInfo = rowExt.UsrPOLinkRef.Split(new char[] { '-' }, 2);
            if (linkInfo.Length != 2) { return adapter.Get(); }

            POOrderEntry poGraph = PXGraph.CreateInstance<POOrderEntry>();
            poGraph.Document.Current = poGraph.Document.Search<POOrder.orderNbr>(linkInfo[1], linkInfo[0]);
            if (poGraph.Document.Current != null)
            {
                throw new PXRedirectRequiredException(poGraph, true, "View Purchase Order") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }

            return adapter.Get();
        }

        #endregion
    }
}