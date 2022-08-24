using System;
using PX.Data;
using PX.Objects.FS;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO;
using System.Collections;
using System.Linq;
using static PX.Objects.PO.POOrderEntry;
using System.Collections.Generic;
using PX.Common;
using PX.Objects.PO.GraphExtensions.POOrderEntryExt;
using PX.Objects.CS;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class POOrderEntryCostPXExt : PXGraphExtension<POOrderEntry>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

        [PXCopyPasteHiddenView]
        public PXSelect<FSSODet,
            Where<FSSODet.poType, Equal<Optional<POLine.orderType>>,
                And<FSSODet.poNbr, Equal<Optional<POLine.orderNbr>>,
                And<FSSODet.poLineNbr, Equal<Optional<POLine.lineNbr>>>>>> FixedDemandViaServiceOrderForCostUpdate;

        [PXCopyPasteHiddenView]
        public PXSelectJoin<FSAppointmentDet, InnerJoin<FSSODet, On<FSSODet.sODetID, Equal<FSAppointmentDet.sODetID>>>,
            Where<FSSODet.poType, Equal<Optional<POLine.orderType>>,
                And<FSSODet.poNbr, Equal<Optional<POLine.orderNbr>>,
                And<FSSODet.poLineNbr, Equal<Optional<POLine.lineNbr>>>>>> FixedDemandViaServiceOrderApptForCostUpdate;

        [PXCopyPasteHiddenView]
        public PXSelect<SOLine, Where<SOLine.orderType, Equal<Optional<SOLine.orderType>>,
                And<SOLine.orderNbr, Equal<Optional<SOLine.orderNbr>>,
                And<SOLine.lineNbr, Equal<Optional<SOLine.lineNbr>>>>>> SOLineLinkForCostUpdate;

        [PXCopyPasteHiddenView]
        public PXSelect<SOLineSplit, Where<SOLineSplit.pOType, Equal<Optional<POLine.orderType>>,
                                                    And<SOLineSplit.pONbr, Equal<Optional<POLine.orderNbr>>,
                                                    And<SOLineSplit.pOLineNbr, Equal<Optional<POLine.lineNbr>>>>>> SOLineSplitsForUpdate;

        [PXCopyPasteHiddenView]
        public PXFilter<AddFreightParams> AddFreightView;

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Service Order Type", Visible = true, Enabled = false)]
        protected virtual void _(Events.CacheAttached<FSSODet.srvOrdType> e) { }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = "Service Order Nbr.", Visible = true, Enabled = false)]
        [FSSelectorSORefNbr]
        protected virtual void _(Events.CacheAttached<FSSODet.refNbr> e) { }

        public delegate void BaseFillPOLineFromDemand(POLine dest, POFixedDemand demand, string OrderType, SOLineSplit3 solinesplit);

        [PXOverride]
        public virtual void FillPOLineFromDemand(POLine dest, POFixedDemand demand, string OrderType, SOLineSplit3 solinesplit,
                                                 BaseFillPOLineFromDemand BaseInvoke)
        {
            // Copy Vendor Cost from Sales Order to Purchase Order
            if (solinesplit != null && solinesplit.POCreate.GetValueOrDefault(false))
            {
                InventoryItem item = InventoryItem.PK.Find(Base, solinesplit?.InventoryID);

                if (item?.ValMethod == INValMethod.Average)
                {
                    InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);

                    if (itemExt.UsrIsSpecialOrderItem.GetValueOrDefault(false))
                    {
                        SOLine soData = SOLine.PK.Find(Base, solinesplit.OrderType, solinesplit.OrderNbr, solinesplit.LineNbr);
                        if (soData != null && 
                            (soData.POSource == INReplenishmentSource.PurchaseToOrder || soData.POSource == INReplenishmentSource.DropShipToOrder))
                        {
                            dest.CuryUnitCost = soData.CuryUnitCost;
                        }
                    }
                }
            }
            BaseInvoke(dest, demand, OrderType, solinesplit);
        }

        protected virtual void _(Events.FieldDefaulting<POLine.curyUnitCost> e, PXFieldDefaulting BaseInvoke)
        {
            POLine poline = (POLine)e.Row;
            if (poline != null)
            {
                POLineCostPXExt poLineExt = PXCache<POLine>.GetExtension<POLineCostPXExt>(poline);
                if (poLineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false) &&
                    poline.CuryUnitCost.HasValue)
                {
                    e.NewValue = poline.CuryUnitCost;
                    e.Cancel = true;
                }
                else
                {
                    BaseInvoke(e.Cache, e.Args);
                }
            }
            else
            {
                BaseInvoke(e.Cache, e.Args);
            }
        }

        protected virtual void _(Events.FieldUpdating<POLine.curyUnitCost> e, PXFieldUpdating BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            //Warn and modify cost in linked Sales/Service Order line
            POLine poline = (POLine)e.Row;
            if (poline == null) { return; }

            if (poline.CuryUnitCost == (decimal?)e.NewValue) { return; }

            POLineCostPXExt poLineExt = PXCache<POLine>.GetExtension<POLineCostPXExt>(poline);

            if (poLineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false))
            {
                if (poline.ReceivedQty.GetValueOrDefault(0) > 0m)
                {
                    e.Cancel = true;
                    throw new PXSetPropertyException<POLine.curyUnitCost>(Messages.POOrderCostUpdateNotAllowedForRecvd, PXErrorLevel.Error);
                }
            }

            if (poline.LineType == POLineType.GoodsForSalesOrder || poline.LineType == POLineType.GoodsForDropShip ||
                poline.LineType == POLineType.NonStockForDropShip || poline.LineType == POLineType.NonStockForSalesOrder)
            {
                SOLineSplit3 demandLine = (PXResult<SOLineSplit3>)Base.FixedDemand.View.SelectMultiBound(new object[] { poline }).FirstOrDefault();
                if (demandLine == null) { return; }

                SOLine solineForUpdate = (SOLine)SOLineLinkForCostUpdate.Select(demandLine.OrderType, demandLine.OrderNbr, demandLine.LineNbr);
                if (solineForUpdate == null || solineForUpdate.POSource != INReplenishmentSource.PurchaseToOrder) { return; }

                if (Base.Transactions.View.Ask(poline, PX.Objects.AP.Messages.Warning,
                                                Messages.ChangingCostForLinkedRecord, MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.No)
                {
                    e.NewValue = poline.CuryUnitCost;
                }
            }
            else if (poline.LineType == POLineType.GoodsForServiceOrder || poline.LineType == POLineType.NonStockForServiceOrder) 
            {
                FSSODet fsoLineforUpdate = (FSSODet)FixedDemandViaServiceOrderForCostUpdate.Select(poline.OrderType, poline.OrderNbr, poline.LineNbr);
                if (fsoLineforUpdate == null) { return; }

                if (fsoLineforUpdate.POSource == ListField_FSPOSource.PurchaseToServiceOrder)
                {
                    if (fsoLineforUpdate.ApptCntr.GetValueOrDefault(0) > 0)
                    {
                        throw new PXSetPropertyException<POLine.curyUnitCost>(Messages.POOrderCostUpdateNotAllowed, PXErrorLevel.Error);
                    }

                    if (Base.Transactions.View.Ask(poline, PX.Objects.AP.Messages.Warning,
                                                    Messages.ChangingCostForLinkedRecordFSO, MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.No)
                    {
                        e.NewValue = poline.CuryUnitCost;
                    }
                }
                else if (fsoLineforUpdate.POSource == ListField_FSPOSource.PurchaseToAppointment)
                {
                    if (Base.Transactions.View.Ask(poline, PX.Objects.AP.Messages.Warning,
                                                    Messages.ChangingCostForLinkedRecordFSOAppt, MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.No)
                    {
                        e.NewValue = poline.CuryUnitCost;
                    }
                }
            }
        }

        protected virtual void _(Events.FieldUpdating<POLine.orderQty> e, PXFieldUpdating BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            //Warn and modify Qty in linked Sales/Service Order line
            POLine poline = (POLine)e.Row;
            if (poline == null) { return; }

            if (poline.OrderQty == (decimal?)e.NewValue) { return; }

            POLineCostPXExt poLineExt = PXCache<POLine>.GetExtension<POLineCostPXExt>(poline);
            
            if (poLineExt.UsrIsSpecialOrderItem.GetValueOrDefault(false))
            {
                if (poline.ReceivedQty.GetValueOrDefault(0) > 0m)
                {
                    e.Cancel = true;
                    throw new PXSetPropertyException<POLine.orderQty>(Messages.POOrderQtyUpdateNotAllowedForRecvd, PXErrorLevel.Error);
                }
            }

            if (poline.LineType == POLineType.GoodsForSalesOrder || poline.LineType == POLineType.GoodsForDropShip ||
                poline.LineType == POLineType.NonStockForDropShip || poline.LineType == POLineType.NonStockForSalesOrder)
            {
                SOLineSplit3 demandLine = (PXResult<SOLineSplit3>)Base.FixedDemand.View.SelectMultiBound(new object[] { poline }).FirstOrDefault();
                if (demandLine == null) { return; }

                SOLine solineForUpdate = (SOLine)SOLineLinkForCostUpdate.Select(demandLine.OrderType, demandLine.OrderNbr, demandLine.LineNbr);
                if (solineForUpdate == null || solineForUpdate.POSource != INReplenishmentSource.PurchaseToOrder) { return; }

                if (Base.Transactions.View.Ask(poline, PX.Objects.AP.Messages.Warning,
                                                Messages.ChangingQtyForLinkedRecord, MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.No)
                {
                    e.NewValue = poline.OrderQty;
                }
            }
            else if (poline.LineType == POLineType.GoodsForServiceOrder || poline.LineType == POLineType.NonStockForServiceOrder)
            {
                FSSODet fsoLineforUpdate = (FSSODet)FixedDemandViaServiceOrderForCostUpdate.Select(poline.OrderType, poline.OrderNbr, poline.LineNbr);
                if (fsoLineforUpdate == null) { return; }

                if (fsoLineforUpdate.POSource == ListField_FSPOSource.PurchaseToServiceOrder)
                {
                    if (fsoLineforUpdate.ApptCntr.GetValueOrDefault(0) > 0)
                    {
                        throw new PXSetPropertyException<POLine.orderQty>(Messages.POOrderQtyUpdateNotAllowed, PXErrorLevel.Error);
                    }

                    if (Base.Transactions.View.Ask(poline, PX.Objects.AP.Messages.Warning,
                                                Messages.ChangingQtyForLinkedRecordFSO, MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.No)
                    {
                        e.NewValue = poline.OrderQty;
                    }
                }
                else if (fsoLineforUpdate.POSource == ListField_FSPOSource.PurchaseToAppointment)
                {
                    if (Base.Transactions.View.Ask(poline, PX.Objects.AP.Messages.Warning,
                                                Messages.ChangingQtyForLinkedRecordFSOAppt, MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.No)
                    {
                        e.NewValue = poline.OrderQty;
                    }
                }
            }
        }

        protected virtual void _(Events.RowDeleting<POLine> e, PXRowDeleting BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            POLine poline = e.Row;
            if (poline == null) { return; }

            //Check if attached to Service Order
            if (poline.LineType == POLineType.GoodsForServiceOrder || poline.LineType == POLineType.NonStockForServiceOrder)
            {
                FSSODet fsoLineExists = (FSSODet)FixedDemandViaServiceOrderForCostUpdate.SelectWindowed(0, 1, poline.OrderType, poline.OrderNbr, poline.LineNbr);

                if (fsoLineExists != null &&
                    Base.Transactions.View.Ask("POLineLinkedToSrvLine", poline, PX.Objects.AP.Messages.Warning,
                                                PXMessages.LocalizeFormatNoPrefixNLA(Messages.POLineLinkedToServiceOrderLine, fsoLineExists.RefNbr),
                                                MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        protected virtual void _(Events.RowDeleting<POOrder> e, PXRowDeleting BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            POOrder poOrder = e.Row;
            if (poOrder == null) { return; }

            List<POLine> items = Base.Transactions.Select().RowCast<POLine>().Select(PXCache<POLine>.CreateCopy).ToList();
            if (items.Count > 0)
            {
                //Check if attached to Service Order
                if (items.Any(x => ((x.LineType == POLineType.GoodsForServiceOrder || x.LineType == POLineType.NonStockForServiceOrder) &&
                                   !String.IsNullOrEmpty(x.GetExtension<POLineCostPXExt>().UsrSOLinkRef))))
                {
                    if (Base.Document.View.Ask("POOrderLinkedToSrv", poOrder, PX.Objects.AP.Messages.Warning,
                                                    Messages.POOrderLinkedToServiceOrder,
                                                    MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.No)
                    {
                        e.Cancel = true;
                    }
                }
                else if (items.Any(x => ((x.LineType == POLineType.GoodsForSalesOrder || x.LineType == POLineType.GoodsForDropShip ||
                                          x.LineType == POLineType.NonStockForSalesOrder || x.LineType == POLineType.NonStockForDropShip) &&
                                   !String.IsNullOrEmpty(x.GetExtension<POLineCostPXExt>().UsrSOLinkRef))))
                {
                    if (Base.Document.View.Ask("POOrderLinkedToSO", poOrder, PX.Objects.AP.Messages.Warning,
                                                    Messages.POOrderLinkedToSalesOrder,
                                                    MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.No)
                    {
                        e.Cancel = true;
                    }
                }
            }

            Base.Transactions.View.SetAnswer("POLineLinkedToSrvLine", WebDialogResult.OK);
        }

        protected virtual void _(Events.RowSelected<POLine> e, PXRowSelected BaseInvoke)
        {
            if (BaseInvoke != null) { BaseInvoke(e.Cache, e.Args); }

            POLine poline = (POLine)e.Row;
            if (poline == null) { return; }

            POLineCostPXExt polineExt = PXCache<POLine>.GetExtension<POLineCostPXExt>(poline);
            ViewServiceOrderDemand.SetVisible(polineExt.UsrIsServiceOrderLine.GetValueOrDefault(false));
            PurchaseToSOLinksExt posoLinkGraph = Base.GetExtension<PurchaseToSOLinksExt>();
            posoLinkGraph?.viewDemand?.SetVisible(polineExt.UsrIsNonServiceOrderLine.GetValueOrDefault(false));

            bool bAllowEdit = !(polineExt.UsrSOLinkRef == Messages.ViewMultiple);
            PXUIFieldAttribute.SetEnabled<POLine.orderQty>(e.Cache, poline, !(!bAllowEdit || polineExt.UsrIsFreight.GetValueOrDefault(false)));
            PXUIFieldAttribute.SetEnabled<POLine.curyUnitCost>(e.Cache, poline, bAllowEdit);
            PXUIFieldAttribute.SetEnabled<POLine.curyLineAmt>(e.Cache, poline, bAllowEdit);
            
            //Dhiren --- Work In Progress 
            //AddFreightItem.SetEnabled(bAllowEdit && !String.IsNullOrEmpty(polineExt.UsrSOLinkRef) && !polineExt.UsrIsFreight.GetValueOrDefault(false));
            //DeLinkCurrentPOLine.SetEnabled(!String.IsNullOrEmpty(polineExt.UsrSOLinkRef));
        }

        #region Link To SO/ServiceOrder
        public PXAction<POOrder> ViewServiceOrderDemand;
        [PXUIField(DisplayName = PX.Objects.PO.Messages.ViewDemand,
                   MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXLookupButton()]
        public virtual IEnumerable viewServiceOrderDemand(PXAdapter adapter)
        {
            FixedDemandViaServiceOrderForCostUpdate.AskExt();
            return adapter.Get();
        }

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [PXFormula(typeof(PXExtSOLinkFromPO<POLine.lineType, POLine.lineNbr, POLine.orderType, POLine.orderNbr>))]
        public void _(Events.CacheAttached<POLineCostPXExt.usrSOLinkRef> e) { }

        public PXAction<POOrder> ViewLinkedSalesOrServiceRef;
        [PXUIField(DisplayName = Messages.ViewSalesOrServiceOrderFromPO,
                   MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXLookupButton]
        public virtual IEnumerable viewLinkedSalesOrServiceRef(PXAdapter adapter)
        {
            POLine row = Base.Transactions.Current;
            if (row == null) { return adapter.Get(); }

            if (!(row.LineType == POLineType.GoodsForDropShip || row.LineType == POLineType.GoodsForSalesOrder ||
                  row.LineType == POLineType.NonStockForDropShip || row.LineType == POLineType.NonStockForSalesOrder ||
                  row.LineType == POLineType.GoodsForServiceOrder || row.LineType == POLineType.NonStockForServiceOrder)) { return adapter.Get(); }

            POLineCostPXExt rowExt = row.GetExtension<POLineCostPXExt>();
            if (String.IsNullOrEmpty(rowExt.UsrSOLinkRef)) { return adapter.Get(); }
            var linkInfo = rowExt.UsrSOLinkRef.Split('-');

            if (row.LineType == POLineType.GoodsForDropShip || row.LineType == POLineType.GoodsForSalesOrder ||
                row.LineType == POLineType.NonStockForDropShip || row.LineType == POLineType.NonStockForSalesOrder)
            {
                if (rowExt.UsrSOLinkRef != Messages.ViewMultiple)
                {
                    SOOrderEntry soGraph = PXGraph.CreateInstance<SOOrderEntry>();
                    soGraph.Document.Current = soGraph.Document.Search<SOOrder.orderNbr>(linkInfo[1], linkInfo[0]);
                    if (soGraph.Document.Current != null)
                    {
                        throw new PXRedirectRequiredException(soGraph, true, "View Sales Order") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                    }
                }
                else
                {
                    Base.FixedDemand.AskExt();                    
                }
            }
            else if (row.LineType == POLineType.GoodsForServiceOrder ||
                     row.LineType == POLineType.NonStockForServiceOrder)
            {
                if (rowExt.UsrSOLinkRef != Messages.ViewMultiple)
                {
                    ServiceOrderEntry soGraph = PXGraph.CreateInstance<ServiceOrderEntry>();
                    soGraph.ServiceOrderRecords.Current = soGraph.ServiceOrderRecords.Search<FSServiceOrder.refNbr>(linkInfo[1], linkInfo[0]);
                    if (soGraph.ServiceOrderRecords.Current != null)
                    {
                        throw new PXRedirectRequiredException(soGraph, true, "View Service Order") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                    }
                }
                else
                {
                    FixedDemandViaServiceOrderForCostUpdate.AskExt();
                }
            }
            return adapter.Get();
        }

        public PXAction<POOrder> DeLinkCurrentPOLine;

        //Dhiren --- Work In Progress 
        //[PXUIField(DisplayName = Messages.Delink,
        //           MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXUIField(DisplayName = Messages.Delink,
                   MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false, Enabled = false)]
        [PXLookupButton]
        public virtual IEnumerable deLinkCurrentPOLine(PXAdapter adapter)
        {
            POLine row = Base.Transactions.Current;
            if (row == null) { return adapter.Get(); }

            if (!(row.LineType == POLineType.GoodsForDropShip || row.LineType == POLineType.GoodsForSalesOrder ||
                  row.LineType == POLineType.NonStockForDropShip || row.LineType == POLineType.NonStockForSalesOrder ||
                  row.LineType == POLineType.GoodsForServiceOrder || row.LineType == POLineType.NonStockForServiceOrder)) { return adapter.Get(); }

            POLineCostPXExt rowExt = row.GetExtension<POLineCostPXExt>();
            //if (String.IsNullOrEmpty(rowExt.UsrSOLinkRef)) { return adapter.Get(); }
            
            //var linkInfo = rowExt.UsrSOLinkRef.Split('-');
            //if (linkInfo.Length != 2) { return adapter.Get(); }

            if (row.LineType == POLineType.GoodsForDropShip || row.LineType == POLineType.GoodsForSalesOrder ||
                row.LineType == POLineType.NonStockForDropShip || row.LineType == POLineType.NonStockForSalesOrder)
            {
                if (Base.Document.View.Ask("DelinkPOOrderLinefromSO", row, PX.Objects.AP.Messages.Warning,
                                                Messages.UnLinkPOOrderLineLinkedToSalesOrderLine,
                                                MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.Yes)
                {
                    rowExt.UsrDeLink = true;
                    Base.Transactions.Update(row);
                }
            }
            else if (row.LineType == POLineType.GoodsForServiceOrder || row.LineType == POLineType.NonStockForServiceOrder)
            {
                if (Base.Document.View.Ask("DelinkPOOrderLinefromSrv", row, PX.Objects.AP.Messages.Warning,
                                                Messages.UnLinkPOOrderLineLinkedToServiceOrderLine,
                                                MessageButtons.YesNo, MessageIcon.Question) == WebDialogResult.Yes)
                {
                    rowExt.UsrDeLink = true;
                    Base.Transactions.Update(row);
                }
            }

            return adapter.Get();
        }

        public PXAction<POOrder> AddFreightItem;
        
        //Dhiren --- Work In Progress 
        //[PXUIField(DisplayName = Messages.AddFreight,
        //           MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXUIField(DisplayName = Messages.AddFreight,
                   MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false, Enabled = false)]
        [PXButton(CommitChanges = true)]
        public virtual IEnumerable addFreightItem(PXAdapter adapter)
        {
            POOrder poOrder = Base.Document.Current;
            POLine poline = Base.Transactions.Current;
            if (poOrder == null || poline == null) { return adapter.Get(); }

            POLineCostPXExt polineExt = PXCache<POLine>.GetExtension<POLineCostPXExt>(poline);
            POSetup poSetup = Base.POSetup.Current;
            POSetupCostPXExt poSetupExt = PXCache<POSetup>.GetExtension<POSetupCostPXExt>(poSetup);
            if (!poSetupExt.UsrFreightInventory.HasValue) { throw new PXException(Messages.FreightInventoryNotSpecified); }

            var poLines = Base.Transactions.Select().RowCast<POLine>().Select(PXCache<POLine>.CreateCopy).ToList();
            if (poLines.Any(x => (x.GetExtension<POLineCostPXExt>()?.UsrSOLinkRef == polineExt.UsrSOLinkRef && 
                                  x.GetExtension<POLineCostPXExt>().UsrIsFreight.GetValueOrDefault(false) &&
                                  x.InventoryID == poSetupExt.UsrFreightInventory))) { throw new PXException(Messages.FreightInventoryAlreadyAdded); } 

            //Check if already added
            //poLines = poLines.Where(x => !String.IsNullOrEmpty(x.GetExtension<POLineCostPXExt>().UsrSOLinkRef)).ToList();
            //if (poLines.Count <= 0) { throw new PXException(Messages.FreightInventoryNotEligible); }

            var addFreightParamsCache = AddFreightView.Cache;

            if (!Base.IsMobile && AddFreightView.AskExt(
                (graph, viewname) =>
                {
                    addFreightParamsCache.Clear();
                },
                true) != WebDialogResult.OK) { return adapter.Get(); }

            var freightParams = AddFreightView.Current;

            if (freightParams.FreightAmount.GetValueOrDefault(0m) <= 0m)
            {
                addFreightParamsCache.RaiseExceptionHandling<AddFreightParams.freightAmount>(
                    freightParams,
                    freightParams.FreightAmount,
                    new PXSetPropertyKeepPreviousException(Messages.InvalidFreight, PXErrorLevel.Error,
                        PXUIFieldAttribute.GetDisplayName<AddFreightParams.freightAmount>(addFreightParamsCache)));
            }
            if (PXUIFieldAttribute.GetErrors(addFreightParamsCache, freightParams).Count > 0) { return adapter.Get(); }


            Base.Transactions.View.SetAnswer("NewPOLineLinkedToSOLine", WebDialogResult.Yes);
            Base.Transactions.View.SetAnswer("NewPOLineLinkedToSrvLine", WebDialogResult.Yes);

            POLine poFrtline = Base.Transactions.Insert();
            
            Base.Transactions.SetValueExt<POLine.inventoryID>(poFrtline, poSetupExt.UsrFreightInventory);
            Base.Transactions.SetValueExt<POLine.orderQty>(poFrtline, 1m);
            poFrtline.LineType = (poOrder?.OrderType == POOrderType.DropShip) ? POLineType.NonStockForDropShip :
                               (poline.LineType == POLineType.GoodsForSalesOrder) ? POLineType.NonStockForSalesOrder :
                               (poline.LineType == POLineType.GoodsForServiceOrder) ? POLineType.NonStockForServiceOrder : POLineType.NonStock;
            poFrtline.CuryUnitCost = freightParams.FreightAmount;
            POLineCostPXExt poFrtlineExt = PXCache<POLine>.GetExtension<POLineCostPXExt>(poFrtline);
            poFrtlineExt.UsrIsFreight = true;
            poFrtlineExt.UsrSOLinkRef = polineExt.UsrSOLinkRef;
            poFrtline = Base.Transactions.Update(poFrtline);
            
            return adapter.Get();
        }

        #endregion

        [PXOverride]
        public void Persist(Action basePersist)
        {
            try
            {
                List<POLine> items = Base.Transactions.Select().RowCast<POLine>().Select(PXCache<POLine>.CreateCopy).ToList();
                var itemsforupdate = items.Where(x => !String.IsNullOrEmpty(x?.GetExtension<POLineCostPXExt>().UsrSOLinkRef)
                                          && Base.Transactions.Cache.GetStatus(x) != PXEntryStatus.Deleted).ToList();

                //Dhiren --- Work In Progress 
                //var itemsDelinked = items.Where(x => !String.IsNullOrEmpty(x?.GetExtension<POLineCostPXExt>().UsrSOLinkRef)
                //                          && Base.Transactions.Cache.GetStatus(x) != PXEntryStatus.Deleted
                //                          && x.GetExtension<POLineCostPXExt>().UsrDeLink.GetValueOrDefault(false)).ToList();

                List<LinkedOrder> soOrders = itemsforupdate.GroupBy(x => x.GetExtension<POLineCostPXExt>().UsrSOLinkRef).
                                              Select(y =>
                                                   new LinkedOrder
                                                   {
                                                       RefNbr = y.Key,
                                                       RefType = y.FirstOrDefault()?.LineType,
                                                       LinkedMultipleOrders = GetOrdersValue(y.FirstOrDefault(), y.Key, y.FirstOrDefault()?.LineType)
                                                   }).ToList();

                using (PXTransactionScope ts = new PXTransactionScope())
                {
                    basePersist();

                    if (soOrders.Count > 0 && itemsforupdate.Count > 0) { UpdateLinkedSOAndServiceOrder(itemsforupdate, soOrders); }
                    if (Base.Transactions.Cache.Deleted.Count() > 0) { ClearPORerencesFromServiceLineForFreight(Base.Transactions.Cache.Deleted); }                 
                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Dhiren --- Work In Progress for delink and addfreight
        protected virtual void UpdateLinkedSOAndServiceOrder(List<POLine> items, List<LinkedOrder> soOrders)
        {
            POOrder poOrder = Base.Document.Current;
            SOOrderEntry soGraph = (PXAccess.FeatureInstalled<FeaturesSet.distributionModule>()) ? 
                                    PXGraph.CreateInstance<SOOrderEntry>() : null;
            ServiceOrderEntry srvGraph = (PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>()) ?
                                    PXGraph.CreateInstance<ServiceOrderEntry>() : null;
            AppointmentEntry graphAppt = (PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>()) ?
                                    PXGraph.CreateInstance<AppointmentEntry>() : null;

            foreach (LinkedOrder linkedSO in soOrders)
            {
                if (linkedSO.RefNbr == Messages.ViewMultiple)
                {
                    List<LinkedOrder> liOrders = new List<LinkedOrder>();
                    foreach (string sOrder in linkedSO.LinkedMultipleOrders.Split(','))
                    {
                        if (!String.IsNullOrEmpty(sOrder))
                        {
                            liOrders.Add(new LinkedOrder() { RefType = linkedSO.RefType, RefNbr = sOrder });
                        }
                    }
                    UpdateLinkedSOAndServiceOrder(items, liOrders);
                }
                else
                {
                    string soType = linkedSO.RefNbr.Split('-')[0];
                    string soNbr = linkedSO.RefNbr.Split('-')[1];
                    //var polines = items.Where(x => x.GetExtension<POLineCostPXExt>().UsrSOLinkRef == linkedSO.RefNbr &&
                    //                               !x.GetExtension<POLineCostPXExt>().UsrIsSpecialOrderItem.GetValueOrDefault(false));
                    //var polines = items.Where(x => !String.IsNullOrEmpty(x.GetExtension<POLineCostPXExt>().UsrSOLinkRef)  &&
                    //                               !x.GetExtension<POLineCostPXExt>().UsrIsSpecialOrderItem.GetValueOrDefault(false));
                    var polines = items.Where(x => !String.IsNullOrEmpty(x.GetExtension<POLineCostPXExt>().UsrSOLinkRef));
                    if ((soGraph != null) && 
                        (linkedSO.RefType == POLineType.GoodsForSalesOrder ||
                         linkedSO.RefType == POLineType.GoodsForDropShip ||
                         linkedSO.RefType == POLineType.NonStockForSalesOrder ||
                         linkedSO.RefType == POLineType.NonStockForDropShip))
                    {
                        bool bChanged = false;
                        soGraph.Clear();
                        soGraph.Document.Current = soGraph.Document.Search<SOOrder.orderNbr>(soNbr, soType);
                        foreach (var poline in polines)
                        {
                            POLineCostPXExt polineExt = PXCache<POLine>.GetExtension<POLineCostPXExt>(poline);
                            var lidemands = Base.FixedDemand.View.SelectMultiBound(new object[] { poline });
                            if (lidemands.Count <= 0 && polineExt.UsrIsFreight.GetValueOrDefault(false))
                            {
                                SOLine freightLine = soGraph.Transactions.Insert();
                                soGraph.Transactions.SetValueExt<SOLine.inventoryID>(freightLine, poline.InventoryID);
                                soGraph.Transactions.SetValueExt<SOLine.orderQty>(freightLine, poline.OrderQty);
                                freightLine.CuryUnitPrice = poline.CuryUnitCost;
                                freightLine.POCreate = true;
                                freightLine.POCreated = true;
                                freightLine.POSource = (poline.OrderType == POOrderType.DropShip) ? INReplenishmentSource.DropShipToOrder :
                                                       (poline.OrderType == POOrderType.RegularOrder) ? INReplenishmentSource.PurchaseToOrder : null;
                                freightLine.VendorID = poline.VendorID;
                                freightLine.ManualPrice = true;
                                freightLine = soGraph.Transactions.Update(freightLine);

                                SOLineSplit splfreightLine = soGraph.splits.Current ?? new SOLineSplit();
                                splfreightLine.RefNoteID = poline.NoteID;
                                splfreightLine.Qty = poline.OrderQty;
                                splfreightLine.PONbr = poline.OrderNbr;
                                splfreightLine.POLineNbr = poline.LineNbr;
                                splfreightLine.POType = poline.OrderType;
                                splfreightLine.VendorID = poline.VendorID;
                                splfreightLine = soGraph.splits.Update(splfreightLine);
                            }
                            else
                            {
                                foreach (PXResult<SOLineSplit3, INItemPlan> fxddemandLine in lidemands)
                                {
                                    SOLineSplit3 demandLine = fxddemandLine;
                                    if (!(demandLine.OrderType == soType && demandLine.OrderNbr == soNbr)) { continue; }
                                    soGraph.Transactions.Current = soGraph.Transactions.Select().RowCast<SOLine>().Where(x => x.LineNbr == demandLine.LineNbr).FirstOrDefault();
                                    if (polineExt.UsrDeLink.GetValueOrDefault(false))
                                    {
                                        bChanged = true;
                                        soGraph.Transactions.Current.POCreated = false;
                                        soGraph.Transactions.Current = soGraph.Transactions.Update(soGraph.Transactions.Current);
                                        soGraph.splits.Current = soGraph.splits.Select().RowCast<SOLineSplit>().Where(x => x.SplitLineNbr == demandLine.SplitLineNbr && x.LineNbr == demandLine.LineNbr).FirstOrDefault();
                                        soGraph.splits.Current.RefNoteID = null;
                                        soGraph.splits.Current.PONbr = null;
                                        soGraph.splits.Current.POType = null;
                                        soGraph.splits.Current.POLineNbr = null;
                                        soGraph.splits.Current = soGraph.splits.Update(soGraph.splits.Current);
                                        polineExt.UsrSOLinkRef = (lidemands.Count > 1) ? polineExt.UsrSOLinkRef : null;
                                        Base.Transactions.Update(poline);
                                    }
                                    else if (polineExt.UsrSOLinkRef != Messages.ViewMultiple)
                                    {
                                        if (soGraph.Transactions.Current.OrderQty != poline.OrderQty ||
                                            soGraph.Transactions.Current.CuryUnitCost != poline.CuryUnitCost)
                                        {
                                            bChanged = true;
                                            soGraph.Transactions.Current.CuryUnitCost = poline.CuryUnitCost;
                                            soGraph.Transactions.Current = soGraph.Transactions.Update(soGraph.Transactions.Current);
                                            soGraph.splits.Current = soGraph.splits.Select().RowCast<SOLineSplit>().Where(x => x.SplitLineNbr == demandLine.SplitLineNbr && x.LineNbr == demandLine.LineNbr).FirstOrDefault();
                                            soGraph.splits.Current.Qty = poline.OrderQty;
                                            soGraph.splits.Current = soGraph.splits.Update(soGraph.splits.Current);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if ((srvGraph != null) && 
                             (linkedSO.RefType == POLineType.GoodsForServiceOrder ||
                              linkedSO.RefType == POLineType.NonStockForServiceOrder))
                    {
                        srvGraph.Clear();
                        srvGraph.ServiceOrderRecords.Current = srvGraph.ServiceOrderRecords.Search<FSServiceOrder.refNbr>(soNbr, soType);
                        foreach (var poline in polines)
                        {
                            POLineCostPXExt polineExt = PXCache<POLine>.GetExtension<POLineCostPXExt>(poline);
                            FSSODet fsoLineforUpdate = (FSSODet)FixedDemandViaServiceOrderForCostUpdate.Select(poline.OrderType, poline.OrderNbr, poline.LineNbr);
                            if (!(fsoLineforUpdate?.SrvOrdType?.Trim() == soType && fsoLineforUpdate?.RefNbr?.Trim() == soNbr)) { continue; }
                            if (fsoLineforUpdate == null && !polineExt.UsrIsFreight.GetValueOrDefault(false)) { continue; }
                            if (fsoLineforUpdate == null && polineExt.UsrIsFreight.GetValueOrDefault(false))
                            {
                                FSSODet freightLine = new FSSODet();
                                freightLine.LineType = ID.LineType_ALL.NONSTOCKITEM;
                                freightLine = srvGraph.ServiceOrderDetails.Insert(freightLine);
                                srvGraph.ServiceOrderDetails.SetValueExt<FSSODet.inventoryID>(freightLine, poline.InventoryID);
                                srvGraph.ServiceOrderDetails.SetValueExt<FSSODet.estimatedQty>(freightLine, poline.OrderQty);
                                srvGraph.ServiceOrderDetails.SetValueExt<FSSODet.curyUnitPrice>(freightLine, poline.CuryUnitCost);

                                freightLine.POStatus = poOrder.Status;
                                freightLine.POCreate = true;
                                freightLine.EnablePO = true;
                                freightLine.POSource = (poline.OrderType == POOrderType.DropShip) ? INReplenishmentSource.DropShipToOrder :
                                                       (poline.OrderType == POOrderType.RegularOrder) ? INReplenishmentSource.PurchaseToOrder : null;
                                freightLine.VendorID = poline.VendorID;
                                freightLine.PONbr = poline.OrderNbr;
                                freightLine.POLineNbr = poline.LineNbr;
                                freightLine.POType = poline.OrderType;
                                freightLine.ManualPrice = true;
                                freightLine = srvGraph.ServiceOrderDetails.Update(freightLine);

                                FSSODetSplit splfreightLine = srvGraph.Splits.Current ?? new FSSODetSplit();
                                splfreightLine.RefNoteID = poline.NoteID;
                                splfreightLine.Qty = poline.OrderQty;
                                splfreightLine.PONbr = poline.OrderNbr;
                                splfreightLine.POLineNbr = poline.LineNbr;
                                splfreightLine.POType = poline.OrderType;
                                splfreightLine.VendorID = poline.VendorID;
                                splfreightLine = srvGraph.Splits.Update(splfreightLine);
                            }
                            else
                            {
                                srvGraph.ServiceOrderDetails.Current = srvGraph.ServiceOrderDetails.Select().RowCast<FSSODet>().Where(x => x.LineNbr == fsoLineforUpdate.LineNbr).FirstOrDefault();
                                if (polineExt.UsrDeLink.GetValueOrDefault(false))
                                {
                                    srvGraph.ServiceOrderDetails.Current.POLineNbr = null;
                                    srvGraph.ServiceOrderDetails.Current.POType = null;
                                    srvGraph.ServiceOrderDetails.Current.PONbr = null;
                                    srvGraph.ServiceOrderDetails.Current.POSource = null;
                                    srvGraph.ServiceOrderDetails.Current.POStatus = null;
                                    srvGraph.ServiceOrderDetails.Current = srvGraph.ServiceOrderDetails.Update(srvGraph.ServiceOrderDetails.Current);
                                    var fsSplits = srvGraph.Splits.Select().RowCast<FSSODetSplit>().
                                                               Where(x => x.LineNbr == fsoLineforUpdate.LineNbr && x.POLineNbr == poline.LineNbr &&
                                                                          x.SrvOrdType == fsoLineforUpdate.SrvOrdType &&
                                                                          x.RefNbr == fsoLineforUpdate.RefNbr);
                                    foreach (FSSODetSplit fssplit in fsSplits)
                                    {
                                        srvGraph.Splits.Current = fssplit;
                                        srvGraph.Splits.Current.POLineNbr = null;
                                        srvGraph.Splits.Current.POType = null;
                                        srvGraph.Splits.Current.PONbr = null;
                                        srvGraph.Splits.Current.POSource = null;
                                        srvGraph.Splits.Current = srvGraph.Splits.Update(srvGraph.Splits.Current);
                                    }
                                    polineExt.UsrSOLinkRef = null;
                                    Base.Transactions.Update(poline);
                                }
                                else if (polineExt.UsrSOLinkRef != Messages.ViewMultiple)
                                {
                                    if (srvGraph.ServiceOrderDetails.Current.EstimatedQty != poline.OrderQty ||
                                        srvGraph.ServiceOrderDetails.Current.CuryUnitCost != poline.CuryUnitCost)
                                    {
                                        if (srvGraph.ServiceOrderDetails.Current.POSource == ListField_FSPOSource.PurchaseToServiceOrder)
                                        {
                                            srvGraph.ServiceOrderDetails.Current.CuryUnitCost = poline.CuryUnitCost;
                                            srvGraph.ServiceOrderDetails.Current = srvGraph.ServiceOrderDetails.Update(srvGraph.ServiceOrderDetails.Current);
                                            var fsSplits = srvGraph.Splits.Select().RowCast<FSSODetSplit>().
                                                                       Where(x => x.LineNbr == fsoLineforUpdate.LineNbr && x.POLineNbr == poline.LineNbr &&
                                                                                  x.SrvOrdType == fsoLineforUpdate.SrvOrdType &&
                                                                                  x.RefNbr == fsoLineforUpdate.RefNbr);
                                            foreach (FSSODetSplit fssplit in fsSplits)
                                            {
                                                srvGraph.Splits.Current = fssplit;
                                                srvGraph.Splits.Current.Qty = poline.OrderQty;
                                                srvGraph.Splits.Current = srvGraph.Splits.Update(srvGraph.Splits.Current);
                                            }
                                            srvGraph.ServiceOrderDetails.Cache.RaiseFieldUpdated<FSSODet.estimatedQty>(srvGraph.ServiceOrderDetails.Current, null);
                                        }
                                        else if (srvGraph.ServiceOrderDetails.Current.POSource == ListField_FSPOSource.PurchaseToAppointment)
                                        {
                                            FSAppointmentDet apptLine = (FSAppointmentDet)FixedDemandViaServiceOrderApptForCostUpdate.Select(poline.OrderType, poline.OrderNbr, poline.LineNbr);
                                            if (apptLine?.AppointmentID != null && graphAppt != null)
                                            {
                                                graphAppt.Clear();

                                                graphAppt.AppointmentRecords.Current = graphAppt.AppointmentRecords.Search<FSAppointment.appointmentID>(apptLine.AppointmentID, apptLine.SrvOrdType);
                                                if (graphAppt.AppointmentRecords.Current != null)
                                                {
                                                    graphAppt.AppointmentDetails.Current = graphAppt.AppointmentDetails.Select().RowCast<FSAppointmentDet>().Where(x => x.LineNbr == apptLine.LineNbr).FirstOrDefault();
                                                    if (graphAppt.AppointmentDetails.Current != null)
                                                    {
                                                        if (graphAppt.AppointmentDetails.Current.EstimatedQty != poline.OrderQty ||
                                                            graphAppt.AppointmentDetails.Current.CuryUnitCost != poline.CuryUnitCost)
                                                        {
                                                            graphAppt.AppointmentDetails.Current.CanChangeMarkForPO = true;
                                                            graphAppt.AppointmentDetails.Current.EstimatedQty = poline.OrderQty;
                                                            graphAppt.AppointmentDetails.Current.CuryUnitCost = poline.CuryUnitCost;
                                                            graphAppt.AppointmentDetails.Update(graphAppt.AppointmentDetails.Current);
                                                            graphAppt.Persist();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (srvGraph.IsDirty) { srvGraph.Persist(); }
                if (soGraph.IsDirty) { soGraph.Persist(); }
            }
        }

        protected virtual void ClearPORerencesFromServiceLineForFreight(IEnumerable items)
        {
            foreach(POLine poline in items)
            {
                if (poline.LineType == POLineType.NonStockForServiceOrder)
                {
                    POLineCostPXExt polineExt = PXCache<POLine>.GetExtension<POLineCostPXExt>(poline);
                    if (polineExt.UsrIsFreight.GetValueOrDefault(false))
                    {
                        PXUpdate<
                            Set<FSSODet.poNbr, Required<FSSODet.poNbr>,
                            Set<FSSODet.poStatus, Required<FSSODet.poStatus>,
                            Set<FSSODet.poCompleted, Required<FSSODet.poCompleted>>>>,
                        FSSODet,
                        Where<
                            FSSODet.poType, Equal<Required<FSSODet.poType>>,
                            And<FSSODet.poNbr, Equal<Required<FSSODet.poNbr>>,
                            And<FSSODet.poLineNbr, Equal<Required<FSSODet.poLineNbr>>>>>>
                        .Update(Base, null, null, null, poline.OrderType, poline.OrderNbr, poline.LineNbr);
                    }
                }
            }
        }

        private String GetOrdersValue(POLine poline, string sRefNbr, string RefType)
        {
            if (sRefNbr != Messages.ViewMultiple) { return String.Empty; }
            string sReturnValue = String.Empty;
            if (RefType == POLineType.GoodsForSalesOrder ||
                RefType == POLineType.GoodsForDropShip ||
                RefType == POLineType.NonStockForSalesOrder ||
                RefType == POLineType.NonStockForDropShip)
            {
                foreach(PXResult<SOLineSplit3, INItemPlan> data in Base.FixedDemand.View.SelectMultiBound(new object[] { poline }))
                {
                    SOLineSplit3 splData = data;
                    sReturnValue += splData.OrderType + "-" + splData.OrderNbr + ",";
                }
            }
            else if (RefType == POLineType.GoodsForServiceOrder ||
                     RefType == POLineType.NonStockForServiceOrder)
            {
                foreach(PXResult<FSSODet> data in FixedDemandViaServiceOrderForCostUpdate.Select(poline.OrderType, poline.OrderNbr, poline.LineNbr))
                {
                    FSSODet fsdetData = data;
                    sReturnValue += fsdetData.SrvOrdType + "-" + fsdetData.RefNbr + ",";
                }
            }
            return sReturnValue;
        }
    }

    public class LinkedOrder
    {
        public String RefNbr { get; set; }
        public String RefType { get; set; }
        public String LinkedMultipleOrders { get; set; }
    }
}