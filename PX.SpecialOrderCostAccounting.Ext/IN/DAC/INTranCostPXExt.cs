using PX.Data;
using PX.Objects.IN;

namespace PX.SpecialOrderCostAccounting.Ext
{
    public sealed class INTranCostPXExt : PXCacheExtension<INTran>
    {
        #region UsrSpecialOrderCost
        public abstract class usrSpecialOrderCost : PX.Data.BQL.BqlBool.Field<usrSpecialOrderCost> { }

        /// <summary>
        /// Indicator if Vendor Cost should be used for Financial Entry of Issue Document.
        /// </summary>
        [PXDBBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Is Special Order Cost", Enabled = false, Visible = false)]
        public bool? UsrSpecialOrderCost { get; set; }
        #endregion
    }
}
