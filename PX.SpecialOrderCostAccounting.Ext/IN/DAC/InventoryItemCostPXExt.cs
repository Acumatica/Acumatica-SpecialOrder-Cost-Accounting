using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public sealed class InventoryItemCostPXExt : PXCacheExtension<InventoryItem>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.inventory>();

        #region UsrIsSpecialOrderItem

        public abstract class usrIsSpecialOrderItem : PX.Data.BQL.BqlBool.Field<usrIsSpecialOrderItem> { }

        /// <summary>
        /// Special Order Item Indicator - Restricted for Non-Lot/Serialized Tracked Stock and with Average Valuation
        /// </summary>
        [PXDBBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Special Order Only")]
        [PXUIVerify(typeof(Where<InventoryItemCostPXExt.usrInvalidSpecialItem, Equal<False>>),
            PXErrorLevel.Error, Messages.InvalidSpecialOrderItem,
            CheckOnRowSelected = false,
            CheckOnInserted = false,
            CheckOnVerify = true,
            CheckOnRowPersisting = true)]
        [PXFormula(typeof(Default<InventoryItem.valMethod>))]
        [PXFormula(typeof(Default<InventoryItem.lotSerClassID>))]
        public bool? UsrIsSpecialOrderItem { get; set; }

        #endregion

        #region UsrInvalidSpecialItem

        public abstract class usrInvalidSpecialItem : PX.Data.BQL.BqlBool.Field<usrInvalidSpecialItem> { }

        /// <summary>
        /// Un-bound field - to identify if Stock Item is valid Special Order Item
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(InventoryItem.valMethod), typeof(InventoryItemCostPXExt.usrIsSpecialOrderItem))]
        [PXFormula(typeof(Switch<Case<Where2<Where<InventoryItem.valMethod, NotEqual<INValMethod.average>,
                                                Or<usrIsLotOrSerial, Equal<True>>>,
                                                And<Where<usrIsSpecialOrderItem, Equal<True>>>>, True>, False>))]
        public bool? UsrInvalidSpecialItem { get; set; }

        #endregion

        #region UsrIsLotOrSerial

        public abstract class usrIsLotOrSerial : PX.Data.BQL.BqlBool.Field<usrIsLotOrSerial> { }

        /// <summary>
        /// Un-bound field - to identify if Stock Item is Lot/Serial Tracked
        /// </summary>
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDependsOnFields(typeof(InventoryItem.lotSerClassID))]
        [PXFormula(typeof(False.When<Data.BQL.Use<Selector<InventoryItem.lotSerClassID, INLotSerClass.lotSerTrack>>.AsString.
                                        IsEqual<INLotSerTrack.notNumbered>>.Else<True>))]
        public bool? UsrIsLotOrSerial { get; set; }

        #endregion
    }
}