using System;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class SOShipmentEntryCostPXExt : PXGraphExtension<SOShipmentEntry>
    {
        public delegate void BasePostShipment(INRegisterEntryBase docgraph, PXResult<SOOrderShipment, SOOrder> sh, DocumentList<INRegister> list, ARInvoice invoice);

        [PXOverride]
        public virtual void PostShipment(INRegisterEntryBase docgraph, PXResult<SOOrderShipment, SOOrder> sh,
                                         DocumentList<INRegister> list, ARInvoice invoice,
                                         BasePostShipment BaseInvoke)
        {
            docgraph.RowInserting.AddHandler<INTran>((sender, e) =>
            {
                INTran data = (INTran)e.Row;

                InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<INTran.inventoryID>(docgraph.Caches[typeof(INTran)], data);
                InventoryItemCostPXExt itemExt = PXCache<InventoryItem>.GetExtension<InventoryItemCostPXExt>(item);

                if (item.ValMethod == INValMethod.Average && itemExt.UsrIsSpecialOrderItem.GetValueOrDefault(false) &&
                    data.DocType == INDocType.Issue && data.SOLineType == SOLineType.Inventory)
                {
                    SOLine solineData = PXSelect<SOLine, Where<SOLine.lineNbr, Equal<Required<SOLine.lineNbr>>,
                                                            And<SOLine.orderType, Equal<Required<SOLine.orderType>>,
                                                            And<SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>>>>>.
                                                            Select(Base, data.SOOrderLineNbr, data.SOOrderType, data.SOOrderNbr);

                    if (solineData != null)
                    {
                        POLine poData = PXSelectJoin<POLine, InnerJoin<POOrderEntry.SOLineSplit3, On<POOrderEntry.SOLineSplit3.pOLineNbr, Equal<POLine.lineNbr>,
                                                                And<POOrderEntry.SOLineSplit3.pOType, Equal<POLine.orderType>,
                                                                And<POOrderEntry.SOLineSplit3.pONbr, Equal<POLine.orderNbr>>>>>,
                                                             Where<POOrderEntry.SOLineSplit3.lineNbr, Equal<Required<POOrderEntry.SOLineSplit3.lineNbr>>,
                                                                And<POOrderEntry.SOLineSplit3.orderType, Equal<Required<POOrderEntry.SOLineSplit3.orderType>>,
                                                                And<POOrderEntry.SOLineSplit3.orderNbr, Equal<Required<POOrderEntry.SOLineSplit3.orderNbr>>>>>>.
                                                                Select(Base, solineData.LineNbr, solineData.OrderType, solineData.OrderNbr);
                        if (poData != null)
                        {
                            INTranCostPXExt dataExt = PXCache<INTran>.GetExtension<INTranCostPXExt>(data);
                            dataExt.UsrSpecialOrderCost = true;

                            PXNoteAttribute.CopyNoteAndFiles(Base.Caches[typeof(SOLine)], solineData, sender, data, true, false);
                        }
                    }
                }
            });

            BaseInvoke(docgraph, sh, list, invoice);
        }
    }
}