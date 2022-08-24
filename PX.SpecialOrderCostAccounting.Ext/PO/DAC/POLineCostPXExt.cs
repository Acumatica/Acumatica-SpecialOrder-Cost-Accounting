using PX.Data;
using PX.Objects.CS;
using PX.Objects.PO;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public sealed class POLineCostPXExt : PXCacheExtension<POLine>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

        #region UsrIsSpecialOrderItem

        public abstract class usrIsSpecialOrderItem : PX.Data.BQL.BqlBool.Field<usrIsSpecialOrderItem> { }

        /// <summary>
        /// Un-bound field - Special Order Item identifier for sepecified inventory Item.
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(POLine.inventoryID))]
        [PXFormula(typeof(Selector<POLine.inventoryID, InventoryItemCostPXExt.usrIsSpecialOrderItem>))]
        public bool? UsrIsSpecialOrderItem { get; set; }

        #endregion

        #region UsrIsServiceOrderLine

        public abstract class usrIsServiceOrderLine : PX.Data.BQL.BqlBool.Field<usrIsServiceOrderLine> { }

        /// <summary>
        /// Un-bound field - Service Order Line Identifer
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(POLine.lineType))]
        [PXFormula(typeof(Switch<Case<Where<POLine.lineType, Equal<POLineType.goodsForServiceOrder>>, True>, False>))]
        public bool? UsrIsServiceOrderLine { get; set; }

        #endregion

        #region UsrIsNonServiceOrderLine

        public abstract class usrIsNonServiceOrderLine : PX.Data.BQL.BqlBool.Field<usrIsNonServiceOrderLine> { }

        /// <summary>
        /// Un-bound field - Not Service Order Line Identifer
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(POLine.lineType))]
        [PXFormula(typeof(Switch<Case<Where<POLine.lineType, NotEqual<POLineType.goodsForServiceOrder>>, True>, False>))]
        public bool? UsrIsNonServiceOrderLine { get; set; }

        #endregion

        #region UsrSOLinkRef
        public abstract class usrSOLinkRef : PX.Data.BQL.BqlString.Field<usrSOLinkRef> { }

        [PXString]
        [PXUIField(DisplayName = "Sales/Service Order #", Enabled = false, IsReadOnly = true)]
        public string UsrSOLinkRef { get; set; }
        #endregion

        #region UsrDeLink
        public abstract class usrDeLink : PX.Data.BQL.BqlBool.Field<usrDeLink> { }

        [PXBool]
        [PXUnboundDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Delinked", Enabled = false)]
        public bool? UsrDeLink { get; set; }
        #endregion

        #region UsrIsFreight
        public abstract class usrIsFreight : PX.Data.BQL.BqlBool.Field<usrIsFreight> { }

        [PXDBBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Freight", Enabled = false)]
        public bool? UsrIsFreight { get; set; }
        #endregion

        #region UsrAddFreightStateFld
        public abstract class usrAddFreightStateFld : PX.Data.BQL.BqlBool.Field<usrAddFreightStateFld> { }

        /// <summary>
        /// Un-bound field - StateColumn to Enable/Disable AddFreight Action -- Work In Progress
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(POLineCostPXExt.usrSOLinkRef), typeof(POLineCostPXExt.usrIsFreight))]
        //Dhiren --- Work In Progress 
        //[PXFormula(typeof(Switch<Case<Where<POLineCostPXExt.usrSOLinkRef, IsNotNull, And<POLineCostPXExt.usrSOLinkRef, NotEqual<StringMultiple>,
        //                                And<POLineCostPXExt.usrIsFreight, NotEqual<True>>>>, True>, False>))]
        [PXFormula(typeof(Switch<Case<Where<POLineCostPXExt.usrSOLinkRef, IsNotNull>, False>, False>))]
        [PXUIField(DisplayName = "Add Freight", IsReadOnly = true)]
        public bool? UsrAddFreightStateFld { get; set; }
        #endregion

        #region UsrDelinkStateFld
        public abstract class usrDelinkStateFld : PX.Data.BQL.BqlBool.Field<usrDelinkStateFld> { }

        /// <summary>
        /// Un-bound field - StateColumn to Enable/Disable Delink Action -- Work In Progress 
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(POLineCostPXExt.usrSOLinkRef))]
        //Dhiren --- Work In Progress  
        //[PXFormula(typeof(Switch<Case<Where<POLineCostPXExt.usrSOLinkRef, IsNotNull>, True>, False>))]
        [PXFormula(typeof(Switch<Case<Where<POLineCostPXExt.usrSOLinkRef, IsNotNull>, False>, False>))]
        [PXUIField(DisplayName = "Delink", IsReadOnly = true)]
        public bool? UsrDelinkStateFld { get; set; }
        #endregion
    }

    public class StringMultiple : PX.Data.BQL.BqlString.Constant<StringMultiple>
    {
        public StringMultiple() : base(Messages.ViewMultiple) { }
    }
}