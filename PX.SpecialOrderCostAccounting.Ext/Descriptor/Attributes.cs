using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.FS;
using PX.Objects.PO;
using PX.Objects.SO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public class PXExtSOLinkFromPO<POLineTypeFld, POLineNbrFld, POOrderTypeFld, POOrderNbrFld> : BqlFormulaEvaluator<POLineTypeFld, POLineNbrFld, POOrderTypeFld, POOrderNbrFld>, IBqlOperand
        where POLineTypeFld : IBqlOperand
        where POLineNbrFld : IBqlOperand
        where POOrderTypeFld : IBqlOperand
        where POOrderNbrFld : IBqlOperand
    {
        public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> parameters)
        {
            string poLineType = Convert.ToString(parameters[typeof(POLineTypeFld)]);
            int? poLineNbr = (int?)(parameters[typeof(POLineNbrFld)]);
            string poOrderType = Convert.ToString(parameters[typeof(POOrderTypeFld)]);
            string poOrderNbr = Convert.ToString(parameters[typeof(POOrderNbrFld)]);

            if (poLineNbr.HasValue && !String.IsNullOrEmpty(poOrderType) && !String.IsNullOrEmpty(poOrderNbr))
            {
                if (poLineType == POLineType.GoodsForDropShip ||
                    poLineType == POLineType.GoodsForSalesOrder ||
                    poLineType == POLineType.NonStockForSalesOrder)
                {
                    var lisodata = PXSelectReadonly<SOLineSplit,
                                            Where<SOLineSplit.pOType, Equal<Required<POLine.orderType>>,
                                                And<SOLineSplit.pONbr, Equal<Required<POLine.orderNbr>>,
                                                And<SOLineSplit.pOLineNbr, Equal<Required<POLine.lineNbr>>>>>>.
                                                Select(cache.Graph, poOrderType, poOrderNbr, poLineNbr);
                    if (lisodata.Count > 1) { return Messages.ViewMultiple; }
                    SOLineSplit sodata = lisodata;
                    return (!String.IsNullOrEmpty(sodata?.OrderType) && !String.IsNullOrEmpty(sodata?.OrderNbr)) ?
                                String.Format("{0}-{1}", sodata?.OrderType.Trim(), sodata?.OrderNbr.Trim()) : null;
                }
                else if (poLineType == POLineType.GoodsForServiceOrder ||
                         poLineType == POLineType.NonStockForServiceOrder)
                {
                    var liserviceOrderData = PXSelectReadonly<FSSODet,
                                                    Where<FSSODet.poType, Equal<Required<POLine.orderType>>,
                                                        And<FSSODet.poNbr, Equal<Required<POLine.orderNbr>>,
                                                        And<FSSODet.poLineNbr, Equal<Required<POLine.lineNbr>>>>>>.
                                                        Select(cache.Graph, poOrderType, poOrderNbr, poLineNbr);
                    if (liserviceOrderData.Count > 1) { return Messages.ViewMultiple; }
                    FSSODet serviceOrderData = liserviceOrderData;
                    return (!String.IsNullOrEmpty(serviceOrderData?.SrvOrdType) && !String.IsNullOrEmpty(serviceOrderData?.RefNbr)) ?
                                String.Format("{0}-{1}", serviceOrderData?.SrvOrdType.Trim(), serviceOrderData?.RefNbr.Trim()) : null;
                }
            }
            return null;
        }
    }

    public class PXExtPOLinkFromSO<POCreatedFld, SOLineNbrFld, SOOrderTypeFld, SOOrderNbrFld> : BqlFormulaEvaluator<POCreatedFld, SOLineNbrFld, SOOrderTypeFld, SOOrderNbrFld>, IBqlOperand
        where POCreatedFld : IBqlOperand
        where SOLineNbrFld : IBqlOperand
        where SOOrderTypeFld : IBqlOperand
        where SOOrderNbrFld : IBqlOperand
    {
        public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> parameters)
        {
            bool? poCreated = (bool?)(parameters[typeof(POCreatedFld)]);
            int? soLineNbr = (int?)(parameters[typeof(SOLineNbrFld)]);
            string soOrderType = Convert.ToString(parameters[typeof(SOOrderTypeFld)]);
            string soOrderNbr = Convert.ToString(parameters[typeof(SOOrderNbrFld)]);

            if (soLineNbr.HasValue && !String.IsNullOrEmpty(soOrderType) && !String.IsNullOrEmpty(soOrderNbr))
            {
                if (poCreated.GetValueOrDefault(false))
                {
                    SOLineSplit sodata = PXSelectReadonly<SOLineSplit,
                                            Where<SOLineSplit.orderType, Equal<Required<SOLineSplit.orderType>>,
                                                And<SOLineSplit.orderNbr, Equal<Required<SOLineSplit.orderNbr>>,
                                                And<SOLineSplit.lineNbr, Equal<Required<SOLineSplit.lineNbr>>>>>>.
                                                Select(cache.Graph, soOrderType, soOrderNbr, soLineNbr);
                    return (!String.IsNullOrEmpty(sodata?.POType) && !String.IsNullOrEmpty(sodata?.PONbr)) ?
                                String.Format("{0}-{1}", sodata?.POType.Trim(), sodata?.PONbr.Trim()) : null;
                }
            }
            return null;
        }
    }
}